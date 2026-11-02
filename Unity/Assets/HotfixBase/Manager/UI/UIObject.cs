using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

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
        void Change(bool b, int version);
    }

    public interface IUISetParam
    {
        void SetParam(IUIParam param);
    }

    public class UIObject : IUISetParam
    {
        private UIEvent _event;
        private List<UIObject> _components;
        private UIState _state = UIState.Hide;
        private IUIParam _paramVo;

        public List<UIObject> Components => _components ??= new List<UIObject>();
        public virtual UIState State
        {
            get => _state;
            private set => _state = value;
        }

        public virtual UIObject Parent { get; private set; }
        public GameObject GameObject { get; private set; }
        public Transform Transform => GameObject != null ? GameObject.transform : null;

        protected int _showVersion;
        protected int _hideVersion;
        protected int _transitionVersion;

        protected virtual IUIAnim ShowAnim { get; set; }
        protected virtual IUIAnim HideAnim { get; set; }

        protected Action<int, IUIParam, bool> OnShowCallback;
        protected Action OnHideCallback;

        public void AddComponent(UIObject component)
        {
            if (component != null)
                Components.Add(component);
        }

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

        protected T ParentAs<T>() where T : UIObject => Parent as T;

        protected void Init(GameObject gameObject, UIObject parent = null)
        {
            if (gameObject == null || GameObject != null)
                return;

            _event ??= new UIEvent();
            GameObject = gameObject;
            Parent = parent;
            CreateChildren();
            OnInit();
        }

        protected virtual void OnInit() { }
        protected virtual void CreateChildren() { }

        protected virtual void ToShow(bool isAnim, int id, bool checkStack, int showVersion)
        {
            _transitionVersion = showVersion;
            _showVersion = showVersion;
            HideAnim?.Stop();

            if (isAnim && ShowAnim != null)
            {
                State = UIState.ShowAnim;
                ShowAnim.SetToStart();
                ShowAnim.Play(() => OnShowAnimCompleteInternal(showVersion));
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
                    component._transitionVersion = showVersion;
                    component.ToShow(isAnim, id, checkStack, showVersion);
                }
            }

            OnShow();
            CheckShow(id, checkStack, showVersion).Forget();
        }

        private void OnShowAnimCompleteInternal(int version)
        {
            if (IsTransitionCurrent(version))
                State = UIState.Show;
        }

        private void ChangeAsync(bool value, int version)
        {
            (this as IUIAsync)?.Change(value, version);
        }

        private async UniTaskVoid CheckShow(int id, bool checkStack, int version)
        {
            var hideVersion = GetHideVersion();
            ChangeAsync(true, version);
            while (State != UIState.Show || Parent is { State: UIState.ShowAnim })
            {
                await UniTask.Yield();
                if (hideVersion != GetHideVersion() || State is UIState.Hide or UIState.HideAnim)
                {
                    ShowAnim?.Stop();
                    return;
                }
                if (version != GetShowVersion())
                {
                    ShowAnim?.SetToEnd();
                    State = UIState.Show;
                    ChangeAsync(false, version);
                    return;
                }
            }

            ChangeAsync(false, version);
            if (!IsTransitionCurrent(version) || State != UIState.Show)
                return;

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
        protected virtual void OnActivate() { }
        protected virtual void OnShowAnimComplete() { }
        protected virtual void OnAddEvent() { }

        protected virtual void ToHide(bool isAnim, bool checkStack, int hideVersion)
        {
            _transitionVersion = hideVersion;
            _hideVersion = hideVersion;
            (this as IUISetParam).SetParam(null);
            ShowAnim?.Stop();

            if (isAnim && HideAnim != null)
            {
                State = UIState.HideAnim;
                HideAnim.SetToStart();
                HideAnim.Play(() => OnHideAnimCompleteInternal(hideVersion));
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
                    component._transitionVersion = hideVersion;
                    component.ToHide(isAnim, checkStack, hideVersion);
                }
            }

            OnHide();
            RemoveTag();
            CheckHide(hideVersion).Forget();
        }

        private void OnHideAnimCompleteInternal(int version)
        {
            if (IsTransitionCurrent(version))
                State = UIState.Hide;
        }

        private async UniTaskVoid CheckHide(int version)
        {
            var showVersion = GetShowVersion();
            ChangeAsync(true, version);
            while (State != UIState.Hide || Parent is { State: UIState.HideAnim })
            {
                await UniTask.Yield();
                if (showVersion != GetShowVersion() || State is UIState.Show or UIState.ShowAnim)
                {
                    HideAnim?.Stop();
                    return;
                }
                if (version != GetHideVersion())
                {
                    HideAnim?.SetToEnd();
                    State = UIState.Hide;
                    ChangeAsync(false, version);
                    return;
                }
            }

            ChangeAsync(false, version);
            if (!IsTransitionCurrent(version) || State != UIState.Hide)
                return;
            OnHideCallback?.Invoke();
        }

        protected int GetShowVersion() => _showVersion;
        protected int GetHideVersion() => _hideVersion;
        protected bool IsTransitionCurrent(int version) => _transitionVersion == version;
        protected virtual void OnHide() { }

        protected void ToDispose(bool destroyGameObject = true)
        {
            _event?.Release();
            _event = null;
            OnShowCallback = null;
            OnHideCallback = null;

            if (_components != null)
            {
                foreach (var component in _components)
                    component.ToDispose(destroyGameObject);
                _components.Clear();
                _components = null;
            }

            OnDispose();
            if (destroyGameObject && GameObject != null)
                UnityEngine.Object.Destroy(GameObject);
            GameObject = null;
            Parent = null;
        }

        protected virtual void OnDispose() { }

        protected void AddClick(GameObject target, Action<UIEventContext> callback) => _event?.AddClick(target, callback);
        protected void AddClick(GameObject target, Action callback) => _event?.AddClick(target, callback);

        /// <summary>
        /// 设置点击缩放参数。默认已在 AddClick 时挂上（0.92 / 0.08s）。
        /// </summary>
        protected void SetClickScale(GameObject target, float pressedScale = 0.92f, float duration = 0.08f)
            => UIButtonScale.Configure(target, true, pressedScale, duration);

        /// <summary>关闭某个按钮的点击缩放。</summary>
        protected void DisableClickScale(GameObject target)
            => UIButtonScale.Configure(target, false, 1f, 0f);
        protected void AddMultipleClick(GameObject target, Action<UIEventContext> callback, int count = 2, float gap = 0.3f) => _event?.AddMultipleClick(target, callback, count, gap);
        protected void AddLongPress(GameObject target, Func<bool> callback, float delay = 0.2f, int loop = 0, int radius = 50) => _event?.AddLongPress(target, callback, 0f, delay, loop, radius);
        protected void AddLongPress(GameObject target, float first, Func<bool> callback, float delay = 0.2f, int loop = 0, int radius = 50) => _event?.AddLongPress(target, callback, first, delay, loop, radius);
        protected void AddItemClick(UIList list, Action<IItemRenderer> callback) => list?.AddItemClick(callback);

        protected void RemoveTag()
        {
            _event?.RemoveEvents();
            EventMgr.Ins.OffTag(this);
            TimeMgr.Ins.RemoveTag(this);
        }
    }
}
