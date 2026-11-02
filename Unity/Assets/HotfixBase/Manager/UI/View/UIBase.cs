using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using static Ux.UIMgr;

namespace Ux
{
    public abstract class UIBase : UIObject, IUI, IUIAsync
    {
        private string _resAddress;

        /// <summary>
        /// UI 资源地址。默认按类型名推导为 UI_{类名}，与 YooAsset 的
        /// AddressByGroupAndFileName 规则一致（分组 UI + Prefab 名）。
        /// 需要复用其他 Prefab 时在子类重写本属性。
        /// </summary>
        protected virtual string ResAddress
        {
            get
            {
                _resAddress ??= $"UI_{GetType().Name}";
                return _resAddress;
            }
        }

        public virtual int HideDestroyTime => 60;
        public virtual UIType Type => UIType.Normal;
        public virtual UIBlur Blur => UIBlur.None;
        public virtual UIFocusMode FocusMode => UIFocusMode.Auto;
        public virtual bool CanFocus => FocusMode != UIFocusMode.None && Visible;

        void IUI.NotifyFocusEnter() => OnFocusEnter();
        void IUI.NotifyFocusExit() => OnFocusExit();
        protected virtual void OnFocusEnter() { }
        protected virtual void OnFocusExit() { }

        private CallbackData? _cbData;
        private int _instanceId;

        public virtual void InitData(IUIData data, CallbackData initData, int instanceId)
        {
            _instanceId = instanceId;
            Data = data;
            _cbData = initData;
            Init(CreateGameObject());
            OnHideCallback += HideInternal;
            OnShowCallback += ShowInternal;
        }

        public virtual void AddChild(UITabView child) { }

        protected virtual GameObject CreateGameObject()
        {
            // 默认实现按类型名推导，但子类可以重写 ResAddress，仍需防御空地址。
            if (string.IsNullOrEmpty(ResAddress))
                throw new InvalidOperationException($"UI 资源地址为空：{GetType().FullName}");

            // 加载失败直接抛异常，由 _ShowAsync 的 catch 回滚，
            // 避免界面对象带着空 GameObject 继续走显示流程。
            var gameObject = ResMgr.Ins.LoadAsset<GameObject>(ResAddress);
            if (gameObject == null)
                throw new InvalidOperationException($"UI 资源加载失败：{ResAddress}");
            return gameObject;
        }

        public bool Visible
        {
            get => GameObject != null && GameObject.activeSelf;
            set
            {
                if (GameObject != null && GameObject.activeSelf != value)
                    GameObject.SetActive(value);
            }
        }

        public int ID => _instanceId;
        public int TypeId => Data?.ID ?? 0;
        public string Name => Data?.Name;
        public IUIData Data { get; private set; }

        protected void HideSelf() => Hide(true);
        public virtual void Hide(bool isAnim = true) => UIMgr.Ins.Hide(ID, isAnim);

        protected virtual void AddToStage() { }

        protected virtual void RemoveToStage()
        {
            if (GameObject != null)
                GameObject.SetActive(false);
        }

        private enum QueuedTransitionKind
        {
            None,
            Show,
            Hide
        }

        private bool _async;
        private int _asyncVersion;
        private int _transitionVersionSeed;
        private Action _queuedTransition;
        private AutoResetUniTaskCompletionSource _queuedShowTask;

        void IUIAsync.Change(bool value, int version)
        {
            if (value)
            {
                _async = true;
                _asyncVersion = version;
                return;
            }

            if (_asyncVersion != version)
                return;

            _async = false;
            _asyncVersion = 0;
            RunQueuedTransition();
        }

        private int NextTransitionVersion() => ++_transitionVersionSeed;

        private void QueueTransition(QueuedTransitionKind kind, Action transition, AutoResetUniTaskCompletionSource showTask = null)
        {
            _queuedShowTask?.TrySetResult();
            _queuedTransition = transition;
            _queuedShowTask = showTask;
        }

        private void ClearQueuedTransition()
        {
            _queuedShowTask?.TrySetResult();
            _queuedTransition = null;
            _queuedShowTask = null;
        }

        private void RunQueuedTransition()
        {
            var transition = _queuedTransition;
            _queuedTransition = null;
            _queuedShowTask = null;
            transition?.Invoke();
        }

        UniTask IUI.DoShow(bool isAnim, int id, IUIParam param, bool checkStack, UIShowReason reason)
        {
            var task = AutoResetUniTaskCompletionSource.Create();
            switch (State)
            {
                case UIState.Show:
                case UIState.ShowAnim:
                    if (reason == UIShowReason.Reactivate)
                    {
                        if (param != null && id == TypeId)
                        {
                            (this as IUISetParam).SetParam(param);
                            ToOverwrite();
                        }
                        OnActivate();
                        EventMgr.Ins.Run(MainEventType.UI_ACTIVATE, ID);
                        EventMgr.Ins.Run(MainEventType.UI_ACTIVATE, GetType());
                        task.TrySetResult();
                        return task.Task;
                    }

                    if (id == TypeId)
                    {
                        (this as IUISetParam).SetParam(param);
                        ToOverwrite();
                    }
                    ShowInternal(id, param, checkStack);
                    task.TrySetResult();
                    return task.Task;

                case UIState.Hide:
                    AddToStage();
                    break;
            }

            if (_async && State != UIState.HideAnim)
                QueueTransition(QueuedTransitionKind.Show, DoShow, task);
            else
            {
                if (State == UIState.HideAnim)
                    ClearQueuedTransition();
                DoShow();
            }

            void DoShow()
            {
                (this as IUISetParam).SetParam(param);
                _showVersion = NextTransitionVersion();
                ToShow(isAnim, id, checkStack, _showVersion);
                task.TrySetResult();
            }

            return task.Task;
        }

        private void ShowInternal(int id, IUIParam param, bool checkStack)
        {
            _cbData?.showCb?.Invoke(this, param, checkStack);
            EventMgr.Ins.Run(MainEventType.UI_SHOW, ID);
            EventMgr.Ins.Run(MainEventType.UI_SHOW, GetType());
        }

        void IUI.DoHide(bool isAnim, bool checkStack)
        {
            if (State is UIState.Hide or UIState.HideAnim)
                return;

            if (_async && State != UIState.ShowAnim)
                QueueTransition(QueuedTransitionKind.Hide, DoHide);
            else
            {
                if (State == UIState.ShowAnim)
                    ClearQueuedTransition();
                DoHide();
            }

            void DoHide()
            {
                _hideVersion = NextTransitionVersion();
                ToHide(isAnim, checkStack, _hideVersion);
            }
        }

        private void HideInternal()
        {
            RemoveToStage();
            _cbData?.hideCb?.Invoke(this);
        }

        void IUI.Dispose() => ToDispose(true);

        protected override void OnDispose()
        {
            Data = null;
            _cbData = null;
            _async = false;
            _asyncVersion = 0;
            _transitionVersionSeed = 0;
            ClearQueuedTransition();
        }
    }
}
