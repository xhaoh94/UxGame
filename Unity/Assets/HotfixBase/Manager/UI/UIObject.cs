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
        private List<UIObject> _animComponents;
        public List<UIObject> Components => _components ??= new List<UIObject>();
        private UIState _state = UIState.Hide;
        private bool _stateDirty = true;
        private UIState _cachedState = UIState.Hide;
        public virtual UIState State
        {
            get
            {
                if (!_stateDirty) return _cachedState;
                if (_animComponents != null)
                {
                    foreach (var component in _animComponents)
                    {
                        var state = component.State;
                        if (state is UIState.ShowAnim or UIState.HideAnim)
                        {
                            _cachedState = state;
                            _stateDirty = false;
                            return state;
                        }
                    }
                }
                _cachedState = _state;
                _stateDirty = false;
                return _cachedState;
            }
            private set
            {
                _state = value;
                MarkStateDirty();
            }
        }
        private void MarkStateDirty()
        {
            _stateDirty = true;
            Parent?.MarkStateDirty();
        }

        public virtual UIObject Parent { get; private set; }
        public GObject GObject { get; private set; }

        public void AddComponent(UIObject component)
        {
            Components.Add(component);
            if (component.ShowAnim != null || component.HideAnim != null)
            {
                _animComponents ??= new List<UIObject>();
                _animComponents.Add(component);
            }
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

        protected Action<int, IUIParam, bool> OnShowCallBack;
        protected Action OnHideCallBack;

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


        protected virtual void OnInit()
        {
        }

    
        protected virtual void CreateChildren()
        {

        }

        // 缓存动画完成回调，避免Lambda闭包分配
        private Action _onShowAnimComplete;
        private Action _onHideAnimComplete;

        protected virtual void ToShow(bool isAnim, int id, bool checkStack, CancellationTokenSource token)
        {
            HideAnim?.Stop();
            if (isAnim && ShowAnim != null)
            {
                State = UIState.ShowAnim;
                ShowAnim?.SetToStart();
                _onShowAnimComplete ??= OnShowAnimCompleteInternal;
                ShowAnim?.Play(_onShowAnimComplete);
            }
            else
            {
                State = UIState.Show;
                ShowAnim?.SetToEnd();
            }
            RemoveTag();
            OnAddEvent();
            foreach (var component in Components)
            {
                (component as IUISetParam).SetParam(_paramVo);
                component.ToShow(isAnim, id, checkStack, token);
            }
            OnShow();
            _CheckShow(id, checkStack, token).Forget();
        }

        private void OnShowAnimCompleteInternal()
        {
            State = UIState.Show;
        }
        void _ChangeAsync(bool b)
        {
            if (this is IUIAsync async)
            {
                async.Change(b);
            }
        }
        async UniTaskVoid _CheckShow(int id, bool checkStack, CancellationTokenSource token)
        {
            _ChangeAsync(true);
            bool isCanceled = false;
            while (State != UIState.Show || Parent is { State: UIState.ShowAnim })
            {
                if (token != null && !isCanceled)
                {
                    isCanceled = await UniTask.Yield(token.Token).SuppressCancellationThrow();
                    if (isCanceled)
                    {
                        ShowAnim?.SetToEnd();
                        State = UIState.Show;
                    }
                }
                else
                {
                    await UniTask.Yield();
                }
            }
            _ChangeAsync(false);
            if (isCanceled)
            {
                return;
            }
            OnShowAnimComplete();
            OnShowCallBack?.Invoke(id, _paramVo, checkStack);
        }

        protected virtual void ToOverwrite()
        {
            foreach (var component in Components)
            {
                (component as IUISetParam).SetParam(_paramVo);
                component.ToOverwrite();
            }
            OnOverwrite();
        }
        protected virtual void OnOverwrite()
        {
        }
        protected virtual void OnShow()
        {
        }

        protected virtual void OnShowAnimComplete()
        {
        }

        protected virtual void OnAddEvent()
        {

        }

        protected virtual void ToHide(bool isAnim, bool checkStack, CancellationTokenSource token)
        {
            (this as IUISetParam).SetParam(null);
            ShowAnim?.Stop();
            if (isAnim && HideAnim != null)
            {
                State = UIState.HideAnim;
                HideAnim?.SetToStart();
                _onHideAnimComplete ??= OnHideAnimCompleteInternal;
                HideAnim?.Play(_onHideAnimComplete);
            }
            else
            {
                State = UIState.Hide;
                HideAnim?.SetToEnd();
            }

            foreach (var component in Components)
            {
                component.ToHide(isAnim, checkStack, token);
            }

            OnHide();
            RemoveTag();
            _CheckHide(token).Forget();
        }

        private void OnHideAnimCompleteInternal()
        {
            State = UIState.Hide;
        }

        async UniTaskVoid _CheckHide(CancellationTokenSource token)
        {
            _ChangeAsync(true);
            bool isCanceled = false;
            while (State != UIState.Hide || Parent is { State: UIState.HideAnim })
            {
                if (token != null && !isCanceled)
                {
                    isCanceled = await UniTask.Yield(token.Token).SuppressCancellationThrow();
                    if (isCanceled)
                    {
                        HideAnim?.SetToEnd();
                        State = UIState.Hide;
                    }
                }
                else
                {
                    await UniTask.Yield();
                }
            }
            _ChangeAsync(false);
            if (isCanceled)
            {
                return;
            }
            OnHideAnimComplete();
            OnHideCallBack?.Invoke();
        }

        /// <summary>
        /// 注意！！！
        /// 不要在这里注册定时器或是监听事件。
        /// 在这里注册的定时器或是事件，都会流程给清空的
        /// </summary>
        protected virtual void OnHide()
        {
        }
        /// <summary>
        /// 注意！！！
        /// 不要在这里注册定时器或是监听事件。
        /// 在这里注册的定时器或是事件，都会流程给清空的
        /// </summary>
        protected virtual void OnHideAnimComplete()
        {
        }

        protected void ToDispose(bool isDisposeGObject = true)
        {
            _event.Release();
            _event = null;
            OnShowCallBack = null;
            OnHideCallBack = null;
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

        protected virtual void OnDispose()
        {
        }


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

        protected void AddItemClick(GList list, EventCallback1 fn)
        {
            if (list == null) return;
            AddEvent(list.onClickItem, fn);
        }

        protected void AddItemClick(GList list, EventCallback0 fn)
        {
            if (list == null) return;
            AddEvent(list.onClickItem, fn);
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