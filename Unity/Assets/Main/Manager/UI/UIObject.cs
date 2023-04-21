using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FairyGUI;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace Ux
{
    public enum UIState
    {
        Show,
        ShowAnim,
        Hide,
        HideAnim
    }

    public class UIObject
    {
        CancellationTokenSource _showToken;
        CancellationTokenSource _hideToken;

        private UIEvent _event;
        private List<UIObject> _components;
        public List<UIObject> Components => _components ??= new List<UIObject>();
        private UIState _state;

        public virtual UIState State
        {
            get
            {
                foreach (var state in Components.Select(t => t.State)
                             .Where(state => state is UIState.ShowAnim or UIState.HideAnim))
                {
                    return state;
                }

                return _state;
            }
        }

        public virtual UIObject Parent { get; private set; }
        public GObject GObject { get; private set; }

        /// <summary>
        /// 显示动效
        /// </summary>
        protected virtual Transition ShowTransition { get; } = null;

        /// <summary>
        /// 关闭动效
        /// </summary>
        protected virtual Transition HideTransition { get; }= null;

        protected Action OnShowCallBack;
        protected Action OnHideCallBack;

        protected T ParentAs<T>() where T : UIObject
        {
            return Parent as T;
        }

        protected T ObjAs<T>() where T : GObject
        {
            return GObject as T;
        }

        protected T ParamAs<T>(object param)
        {
            if (param is T cParam)
            {
                return cParam;
            }

            return default(T);
        }

        protected void Init(GObject gObj, UIObject parent = null)
        {
            if (gObj == null || GObject != null) return;
            _event ??= Pool.Get<UIEvent>();
            Parent = parent;
            GObject = gObj;
            CreateChildren();
            OnInit();
        }


        protected virtual void OnInit()
        {
        }


        private void ToCreateChildren(Type selfType, GComponent component)
        {
            for (int i = 0; i < component.numChildren; i++)
            {
                var child = component.GetChildAt(i);
                const BindingFlags flag = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
                var info = selfType.GetField(child.name, flag);
                if (info == null)
                {
                    continue;
                }

                object[] attributes = info.GetCustomAttributes(typeof(UIComponentAttribute), true);
                if (attributes.Length == 0)
                {
                    info.SetValue(this, child);
                }
                else
                {
                    var attribute = (UIComponentAttribute)attributes[0];
                    UIObject com;
                    if (attribute.Component != null)
                    {
                        com = (UIObject)Activator.CreateInstance(attribute.Component);
                    }
                    else
                    {
                        com = (UIObject)Activator.CreateInstance(info.FieldType);
                    }

                    com.Init(child, this);
                    info.SetValue(this, com);
                    Components.Add(com);
                }
            }

            for (int i = 0; i < component.Controllers.Count; i++)
            {
                var cont = component.GetControllerAt(i);
                const BindingFlags flag = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
                var info = selfType.GetField(cont.name, flag);
                if (info == null)
                {
                    continue;
                }

                info.SetValue(this, cont);
            }

            for (int i = 0; i < component.Transitions.Count; i++)
            {
                var trans = component.GetTransitionAt(i);
                const BindingFlags flag = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
                var info = selfType.GetField(trans.name, flag);
                if (info == null)
                {
                    continue;
                }

                info.SetValue(this, trans);
            }
        }

        protected virtual void CreateChildren()
        {
            var component = GObject is Window ? ObjAs<Window>().contentPane : ObjAs<GComponent>();
            var selfType = GetType();
            try
            {
                ToCreateChildren(selfType, component);
            }
            catch
            {
                Log.Error("ToCreateChildren失败,检查是否存在不规范的命名");
            }
        }


        public virtual void DoShow(bool isAnim, object param)
        {
            HideTransition?.Stop();
            if (isAnim && ShowTransition != null)
            {
                _state = UIState.ShowAnim;
                ShowTransition?.Play(() => { _state = UIState.Show; });
            }
            else
            {
                _state = UIState.Show;
            }

            OnShow(param);
            EventMgr.Instance.___RegisterFastMethod(this);

            foreach (var component in Components)
            {
                component.DoShow(isAnim, this);
            }

            _showToken = new CancellationTokenSource();
            _CheckShow(_showToken.Token).Forget();
        }

        async UniTaskVoid _CheckShow(CancellationToken token)
        {
            while (State != UIState.Show || Parent is { State: UIState.ShowAnim })
                await UniTask.Yield(token);
            if (!token.IsCancellationRequested)
            {
                OnShowTransitionComplete();
                OnShowCallBack?.Invoke();
            }

            _showToken = null;
        }

        void ClearShowToken()
        {
            if (_showToken == null) return;
            _showToken.Cancel();
            _showToken = null;
        }

        void ClearHideToken()
        {
            if (_hideToken == null) return;
            _hideToken.Cancel();
            _hideToken = null;
        }

        protected virtual void OnShow(object param)
        {
        }

        protected virtual void OnShowTransitionComplete()
        {
        }


        public virtual void DoResume(object param)
        {
            switch (State)
            {
                case UIState.Hide:
                    Log.Error("状态错误，不应该存在Hide状态下调用DoResume方法");
                    break;
                case UIState.HideAnim:
                    ClearHideToken();
                    DoShow(false, param);
                    break;
                case UIState.Show:
                case UIState.ShowAnim:
                default:
                    OnResume(param);
                    foreach (var component in Components)
                    {
                        component.DoResume(this);
                    }

                    break;
            }
        }

        protected virtual void OnResume(object param)
        {
        }


        public virtual void DoHide(bool isAnim)
        {
            ClearShowToken();
            ShowTransition?.Stop();
            if (isAnim && HideTransition != null)
            {
                _state = UIState.HideAnim;
                HideTransition?.Play(() => { _state = UIState.Hide; });
            }
            else
            {
                _state = UIState.Hide;
            }

            RemoveEvents();
            RemoveTimers();
            foreach (var component in Components)
            {
                component.DoHide(isAnim);
            }

            OnHide();
            _hideToken = new CancellationTokenSource();
            _CheckHide(_hideToken.Token).Forget();
        }

        async UniTaskVoid _CheckHide(CancellationToken token)
        {
            while (State != UIState.Hide || Parent is { State: UIState.HideAnim })
                await UniTask.Yield(token);
            if (!token.IsCancellationRequested)
            {
                OnHideTransitionComplete();
                OnHideCallBack?.Invoke();
            }

            _hideToken = null;
        }

        protected virtual void OnHide()
        {
        }

        protected virtual void OnHideTransitionComplete()
        {
        }

        public void Dispose(bool isDisposeGObject = true)
        {
            _event.Release();
            _event = null;
            ClearShowToken();
            ClearHideToken();
            OnShowCallBack = null;
            OnHideCallBack = null;
            if (_components != null)
            {
                foreach (var component in _components)
                {
                    component.Dispose(isDisposeGObject);
                }

                _components.Clear();
                _components = null;
            }

            if (isDisposeGObject) GObject?.Dispose();
            GObject = null;
            Parent = null;
            OnDispose();
        }

        protected virtual void OnDispose()
        {
        }


        #region 事件

        protected void AddEvent(GObject gObj, string type, EventCallback1 fn)
        {
            if (gObj == null) return;
            AddEvent(new EventListener(gObj, type), fn);
        }

        protected void AddEvent(GObject gObj, string type, EventCallback0 fn)
        {
            if (gObj == null) return;
            AddEvent(new EventListener(gObj, type), fn);
        }

        protected void AddEvent(EventListener listener, EventCallback1 fn)
        {
            _event?.AddEvent(listener, fn);
        }

        protected void AddEvent(EventListener listener, EventCallback0 fn)
        {
            _event?.AddEvent(listener, fn);
        }

        /// <summary>
        /// 多次点击事件，注册了多次点击事件，即使是单击也会受到gapTime延时触发
        /// </summary>        
        protected void AddDoubleClick(GObject gObj, EventCallback0 fn0, int clickCnt = 2, float gapTime = 0.3f)
        {
            if (gObj == null) return;
            _event?.AddDoubleClick(gObj, fn0, clickCnt, gapTime);
        }

        /// <summary>
        /// 多次点击事件，注册了多次点击事件，即使是单击也会受到gapTime延时触发
        /// </summary>    
        protected void AddDoubleClick(GObject gObj, EventCallback1 fn1, int clickCnt = 2, float gapTime = 0.3f)
        {
            if (gObj == null) return;
            _event?.AddDoubleClick(gObj, fn1, clickCnt, gapTime);
        }

        /// <summary>
        /// 长按事件
        /// </summary>
        /// <param name="gObj"></param>
        /// <param name="fn"></param>        
        /// <param name="delay">后续触发间隔</param>
        /// <param name="loopCnt">触发次数</param>
        /// <param name="holdRangeRadius">手指触发范围</param>
        protected void AddLongClick(GObject gObj, Func<bool> fn, float delay = 0.2f, int loopCnt = 0,
            int holdRangeRadius = 50)
        {
            if (gObj == null) return;
            _event?.AddLongClick(gObj, fn, -1, delay, loopCnt, holdRangeRadius);
        }

        /// <summary>
        /// 长按事件
        /// </summary>
        /// <param name="gObj"></param>
        /// <param name="fn"></param>
        /// <param name="first">第一次触发时间</param>
        /// <param name="delay">后续触发间隔</param>
        /// <param name="loopCnt">触发次数</param>
        /// <param name="holdRangeRadius">手指触发范围</param>
        protected void AddLongClick(GObject gObj, float first, Func<bool> fn, float delay = 0.2f, int loopCnt = 0,
            int holdRangeRadius = 50)
        {
            if (gObj == null) return;
            _event?.AddLongClick(gObj, fn, first, delay, loopCnt, holdRangeRadius);
        }

        protected void AddClick(GObject gObj, EventCallback1 fn)
        {
            if (gObj == null) return;
            AddEvent(gObj.onClick, fn);
        }

        protected void AddClick(GObject gObj, EventCallback0 fn)
        {
            if (gObj == null) return;
            AddEvent(gObj.onClick, fn);
        }

        protected void AddItemClick(GList gList, EventCallback1 fn)
        {
            if (gList == null) return;
            AddEvent(gList.onClickItem, fn);
        }

        protected void AddItemClick(GList gList, EventCallback0 fn)
        {
            if (gList == null) return;
            AddEvent(gList.onClickItem, fn);
        }

        private void RemoveEvents()
        {
            _event?.RemoveEvents();
            EventMgr.Instance.OffAll(this);
        }

        #endregion

        #region 定时器

        private void RemoveTimers()
        {
            TimeMgr.Instance.RemoveAll(this);
        }

        #region 定时器 Time

        /// <summary>
        /// 循环回调
        /// </summary>
        /// <param name="delay">延时秒数</param>
        /// <param name="action">调用方法</param>
        /// <returns></returns>
        protected long DoLoop(float delay, Action action)
        {
            return DoTimer(delay, 0, action);
        }

        /// <summary>
        /// 循环回调
        /// </summary>
        /// <param name="first">第一次触发秒数</param>
        /// <param name="delay">延时秒数</param>
        /// <param name="action">调用方法</param>
        /// <returns></returns>
        protected long DoLoop(float first, float delay, Action action)
        {
            return DoTimer(first, delay, 0, action);
        }

        /// <summary>
        /// 单次回调
        /// </summary>
        /// <param name="delay">延时秒数</param>
        /// <param name="action">调用方法</param>
        /// <returns></returns>
        protected long DoOnce(float delay, Action action)
        {
            return DoTimer(delay, 1, action);
        }

        /// <summary>
        /// 延时调用
        /// </summary>
        /// <param name="delay">延时秒数</param>
        /// <param name="repeat">调用次数 小于或等于0则循环 </param>
        /// <param name="action">调用方法</param>
        /// <param name="complete">结束回调</param>
        /// <returns></returns>
        protected long DoTimer(float delay, int repeat, Action action,
            Action complete = null)
        {
            return TimeMgr.Instance.DoTimer(delay, repeat, action, complete);
        }

        protected long DoTimer(float first, float delay, int repeat, Action action,
            Action complete = null)
        {
            return TimeMgr.Instance.DoTimer(first, delay, repeat, action, complete);
        }

        protected long DoTimer(float delay, int repeat, Action action,
            Action<object> complete, object completeParam)
        {
            return TimeMgr.Instance.DoTimer(delay, repeat, action, complete, completeParam);
        }

        protected long DoTimer(float first, float delay, int repeat, Action action,
            Action<object> complete, object completeParam)
        {
            return TimeMgr.Instance.DoTimer(first, delay, repeat, action, complete, completeParam);
        }

        /// <summary>
        /// 延时调用
        /// </summary>
        /// <param name="delay">延时秒数</param>
        /// <param name="repeat">调用次数 小于或等于0则循环 </param>
        /// <param name="action">调用方法</param>
        /// <param name="a">附加参数</param>
        /// <param name="complete">结束回调</param>
        /// <returns></returns>
        public long DoTimer<A>(float delay, int repeat, Action<A> action, A a,
            Action complete = null)
        {
            return TimeMgr.Instance.DoTimer(delay, repeat, action, a, complete);
        }

        public long DoTimer<A>(float first, float delay, int repeat, Action<A> action, A a,
            Action complete = null)
        {
            return TimeMgr.Instance.DoTimer(first, delay, repeat, action, a, complete);
        }

        public long DoTimer<A>(float delay, int repeat, Action<A> action, A a,
            Action<object> complete, object completeParam)
        {
            return TimeMgr.Instance.DoTimer(delay, repeat, action, a, complete, completeParam);
        }

        public long DoTimer<A>(float first, float delay, int repeat, Action<A> action, A a,
            Action<object> complete, object completeParam)
        {
            return TimeMgr.Instance.DoTimer(first, delay, repeat, action, a, complete, completeParam);
        }

        /// <summary>
        /// 延时调用
        /// </summary>
        /// <param name="delay">延时秒数</param>
        /// <param name="repeat">调用次数 小于或等于0则循环 </param>
        /// <param name="action">调用方法</param>
        /// <param name="a">附加参数</param>
        /// <param name="b">附加参数</param>
        /// <param name="complete">结束回调</param>
        /// <returns></returns>
        public long DoTimer<A, B>(float delay, int repeat, Action<A, B> action, A a, B b,
            Action complete = null)
        {
            return TimeMgr.Instance.DoTimer(delay, repeat, action, a, b, complete);
        }

        public long DoTimer<A, B>(float first, float delay, int repeat, Action<A, B> action, A a, B b,
            Action complete = null)
        {
            return TimeMgr.Instance.DoTimer(first, delay, repeat, action, a, b, complete);
        }

        public long DoTimer<A, B>(float delay, int repeat, Action<A, B> action, A a, B b,
            Action<object> complete, object completeParam)
        {
            return TimeMgr.Instance.DoTimer(delay, repeat, action, a, b, complete, completeParam);
        }

        public long DoTimer<A, B>(float first, float delay, int repeat, Action<A, B> action, A a, B b,
            Action<object> complete, object completeParam)
        {
            return TimeMgr.Instance.DoTimer(first, delay, repeat, action, a, b, complete, completeParam);
        }

        /// <summary>
        /// 延时调用
        /// </summary>
        /// <param name="delay">延时秒数</param>
        /// <param name="repeat">调用次数 小于或等于0则循环 </param>
        /// <param name="action">调用方法</param>        
        /// <param name="a">附加参数</param>
        /// <param name="b">附加参数</param>
        /// <param name="c">附加参数</param>
        /// <param name="complete">结束回调</param>
        /// <returns></returns>
        public long DoTimer<A, B, C>(float delay, int repeat, Action<A, B, C> action, A a, B b, C c,
            Action complete = null)
        {
            return TimeMgr.Instance.DoTimer(delay, repeat, action, a, b, c, complete);
        }

        public long DoTimer<A, B, C>(float first, float delay, int repeat, Action<A, B, C> action, A a, B b, C c,
            Action complete = null)
        {
            return TimeMgr.Instance.DoTimer(first, delay, repeat, action, a, b, c, complete);
        }

        public long DoTimer<A, B, C>(float delay, int repeat, Action<A, B, C> action, A a, B b, C c,
            Action<object> complete, object completeParam)
        {
            return TimeMgr.Instance.DoTimer(delay, repeat, action, a, b, c, complete, completeParam);
        }

        public long DoTimer<A, B, C>(float first, float delay, int repeat, Action<A, B, C> action, A a, B b, C c,
            Action<object> complete, object completeParam)
        {
            return TimeMgr.Instance.DoTimer(first, delay, repeat, action, a, b, c, complete, completeParam);
        }

        public void RemoveTimer(Action action)
        {
            TimeMgr.Instance.RemoveTimer(action);
        }

        public void RemoveTimer<A>(Action<A> action)
        {
            TimeMgr.Instance.RemoveTimer(action);
        }

        public void RemoveTimer<A, B>(Action<A, B> action)
        {
            TimeMgr.Instance.RemoveTimer(action);
        }

        public void RemoveTimer<A, B, C>(Action<A, B, C> action)
        {
            TimeMgr.Instance.RemoveTimer(action);
        }

        #endregion

        #region 定时器 Frame

        /// <summary>
        /// 延帧调用
        /// </summary>
        /// <param name="delay">延时帧数</param>
        /// <param name="repeat">调用次数 小于或等于0则循环 </param>
        /// <param name="action">调用方法</param>
        /// <param name="complete">结束回调</param>
        /// <returns></returns>
        public long DoFrame(int delay, int repeat, Action action,
            Action complete = null)
        {
            return TimeMgr.Instance.DoFrame(delay, repeat, action, complete);
        }

        public long DoFrame(int first, int delay, int repeat, Action action,
            Action complete = null)
        {
            return TimeMgr.Instance.DoFrame(first, delay, repeat, action, complete);
        }

        public long DoFrame(int delay, int repeat, Action action,
            Action<object> complete, object completeParam)
        {
            return TimeMgr.Instance.DoFrame(delay, repeat, action, complete, completeParam);
        }

        public long DoFrame(int first, int delay, int repeat, Action action,
            Action<object> complete, object completeParam)
        {
            return TimeMgr.Instance.DoFrame(first, delay, repeat, action, complete, completeParam);
        }

        /// <summary>
        /// 延帧调用
        /// </summary>
        /// <param name="delay">延时帧数</param>
        /// <param name="repeat">调用次数 小于或等于0则循环 </param>
        /// <param name="action">调用方法</param>        
        /// <param name="a">附加参数</param>
        /// <param name="complete">结束回调</param>
        /// <returns></returns>
        public long DoFrame<A>(int delay, int repeat, Action<A> action, A a,
            Action complete = null)
        {
            return TimeMgr.Instance.DoFrame(delay, repeat, action, a, complete);
        }

        public long DoFrame<A>(int first, int delay, int repeat, Action<A> action, A a,
            Action complete = null)
        {
            return TimeMgr.Instance.DoFrame(first, delay, repeat, action, a, complete);
        }

        public long DoFrame<A>(int delay, int repeat, Action<A> action, A a,
            Action<object> complete, object completeParam)
        {
            return TimeMgr.Instance.DoFrame(delay, repeat, action, a, complete, completeParam);
        }

        public long DoFrame<A>(int first, int delay, int repeat, Action<A> action, A a,
            Action<object> complete, object completeParam)
        {
            return TimeMgr.Instance.DoFrame(first, delay, repeat, action, a, complete, completeParam);
        }

        /// <summary>
        /// 延帧调用
        /// </summary>
        /// <param name="delay">延时帧数</param>
        /// <param name="repeat">调用次数 小于或等于0则循环 </param>
        /// <param name="action">调用方法</param>        
        /// <param name="a">附加参数</param>
        /// <param name="b">附加参数</param>
        /// <param name="complete">结束回调</param>
        /// <returns></returns>
        public long DoFrame<A, B>(int delay, int repeat, Action<A, B> action, A a, B b,
            Action complete = null)
        {
            return TimeMgr.Instance.DoFrame(delay, repeat, action, a, b, complete);
        }

        public long DoFrame<A, B>(int first, int delay, int repeat, Action<A, B> action, A a, B b,
            Action complete = null)
        {
            return TimeMgr.Instance.DoFrame(first, delay, repeat, action, a, b, complete);
        }

        public long DoFrame<A, B>(int delay, int repeat, Action<A, B> action, A a, B b,
            Action<object> complete, object completeParam)
        {
            return TimeMgr.Instance.DoFrame(delay, repeat, action, a, b, complete, completeParam);
        }

        public long DoFrame<A, B>(int first, int delay, int repeat, Action<A, B> action, A a, B b,
            Action<object> complete, object completeParam)
        {
            return TimeMgr.Instance.DoFrame(first, delay, repeat, action, a, b, complete, completeParam);
        }

        /// <summary>
        /// 延帧调用
        /// </summary>
        /// <param name="delay">延时帧数</param>
        /// <param name="repeat">调用次数 小于或等于0则循环 </param>
        /// <param name="action">调用方法</param>
        /// <param name="a">附加参数</param>
        /// <param name="b">附加参数</param>
        /// <param name="c">附加参数</param>
        /// <param name="complete">结束回调</param>
        /// <returns></returns>
        public long DoFrame<A, B, C>(int delay, int repeat, Action<A, B, C> action, A a, B b, C c,
            Action complete = null)
        {
            return TimeMgr.Instance.DoFrame(delay, repeat, action, a, b, c, complete);
        }

        public long DoFrame<A, B, C>(int first, int delay, int repeat, Action<A, B, C> action, A a, B b, C c,
            Action complete = null)
        {
            return TimeMgr.Instance.DoFrame(first, delay, repeat, action, a, b, c, complete);
        }

        public long DoFrame<A, B, C>(int delay, int repeat, Action<A, B, C> action, A a, B b, C c,
            Action<object> complete, object completeParam)
        {
            return TimeMgr.Instance.DoFrame(delay, repeat, action, a, b, c, complete, completeParam);
        }

        public long DoFrame<A, B, C>(int first, int delay, int repeat, Action<A, B, C> action, A a, B b, C c,
            Action<object> complete, object completeParam)
        {
            return TimeMgr.Instance.DoFrame(first, delay, repeat, action, a, b, c, complete, completeParam);
        }


        public void RemoveFrame(Action action)
        {
            TimeMgr.Instance.RemoveFrame(action);
        }

        public void RemoveFrame<A>(Action<A> action)
        {
            TimeMgr.Instance.RemoveFrame(action);
        }

        public void RemoveFrame<A, B>(Action<A, B> action)
        {
            TimeMgr.Instance.RemoveFrame(action);
        }

        public void RemoveFrame<A, B, C>(Action<A, B, C> action)
        {
            TimeMgr.Instance.RemoveFrame(action);
        }

        #endregion

        #region 定时器 TimeStamp

        public long DoTimeStamp(DateTime dt, Action action)
        {
            return DoTimeStamp(dt.ToTimeStamp(), action);
        }

        public long DoTimeStamp(long timeStamp, Action action)
        {
            return TimeMgr.Instance.DoTimeStamp(timeStamp, action);
        }

        public long DoTimeStamp<A>(DateTime dt, Action<A> action, A a)
        {
            return DoTimeStamp(dt.ToTimeStamp(), action, a);
        }

        public long DoTimeStamp<A>(long timeStamp, Action<A> action, A a)
        {
            return TimeMgr.Instance.DoTimeStamp(timeStamp, action, a);
        }

        public long DoTimeStamp<A, B>(DateTime dt, Action<A, B> action, A a, B b)
        {
            return DoTimeStamp(dt.ToTimeStamp(), action, a, b);
        }

        public long DoTimeStamp<A, B>(long timeStamp, Action<A, B> action, A a, B b)
        {
            return TimeMgr.Instance.DoTimeStamp(timeStamp, action, a, b);
        }

        public long DoTimeStamp<A, B, C>(DateTime dt, Action<A, B, C> action, A a, B b, C c)
        {
            return DoTimeStamp(dt.ToTimeStamp(), action, a, b, c);
        }

        public long DoTimeStamp<A, B, C>(long timeStamp, Action<A, B, C> action, A a, B b, C c)
        {
            return TimeMgr.Instance.DoTimeStamp(timeStamp, action, a, b, c);
        }

        public void RemoveTimeStamp(Action action)
        {
            TimeMgr.Instance.RemoveTimeStamp(action);
        }

        public void RemoveTimeStamp<A>(Action<A> action)
        {
            TimeMgr.Instance.RemoveTimeStamp(action);
        }

        public void RemoveTimeStamp<A, B>(Action<A, B> action)
        {
            TimeMgr.Instance.RemoveTimeStamp(action);
        }

        public void RemoveTimeStamp<A, B, C>(Action<A, B, C> action)
        {
            TimeMgr.Instance.RemoveTimeStamp(action);
        }

        #endregion TimeStamp

        #region 定时器 Cron

        public long DoCron(string cron, Action action)
        {
            return TimeMgr.Instance.DoCron(cron, action);
        }

        public long DoCron<A>(string cron, Action<A> action, A a)
        {
            return TimeMgr.Instance.DoCron(cron, action, a);
        }

        public long DoCron<A, B>(string cron, Action<A, B> action, A a, B b)
        {
            return TimeMgr.Instance.DoCron(cron, action, a, b);
        }

        public long DoCron<A, B, C>(string cron, Action<A, B, C> action, A a, B b, C c)
        {
            return TimeMgr.Instance.DoCron(cron, action, a, b, c);
        }

        public void RemoveCron(Action action)
        {
            TimeMgr.Instance.RemoveCron(action);
        }

        public void RemoveCron<A>(Action<A> action)
        {
            TimeMgr.Instance.RemoveCron(action);
        }

        public void RemoveCron<A, B>(Action<A, B> action)
        {
            TimeMgr.Instance.RemoveCron(action);
        }

        public void RemoveCron<A, B, C>(Action<A, B, C> action)
        {
            TimeMgr.Instance.RemoveCron(action);
        }

        #endregion

        #endregion
    }
}