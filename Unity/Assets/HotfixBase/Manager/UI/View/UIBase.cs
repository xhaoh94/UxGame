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
        public virtual UIType Type => UIType.Stackable;
        public virtual UIBlurMode Blur => UIBlurMode.Off;
        public virtual UIFocusMode FocusMode => UIFocusMode.Auto;
        public virtual bool CanFocus => FocusMode != UIFocusMode.None && Visible;

        void IUI.NotifyFocusEnter() => OnFocusEnter();
        void IUI.NotifyFocusExit() => OnFocusExit();
        protected virtual void OnFocusEnter() { }
        protected virtual void OnFocusExit() { }

        private int _instanceId;

        public virtual void InitData(int instanceId, IUIData data)
        {
            _instanceId = instanceId;
            Data = data;
            Init(CreateGameObject());
            OnHideCallback += HideInternal;
            OnShowCallback += ShowInternal;
        }

        public virtual void AddChild(UITabView child) { }

        protected virtual GameObject CreateGameObject()
        {            
            if (string.IsNullOrEmpty(ResAddress))
                throw new InvalidOperationException($"UI 资源地址为空：{GetType().FullName}");
            
            var gameObject = ResMgr.Ins.LoadAsset<GameObject>(ResAddress);
            if (gameObject == null)
                throw new InvalidOperationException($"UI 资源加载失败：{ResAddress}");
            
            gameObject.SetLayer(LayerMask.NameToLayer(Layers.UI));
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

        // —— SSC 上屏排序 ——
        /// <summary>界面上屏后所在的 Sorting Layer（由 RegisterStage 写入）。</summary>
        internal string StageSortingLayerName;
        /// <summary>层内显示顺序 × 100，重排时由 UIMgr 写入。</summary>
        internal int StageSortingOrder;
        /// <summary>上屏时挂的 Canvas（RegisterStage 写入），重排时直接写它的 sortingOrder。</summary>
        internal Canvas StageCanvas;
        private bool _registeredToStage = false;

        /// <summary>界面挂到哪个层；Root 表示不上屏（子页面自己挂到父级容器下）。</summary>
        protected virtual UILayer Layer => UILayer.Root;

        /// <summary>由 UIMgr 在层内重排时调用：写回新 order 并通知子组件（模型/特效跟随）。</summary>
        internal void ApplyStageSortingOrder(int order)
        {
            StageSortingOrder = order;
            if (StageCanvas != null)
                StageCanvas.sortingOrder = order;
            OnStageSortingChanged(order);
        }

        /// <summary>把排序变化分发给子组件（UIModel 等按它对齐 SortingGroup）。</summary>
        internal override void OnStageSortingChanged(int order)
        {
            var components = Components;
            for (int i = 0; i < components.Count; i++)
            {
                components[i].OnStageSortingChanged(order);
            }
        }

        private void RegisterToStage()
        {
            if (_registeredToStage) return;
            UIMgr.Ins.RegisterStage(ID, Layer);
            _registeredToStage = true;
        }
        private void UnregisterStage()
        {
            if (!_registeredToStage) return;
            UIMgr.Ins.UnregisterStage(ID, Layer);
            _registeredToStage = false;
        }

        protected void HideSelf() => Hide(true);
        public virtual void Hide(bool isAnim = true) => UIMgr.Ins.Hide(ID, isAnim);

        protected virtual void AddToStage()
        {
            var layer = Layer;
            if (layer == UILayer.Root) return;

            GameObject.transform.SetParent(UIMgr.Ins.GetUILayer(layer), false);
            Visible = true;            
            RegisterToStage();
        }

        protected virtual void RemoveToStage()
        {
            Visible = false;            
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
            UIMgr.Ins.NotifyShown(this, param, checkStack);
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
            UnregisterStage();
            UIMgr.Ins.NotifyHidden(this);
        }

        void IUI.Dispose() => ToDispose(true);

        protected override void OnDispose()
        {
            UnregisterStage();
            Data = null;
            _async = false;
            _asyncVersion = 0;
            _transitionVersionSeed = 0;
            ClearQueuedTransition();
        }
    }
}
