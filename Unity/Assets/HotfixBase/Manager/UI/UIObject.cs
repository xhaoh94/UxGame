using System;
using System.Collections.Generic;
using System.Reflection;
using FairyGUI;
using Cysharp.Threading.Tasks;

namespace Ux
{
    public enum UIState
    {
        Show,
        ShowAnim,
        Hide,
        HideAnim
    }
    public interface IUIAsync
    {
        void Change(bool b);
    }
    public interface IUISetParam
    {
        void SetParam(IUIParam param);
    }
    public class UIObject : IUISetParam
    {
        private UIEvent _event;
        private List<UIObject> _components;
        public List<UIObject> Components => _components ??= new List<UIObject>();
        private UIState _state = UIState.Hide;
        protected int _showVersion;
        protected int _hideVersion;
        public virtual UIState State
        {
            get => _state;
            private set => _state = value;
        }

        public virtual UIObject Parent { get; private set; }
        public GObject GObject { get; private set; }

        public void AddComponent(UIObject component)
        {
            Components.Add(component);
        }

        IUIParam _paramVo;
        void IUISetParam.SetParam(IUIParam param)
        {
            _paramVo = param;
        }
        protected bool TryGetParam<V>(out V value, UIParamType type = UIParamType.A)
        {
            if (_paramVo == null)
            {
                value = default;
                return false;
            }
            return _paramVo.TryGet(out value, type);
        }

        /// <summary>
        /// 显示动效
        /// </summary>
        protected virtual IUIAnim ShowAnim { get; set; } = null;

        /// <summary>
        /// 关闭动效
        /// </summary>
        protected virtual IUIAnim HideAnim { get; set; } = null;

        protected Action<int, IUIParam, bool> OnShowCallback;
        protected Action OnHideCallback;

        protected T ParentAs<T>() where T : UIObject
        {
            return Parent as T;
        }

        protected T ObjAs<T>() where T : GObject
        {
            return GObject as T;
        }

        protected void Init(GObject gObj, UIObject parent = null)
        {
            if (gObj == null || GObject != null) return;
            _event ??= Pool.Get<UIEvent>();
            GObject = gObj;
            Parent = parent;
            CreateChildren();
            OnInit();
        }


        protected virtual void OnInit() { }

        protected virtual void CreateChildren() { }
        

        protected virtual void ToShow(bool isAnim, int id, bool checkStack, int showVersion)
        {
            HideAnim?.Stop();
            if (isAnim && ShowAnim != null)
            {
                State = UIState.ShowAnim;
                ShowAnim?.SetToStart();                
                ShowAnim?.Play(OnShowAnimCompleteInternal);
            }
            else
            {
                State = UIState.Show;
                ShowAnim?.SetToEnd();
            }
            RemoveTag();
            OnAddEvent();
            if (_components != null)
            {
                foreach (var component in _components)
                {
                    (component as IUISetParam).SetParam(_paramVo);
                    component._showVersion = showVersion;
                    component.ToShow(isAnim, id, checkStack, showVersion);
                }
            }
            OnShow();
            _CheckShow(id, checkStack, showVersion).Forget();
        }

        private void OnShowAnimCompleteInternal()
        {
            State = UIState.Show;
        }
        private void _ChangeAsync(bool b)
        {
            if (this is IUIAsync async)
            {
                async.Change(b);
            }
        }
        
        /// <summary>
        /// 等待显示动画完成
        /// 循环等待直到：1)自身状态变为Show 2)父节点动画完成（避免父节点动画期间子节点状态异常）
        /// </summary>
        async UniTaskVoid _CheckShow(int id, bool checkStack, int showVersion)
        {
            _ChangeAsync(true);
            while (State != UIState.Show || Parent is { State: UIState.ShowAnim })
            {
                await UniTask.Yield();
                if (showVersion != GetShowVersion())
                {
                    ShowAnim?.SetToEnd();
                    State = UIState.Show;
                    _ChangeAsync(false);
                    return;
                }
            }
            _ChangeAsync(false);
            // 如果延迟的DoHide在_ChangeAsync中被执行，State已不再是Show，
            // 此时不应触发显示完成回调，否则会导致已隐藏的UI被错误地提交为可见。
            if (State != UIState.Show)
            {
                return;
            }
            OnShowAnimComplete();
            OnShowCallback?.Invoke(id, _paramVo, checkStack);
        }

        protected virtual void ToOverwrite()
        {
            if (_components != null)
            {
                foreach (var component in _components)
                {
                    (component as IUISetParam).SetParam(_paramVo);
                    component.ToOverwrite();
                }
            }
            OnOverwrite();
        }
        protected virtual void OnOverwrite() { }
        protected virtual void OnShow() { }
        protected virtual void OnShowAnimComplete() { }
        protected virtual void OnAddEvent() { }

        protected virtual void ToHide(bool isAnim, bool checkStack, int hideVersion)
        {
            (this as IUISetParam).SetParam(null);
            ShowAnim?.Stop();
            if (isAnim && HideAnim != null)
            {
                State = UIState.HideAnim;
                HideAnim?.SetToStart();                
                HideAnim?.Play(OnHideAnimCompleteInternal);
            }
            else
            {
                State = UIState.Hide;
                HideAnim?.SetToEnd();
            }

            if (_components != null)
            {
                foreach (var component in _components)
                {
                    component._hideVersion = hideVersion;
                    component.ToHide(isAnim, checkStack, hideVersion);
                }
            }

            OnHide();
            RemoveTag();
            _CheckHide(hideVersion).Forget();
        }

        private void OnHideAnimCompleteInternal()
        {
            State = UIState.Hide;
        }

        /// <summary>
        /// 等待隐藏动画完成
        /// 循环等待直到：1)自身状态变为Hide 2)父节点动画完成（确保父节点先于子节点隐藏）
        /// </summary>
        async UniTaskVoid _CheckHide(int hideVersion)
        {
            _ChangeAsync(true);
            while (State != UIState.Hide || Parent is { State: UIState.HideAnim })
            {
                await UniTask.Yield();
                if (hideVersion != GetHideVersion())
                {
                    HideAnim?.SetToEnd();
                    State = UIState.Hide;
                    _ChangeAsync(false);
                    return;
                }
            }
            _ChangeAsync(false);
            // 如果延迟的DoShow在_ChangeAsync中被执行，State已不再是Hide，
            // 此时不应触发隐藏完成回调。
            if (State != UIState.Hide)
            {
                return;
            }
            OnHideCallback?.Invoke();
        }

        protected int GetShowVersion()
        {
            return _showVersion;
        }

        protected int GetHideVersion()
        {
            return _hideVersion;
        }

        /// <summary>
        /// 注意！！！
        /// 不要在这里注册定时器或是监听事件。
        /// 在这里注册的定时器或是事件，都会流程给清空的
        /// </summary>
        protected virtual void OnHide() { }

        protected void ToDispose(bool isDisposeGObject = true)
        {
            _event.Release();
            _event = null;
            OnShowCallback = null;
            OnHideCallback = null;
            if (_components != null)
            {
                foreach (var component in _components)
                {
                    component.ToDispose(isDisposeGObject);
                }

                _components.Clear();
                _components = null;
            }
            OnDispose();
            if (isDisposeGObject) GObject?.Dispose();
            GObject = null;
            Parent = null;
        }

        protected virtual void OnDispose() { }


        #region 事件
        protected void AddEvent(UIObject gObj, string type, EventCallback1 fn)
        {
            if (gObj == null) return;
            AddEvent(gObj.GObject, type, fn);
        }

        protected void AddEvent(UIObject gObj, string type, EventCallback0 fn)
        {
            if (gObj == null) return;
            AddEvent(gObj.GObject, type, fn);
        }
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
        protected void AddMultipleClick(UIObject gObj, EventCallback0 fn0, int clickCnt = 2, float gapTime = 0.3f)
        {
            if (gObj == null) return;
            AddMultipleClick(gObj.GObject, fn0, clickCnt, gapTime);
        }
        /// <summary>
        /// 多次点击事件，注册了多次点击事件，即使是单击也会受到gapTime延时触发
        /// </summary>        
        protected void AddMultipleClick(GObject gObj, EventCallback0 fn0, int clickCnt = 2, float gapTime = 0.3f)
        {
            if (gObj == null) return;
            _event?.AddMultipleClick(gObj, fn0, clickCnt, gapTime);
        }
        protected void AddMultipleClick(UIObject gObj, EventCallback1 fn1, int clickCnt = 2, float gapTime = 0.3f)
        {
            if (gObj == null) return;
            AddMultipleClick(gObj.GObject, fn1, clickCnt, gapTime);
        }
        /// <summary>
        /// 多次点击事件，注册了多次点击事件，即使是单击也会受到gapTime延时触发
        /// </summary>    
        protected void AddMultipleClick(GObject gObj, EventCallback1 fn1, int clickCnt = 2, float gapTime = 0.3f)
        {
            if (gObj == null) return;
            _event?.AddMultipleClick(gObj, fn1, clickCnt, gapTime);
        }

        protected void AddLongPress(UIObject gObj, Func<bool> fn, float delay = 0.2f, int loopCnt = 0,
       int holdRangeRadius = 50)
        {
            if (gObj == null) return;
            AddLongPress(gObj.GObject, fn, delay, loopCnt, holdRangeRadius);
        }
        /// <summary>
        /// 长按事件
        /// </summary>
        /// <param name="gObj"></param>
        /// <param name="fn"></param>        
        /// <param name="delay">后续触发间隔</param>
        /// <param name="loopCnt">触发次数</param>
        /// <param name="holdRangeRadius">手指触发范围</param>
        protected void AddLongPress(GObject gObj, Func<bool> fn, float delay = 0.2f, int loopCnt = 0,
            int holdRangeRadius = 50)
        {
            if (gObj == null) return;
            _event?.AddLongPress(gObj, fn, -1, delay, loopCnt, holdRangeRadius);
        }
        protected void AddLongPress(UIObject gObj, float first, Func<bool> fn, float delay = 0.2f, int loopCnt = 0,
            int holdRangeRadius = 50)
        {
            if (gObj == null) return;
            AddLongPress(gObj.GObject, first, fn, delay, loopCnt, holdRangeRadius);
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
        protected void AddLongPress(GObject gObj, float first, Func<bool> fn, float delay = 0.2f, int loopCnt = 0,
            int holdRangeRadius = 50)
        {
            if (gObj == null) return;
            _event?.AddLongPress(gObj, fn, first, delay, loopCnt, holdRangeRadius);
        }
        protected void AddClick(UIObject gObj, EventCallback1 fn)
        {
            if (gObj == null) return;
            AddClick(gObj.GObject, fn);
        }
        protected void AddClick(GObject gObj, EventCallback1 fn)
        {
            if (gObj == null) return;
            AddEvent(gObj.onClick, fn);
        }
        protected void AddClick(UIObject gObj, EventCallback0 fn)
        {
            if (gObj == null) return;
            AddClick(gObj.GObject, fn);
        }
        protected void AddClick(GObject gObj, EventCallback0 fn)
        {
            if (gObj == null) return;
            AddEvent(gObj.onClick, fn);
        }

        protected void AddItemClick(UIList list, Action<IItemRenderer> fn)
        {
            if (list == null) return;
            list.AddItemClick(fn);
        }

        #endregion

        protected void RemoveTag()
        {
            _event?.RemoveEvents();
            EventMgr.Ins.OffTag(this);
            TimeMgr.Ins.RemoveTag(this);
        }
    }
}
