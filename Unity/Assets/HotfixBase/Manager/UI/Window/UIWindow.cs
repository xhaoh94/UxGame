using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ux
{
    /// <summary>
    /// 窗口基类。只承载「窗口身份 + 布局状态 + 能力声明 + 激活入口」。
    ///
    /// 交互分两条路径：
    /// 1. **框架预制**：窗口 Prefab 里放了 <see cref="UIWindowFrame"/> 节点（如 CommonWindowFrame），
    ///    CodeGen 生成 `new CommonWindowFrame(node, this)`，框架按能力装配拖拽/缩放；
    /// 2. **退化路径**：没放框架时，本类用窗口主体（或窗口根）当热区自行装配拖拽。
    ///
    /// 拖拽/缩放本身是纯算法类（<see cref="UIWindowDrag"/>），不继承 UIObject、不进节点树。
    /// </summary>
    public abstract class UIWindow : UIBase, IUIStaged
    {
        protected override UILayer Layer => UILayer.Window;

        /// <summary>
        /// 窗口不参与 UIType 栈：多窗口并存，隐藏一个窗口不该把别的界面重新 Show 一遍。
        /// 窗口之间的层序由 <see cref="UIWindowTier"/> + SortingLayer 决定。
        /// </summary>
        public sealed override UIType Type => UIType.NonStackable;

        /// <summary>层叠级别（同层内分组，越大越靠上）。</summary>
        public virtual UIWindowTier Tier => UIWindowTier.Normal;

        private UIWindowTier? _stageTierOverride;

        /// <summary>实际层叠级别：运行时改写优先（AlwaysOnTop 开关），否则用声明值。</summary>
        UIWindowTier IUIStaged.WindowTier => _stageTierOverride ?? Tier;

        /// <summary>给业务/服务读的公开门面。</summary>
        public UIWindowTier WindowTier => _stageTierOverride ?? Tier;

        void IUIStaged.SetWindowTier(UIWindowTier tier) => _stageTierOverride = tier;

        /// <summary>布局状态。**不是** <see cref="UIObject.State"/>（UIState 过渡机）：最小化不隐藏、不进缓存。</summary>
        public WindowState LayoutState { get; private set; } = WindowState.Normal;

        /// <summary>
        /// 声明窗口支持哪些交互。默认「可拖 + 松手贴边」。
        /// 业务按需覆盖，例如只读窗口返回 <see cref="UIWindowCapability.None"/>。
        /// </summary>
        protected virtual UIWindowCapability Capabilities =>
            UIWindowCapability.Drag | UIWindowCapability.Snap | UIWindowCapability.Resize | UIWindowCapability.Maximize | UIWindowCapability.Minimize;

        /// <summary>给框架/服务读的只读门面（protected 属性外部无法读）。</summary>
        internal UIWindowCapability WindowCapabilities => Capabilities;

        /// <summary>
        /// 拖拽/吸附的限制区域节点：优先生成器绑定的 __winDragArea，
        /// 其次用全局默认区（UIWindowService.DefaultDragArea），都为 null 时用 Window 层容器（全屏）。
        /// 子类可覆盖成代码计算出来的区域。
        /// </summary>
        protected virtual RectTransform DragArea
        {
            get
            {
                if (__winDragArea != null)
                    return __winDragArea;

                return UIMgr.Ins.Windows != null ? UIMgr.Ins.Windows.DefaultDragArea : null;
            }
        }

        internal RectTransform WindowDragArea => DragArea;

        #region Prefab 绑定
        /// <summary>
        /// 窗口主体（拖动/缩放的作用对象）。由生成器按 WindowBody 槽位绑定到 panelBody；
        /// 没绑时用窗口根节点（见 <see cref="BodyRect"/>）。
        /// 标题栏/标题/按钮属于 UIWindowFrame，不在这里。
        /// </summary>
        protected virtual RectTransform __winBody { get; } = null;

        /// <summary>
        /// 拖动/吸附的限制范围。由生成器按 WindowDragArea 槽位绑定；
        /// 没绑时用父容器（Window 层全屏）。
        /// </summary>
        protected virtual RectTransform __winDragArea { get; } = null;
        #endregion

        /// <summary>
        /// 窗口主体区域（拖动/缩放/最大化作用对象）。
        /// **硬性要求**：必须在窗口 Prefab 里配好 panelBody（或通过槽位指定），不再兼容“根就是 Body”。
        /// </summary>
        internal RectTransform BodyRect => __winBody;

        private UIWindowDrag _fallbackDrag;
        private UIWindowResize _fallbackResize;
        private CanvasGroup _canvasGroup;
        private Vector2 _restoreAnchoredPosition;
        private Vector2 _restoreSizeDelta;
        private readonly Vector3[] _stateCorners = new Vector3[4];
        private bool _layoutDirty;
        private bool _restoringLayout;
        private bool _occluded;
        private bool _hasInstanceSlot;
        private bool _stageRootChecked;
        private WindowLayout _layout;

        /// <summary>布局是否被拖拽/缩放改脏。</summary>
        public bool IsLayoutDirty => _layoutDirty;

        protected sealed override void OnInit()
        {
            var caps = WindowCapabilities;

            // 无窗口模式不进注册表：不参与 LRU、不占实例序号、不上任务栏。
            // 超限被拒时同样拿不到实例序号，此时禁止读写布局存档 —— GetSlot 会返回 0，
            // 写进去就覆盖同类首个窗口的存档。
            if (caps != UIWindowCapability.None)
                _hasInstanceSlot = UIMgr.Ins.Windows.Register(this);

            OnWindowConfigured();          // 业务显式定制（可 BindDrag 指定热区）
            BindFallbackInteractions();    // 没有框架预制时退化为内置拖拽
        }

        /// <summary>
        /// 业务钩子。不要 override <see cref="UIObject.OnInit"/>（基类已 sealed），
        /// 也不要往自动生成的 CreateChildren 里写代码。
        /// </summary>
        protected virtual void OnWindowConfigured() { }

        /// <summary>
        /// 退化装配：窗口 Prefab 里没有 <see cref="UIWindowFrame"/> 节点时，用标题栏槽位（或整窗）拖拽。
        /// </summary>
        protected virtual void BindFallbackInteractions()
        {
            var caps = WindowCapabilities;

            // 无窗口模式（手游把窗口概念去掉）：不要求 Body、不装配任何交互、不报错。
            // 界面照常走 UIBase 的显隐流程，当作普通页面用。
            if (caps == UIWindowCapability.None)
                return;

            // 有交互能力时 Body 是硬性要求：没有它拖拽/缩放/最大化都没有作用对象。
            if (BodyRect == null)
            {
                Log.Error("窗口未绑定 body（__winBody / panelBody），跳过默认交互。Window[{0}]。" +
                          "请在 Prefab 里加 panelBody 并在生成面板绑定「窗口主体」；" +
                          "如果这是手游全屏页面，请把 Capabilities 声明为 None。", GetType().Name);
                return;
            }

            // 框架预制已经接管（CreateChildren 在 OnInit 之前执行，所以这里能看到它）。
            if (HasComponent<UIWindowFrame>())
                return;

            // 业务已经显式 BindDrag 过，不再覆盖。
            if (_fallbackDrag != null)
                return;

            if (caps.HasFlag(UIWindowCapability.Drag))
                BindDrag(null);

            if (caps.HasFlag(UIWindowCapability.Resize))
                BindResize();
        }

        /// <summary>
        /// 用指定节点作为拖拽热区（传 null 时自动取标题栏槽位，没有则整窗）。
        /// 由窗口统一管理算法实例的生命周期，业务不要在外部自己 new。
        /// </summary>
        protected void BindDrag(GameObject handle)
        {
            _fallbackDrag?.Dispose();
            _fallbackDrag = new UIWindowDrag(this, handle)
            {
                SnapEnabled = WindowCapabilities.HasFlag(UIWindowCapability.Snap),
                DragArea = DragArea,
            };
        }

        /// <summary>
        /// 装配缩放（传 null 时运行时生成 8 个透明把手）。
        /// 同样由窗口统一管理生命周期。
        /// </summary>
        protected void BindResize(GameObject handleRoot = null)
        {
            _fallbackResize?.Dispose();
            _fallbackResize = new UIWindowResize(this, handleRoot);
        }

        private bool HasComponent<T>() where T : UIObject
        {
            var components = Components;
            for (int i = 0; i < components.Count; i++)
            {
                if (components[i] is T)
                    return true;
            }
            return false;
        }

        // —— 生命周期 ——

        protected override void AddToStage()
        {
            base.AddToStage();

            // 必须先校验再算比例：坐标系不对时算出来的比例不报错，只是整体偏。
            ValidateStageRoot();

            // 先上屏，父容器尺寸才成立，布局比例才能算准。
            RestoreLayout();

            // 观察者（任务栏/切换器）在布局落定后再收事件。
            UIMgr.Ins.Windows.NotifyShown(this);

            // 首次上屏也计算一次遮挡（Register 时窗口还没上屏，会被跳过）。
            UIMgr.Ins.Windows.RefreshOcclusion();
        }

        /// <summary>
        /// 下屏后重算遮挡：被本窗口完全盖住的下层会关掉 Canvas，不重算就留一片空白。
        /// 放在这里而不是 ToHide —— 带隐藏动画时 Visible 要到这一步才翻成 false，
        /// 在 ToHide 里算，被遮的下层仍然认为上层可见。
        /// </summary>
        protected override void RemoveToStage()
        {
            base.RemoveToStage();
            UIMgr.Ins.Windows.NotifyHidden(this);
            UIMgr.Ins.Windows.RefreshOcclusion();
        }

        protected override void ToShow(bool isAnim, int id, bool checkStack, int showVersion)
        {
            // base 会先 RemoveTag（清空 UIEvent 订阅）再走节点/动画流程，
            // 所以退化拖拽的事件重绑必须放在 base 之后。
            base.ToShow(isAnim, id, checkStack, showVersion);
            _fallbackDrag?.Rebind();
            _fallbackResize?.Rebind();
        }

        protected override void ToHide(bool isAnim, bool checkStack, int hideVersion)
        {
            _fallbackDrag?.Reset();
            _fallbackResize?.Reset();
            base.ToHide(isAnim, checkStack, hideVersion);
        }

        protected override void OnDispose()
        {
            // 销毁前落盘一次（此时 GameObject 还在）。
            SaveLayoutInternal();

            _fallbackDrag?.Dispose();
            _fallbackDrag = null;
            _fallbackResize?.Dispose();
            _fallbackResize = null;
            _canvasGroup = null;

            // 直接销毁（没走 Hide）时补一次关闭事件；已经关过的窗口这里是空操作。
            UIMgr.Ins.Windows.NotifyHidden(this);
            UIMgr.Ins.Windows.Unregister(ID);
            base.OnDispose();
        }

        // —— 供交互算法使用的内部能力 ——

        /// <summary>供算法/框架注册点击（UIObject 的 AddClick 是 protected）。</summary>
        internal void RegisterClick(GameObject target, Action onClick)
            => AddClick(target, onClick);

        /// <summary>供算法类注册拖拽事件（UIObject 的 AddDrag 是 protected）。</summary>
        internal void RegisterDrag(GameObject target, Action<UIEventContext> onDrag,
            Action<UIEventContext> onBegin, Action<UIEventContext> onEnd)
            => AddDrag(target, onDrag, onBegin, onEnd);

        /// <summary>供算法类注册按下事件（点一下即置顶）。</summary>
        internal void RegisterPointerDown(GameObject target, Action<UIEventContext> onDown)
            => AddPointerDown(target, onDown);

        /// <summary>供框架注册多击（双击标题栏最大化）。</summary>
        internal void RegisterMultipleClick(GameObject target, Action<UIEventContext> onClick,
            int count = 2, float gap = 0.25f)
            => AddMultipleClick(target, onClick, count, gap);

        /// <summary>拖拽/缩放/状态切换后调用：更新内存布局、写盘、并重算遮挡（M6）。</summary>
        internal void NotifyLayoutChanged()
        {
            _layoutDirty = true;
            SaveLayoutInternal();
            UIMgr.Ins.Windows.RefreshOcclusion();
        }

        /// <summary>
        /// 所有布局比例的坐标基准（Body 的父节点）。存档、遮挡、拖拽 clamp 全按它的 rect 归一化，
        /// **它必须铺满窗口层容器**，见 <see cref="ValidateStageRoot"/>。
        /// </summary>
        internal RectTransform LayoutSpace => BodyRect != null ? BodyRect.parent as RectTransform : null;

        /// <summary>
        /// 窗口根必须是全屏 stretch（anchorMin=0,0 / anchorMax=1,1 / 四边 offset=0）。
        /// 不满足时比例会按错的尺寸缩放，不抛异常只是偏 —— 所以上屏时校验一次并报错。
        /// </summary>
        private void ValidateStageRoot()
        {
            if (_stageRootChecked)
                return;

            _stageRootChecked = true;

            var space = LayoutSpace;
            var layer = UIMgr.Ins.GetUILayer(UILayer.Window);
            if (space == null || layer == null)
                return;

            var spaceRect = space.rect;
            var layerRect = layer.rect;
            if (Mathf.Abs(spaceRect.width - layerRect.width) <= 1f &&
                Mathf.Abs(spaceRect.height - layerRect.height) <= 1f)
                return;

            Log.Error("窗口根的坐标基准不是全屏：Body 父节点 [{0}] {1}x{2}，窗口层容器 [{3}] {4}x{5}。" +
                      "窗口根必须 stretch 铺满，否则布局存档、遮挡判定、拖拽范围都会按错误比例计算。Window[{6}]",
                space.name, spaceRect.width, spaceRect.height,
                layer.name, layerRect.width, layerRect.height, GetType().Name);
        }

        // —— 遮挡 / 射线（M6）——

        /// <summary>窗口是否完全不透明。不透明才能遮住下层窗口；透明 HUD 可覆盖为 false。</summary>
        public virtual bool IsOpaque => true;

        /// <summary>是否被上方窗口完全遮挡（M6）。</summary>
        public bool IsOccluded => _occluded;

        /// <summary>由 UIWindowService 在层序/布局变化后调用。</summary>
        internal void SetOccluded(bool occluded)
        {
            if (_occluded == occluded)
                return;

            _occluded = occluded;
            ApplyRaycasts();
            OnOcclusionChanged(occluded);
        }

        /// <summary>
        /// 遮挡变化回调：业务在这里暂停/恢复自己的动画、列表刷新、视频等。
        /// 框架只管射线，不管业务内容。
        /// </summary>
        protected virtual void OnOcclusionChanged(bool occluded) { }

        /// <summary>取窗口在父容器局部系里的矩形（遮挡判定用）。</summary>
        internal Rect GetStageRect()
        {
            var body = BodyRect;
            var parent = LayoutSpace;
            if (parent == null)
                return default;

            body.GetWorldCorners(_stateCorners);
            var bl = parent.InverseTransformPoint(_stateCorners[0]);
            var tr = parent.InverseTransformPoint(_stateCorners[2]);
            return Rect.MinMaxRect(bl.x, bl.y, tr.x, tr.y);
        }

        /// <summary>
        /// 可见性开关 = 未被遮挡 且 未最小化。
        /// 同时控制 Canvas / GraphicRaycaster / blocksRaycasts：
        /// 完全遮挡或最小化时整个窗口不渲染、不参与射线，省 DrawCall 与射线检测。
        /// </summary>
        private void ApplyRaycasts()
        {
            var allow = !_occluded && LayoutState != WindowState.Minimized;

            if (BodyRect != null)
                GetCanvasGroup(BodyRect).blocksRaycasts = allow;

            if (GameObject != null)
            {
                var canvas = GameObject.GetComponent<Canvas>();
                if (canvas != null)
                    canvas.enabled = allow;

                var raycaster = GameObject.GetComponent<GraphicRaycaster>();
                if (raycaster != null)
                    raycaster.enabled = allow;
            }
        }

        // —— 布局持久化（M3）——

        /// <summary>把保存的比例矩形应用到 Body；上次是 Minimized 时不恢复（避免打开就消失）。</summary>
        private void RestoreLayout()
        {
            if (BodyRect == null || WindowCapabilities == UIWindowCapability.None)
                return;

            _restoringLayout = true;
            try
            {
                // 没有实例序号（超限未登记）时不碰存档：GetSlot 恒为 0，读写会污染同类首个窗口。
                var hasSaved = _hasInstanceSlot && UIMgr.Ins.Windows.HasSavedLayout(this);
                _layout = _hasInstanceSlot
                    ? UIMgr.Ins.Windows.LoadLayout(this, CaptureRect())
                    : WindowLayout.CreateDefault(0, WindowTier, CaptureRect());

                LayoutState = WindowState.Normal;
                ApplyRect(_layout.Normal);

                // 多实例：同类第 n 个（slot > 0）首次打开时级联偏移，避免完全重叠。
                if (!hasSaved && _layout.InstanceSlot > 0)
                {
                    var offset = 24f * _layout.InstanceSlot;
                    BodyRect.anchoredPosition += new Vector2(offset, -offset);
                    _layout.Normal = CaptureRect();
                }
                _restoreAnchoredPosition = BodyRect.anchoredPosition;
                _restoreSizeDelta = BodyRect.sizeDelta;

                if (_layout.Tier != WindowTier)
                    SetTier(_layout.Tier);

                if (_layout.State != WindowState.Normal)
                    SetLayoutState(_layout.State);
            }
            finally
            {
                _restoringLayout = false;
            }
        }

        private void SaveLayoutInternal()
        {
            // 没有实例序号就落到 slot 0，会把同类首个窗口的存档写坏，干脆不存。
            if (_restoringLayout || BodyRect == null || !_hasInstanceSlot)
                return;
            if (WindowCapabilities == UIWindowCapability.None)
                return;

            if (_layout.Version == 0)
                _layout = UIMgr.Ins.Windows.LoadLayout(this, CaptureRect());

            if (LayoutState == WindowState.Normal)
                _layout.Normal = CaptureRect();

            _layout.State = LayoutState;
            _layout.Tier = WindowTier;
            _layout.InstanceSlot = UIMgr.Ins.Windows.GetSlot(ID);
            UIMgr.Ins.Windows.SaveLayout(this, _layout);
        }

        /// <summary>分辨率/窗口尺寸变化后重新应用保存的比例布局（由外部调用）。</summary>
        internal void ReapplyLayout()
        {
            if (BodyRect == null || WindowCapabilities == UIWindowCapability.None)
                return;

            RestoreLayout();
        }

        /// <summary>取 Body 在父容器里的比例矩形。</summary>
        private WindowRect CaptureRect()
        {
            var body = BodyRect;
            var parent = LayoutSpace;
            if (parent == null)
                return default;

            var parentRect = parent.rect;
            if (parentRect.width <= 0f || parentRect.height <= 0f)
                return default;

            body.GetWorldCorners(_stateCorners);
            var bl = parent.InverseTransformPoint(_stateCorners[0]);
            var tr = parent.InverseTransformPoint(_stateCorners[2]);

            return new WindowRect
            {
                Left = (bl.x - parentRect.xMin) / parentRect.width,
                Top = (parentRect.yMax - tr.y) / parentRect.height,
                Width = (tr.x - bl.x) / parentRect.width,
                Height = (tr.y - bl.y) / parentRect.height,
            };
        }

        /// <summary>把比例矩形转回像素并写入 Body。</summary>
        private void ApplyRect(in WindowRect rect)
        {
            var body = BodyRect;
            var parent = LayoutSpace;
            if (parent == null || !rect.IsValid)
                return;

            var parentRect = parent.rect;
            var width = rect.Width * parentRect.width;
            var height = rect.Height * parentRect.height;
            var xMin = parentRect.xMin + rect.Left * parentRect.width;
            var yMax = parentRect.yMax - rect.Top * parentRect.height;

            var area = Rect.MinMaxRect(xMin, yMax - height, xMin + width, yMax);
            body.sizeDelta = area.size;
            body.anchoredPosition = ToAnchoredPosition(body, area);
        }

        // —— 布局状态（Normal / Maximized / Minimized）——

        /// <summary>最小化：不可见、不挡射线，但仍算“显示中”（不进缓存、不重发 UI_SHOW）。</summary>
        public void Minimize() => SetLayoutState(WindowState.Minimized);

        /// <summary>最大化：铺满 DragArea（或父容器）。</summary>
        public void Maximize() => SetLayoutState(WindowState.Maximized);

        /// <summary>还原到最大化/最小化之前的矩形。</summary>
        public void Restore() => SetLayoutState(WindowState.Normal);

        public void SetLayoutState(WindowState next)
        {
            if (LayoutState == next)
                return;

            var body = BodyRect;
            if (body == null)
            {
                Log.Error("窗口状态切换失败：没有绑定 body。Window[{0}]", GetType().Name);
                return;
            }

            // 只在离开 Normal 时记录一次，保证 Maximized ↔ Minimized 之间切换不会覆盖原始矩形。
            if (LayoutState == WindowState.Normal)
            {
                _restoreAnchoredPosition = body.anchoredPosition;
                _restoreSizeDelta = body.sizeDelta;
            }

            ApplyLayoutState(next, body);
            LayoutState = next;
            ApplyRaycasts();
            NotifyLayoutChanged();
            OnWindowStateChanged(next);
        }

        /// <summary>状态变化回调（框架/业务可覆盖，例如切换最大化按钮图标）。</summary>
        protected virtual void OnWindowStateChanged(WindowState state) { }

        private void ApplyLayoutState(WindowState state, RectTransform body)
        {
            var canvasGroup = GetCanvasGroup(body);

            switch (state)
            {
                case WindowState.Maximized:
                    {
                        var area = GetMaximizeRect(body);
                        body.sizeDelta = area.size;
                        body.anchoredPosition = ToAnchoredPosition(body, area);

                        canvasGroup.alpha = 1f;
                        break;
                    }

                case WindowState.Minimized:
                    canvasGroup.alpha = 0f;
                    break;

                default:
                    body.anchoredPosition = _restoreAnchoredPosition;
                    body.sizeDelta = _restoreSizeDelta;
                    canvasGroup.alpha = 1f;
                    break;
            }
        }

        /// <summary>最大化目标矩形（坐标基准局部坐标）：优先 DragArea，否则基准本身。</summary>
        private Rect GetMaximizeRect(RectTransform body)
        {
            var parent = LayoutSpace;
            if (parent == null)
                return body.rect;

            var area = DragArea;
            // 限制区在 Body 子树内会跟着窗口变，最大化时不能用，退回父容器。
            if (area == null || area == body || area.IsChildOf(body))
                return parent.rect;

            area.GetWorldCorners(_stateCorners);
            var min = parent.InverseTransformPoint(_stateCorners[0]);
            var max = parent.InverseTransformPoint(_stateCorners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private Vector2 ToAnchoredPosition(RectTransform body, Rect area)
        {
            var parent = LayoutSpace;
            if (parent == null)
                return body.anchoredPosition;

            var parentRect = parent.rect;
            var anchor = new Vector2(
                Mathf.Lerp(parentRect.xMin, parentRect.xMax, body.anchorMin.x),
                Mathf.Lerp(parentRect.yMin, parentRect.yMax, body.anchorMin.y));

            var pivot = new Vector2(
                area.xMin + area.width * body.pivot.x,
                area.yMin + area.height * body.pivot.y);

            return pivot - anchor;
        }

        private CanvasGroup GetCanvasGroup(RectTransform body)
        {
            if (_canvasGroup != null)
                return _canvasGroup;

            _canvasGroup = body.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = body.gameObject.AddComponent<CanvasGroup>();

            return _canvasGroup;
        }

        // —— 对外能力 ——

        /// <summary>激活：视觉置顶 + 焦点切换，两者原子完成。</summary>
        public void BringToFront() => UIMgr.Ins.Windows.Activate(ID);

        /// <summary>降到本 Tier 开头。</summary>
        public void SendToBack() => UIMgr.Ins.Windows.SendToBack(ID);

        /// <summary>运行时调整层叠级别（AlwaysOnTop 开关等）。</summary>
        public void SetTier(UIWindowTier tier)
        {
            if (WindowTier == tier)
                return;

            UIMgr.Ins.Windows.SetTier(ID, tier);
            NotifyLayoutChanged();
            TierChanged?.Invoke(tier);
        }

        /// <summary>是否处于置顶（AlwaysOnTop）状态。</summary>
        public bool IsTopmost => WindowTier == UIWindowTier.AlwaysOnTop;

        /// <summary>设置/取消置顶。只改层内排序，不影响最大化/最小化。</summary>
        public void SetTopmost(bool value) =>
            SetTier(value ? UIWindowTier.AlwaysOnTop : UIWindowTier.Normal);

        /// <summary>切换置顶（置顶按钮用）。</summary>
        public void ToggleTopmost() => SetTopmost(!IsTopmost);

        /// <summary>层叠级别变化（置顶按钮更新图标等）。</summary>
        public event Action<UIWindowTier> TierChanged;
    }
}
