using TMPro;
using UnityEngine;

namespace Ux
{
    /// <summary>
    /// 窗口框架：封装「窗口骨架」预制（标题栏、按钮、边框）。
    /// 与 <see cref="UITabFrame"/> 同构——由代码生成器按命名规则产出，
    /// 并通过 <see cref="UIObject.AddComponent"/> 进入窗口的节点树。
    ///
    /// 它只负责「节点在哪、装什么交互」；拖拽/缩放的算法在
    /// <see cref="UIWindowDrag"/> 等纯逻辑类里，按 <see cref="UIWindow.WindowCapabilities"/> 装配。
    /// </summary>
    public abstract class UIWindowFrame : UIObject
    {
        /// <summary>标题栏节点（拖拽热区）。由生成器按 FrameTitleBar 槽位生成 override。</summary>
        protected virtual RectTransform __frameTitleBar { get; } = null;

        /// <summary>标题文本。</summary>
        protected virtual TMP_Text __frameTitle { get; } = null;

        /// <summary>最小化 / 最大化 / 关闭按钮。</summary>
        protected virtual GameObject __frameBtnMin { get; } = null;
        protected virtual GameObject __frameBtnMax { get; } = null;
        protected virtual GameObject __frameBtnClose { get; } = null;

        /// <summary>置顶按钮。约定子节点 on/off 两个图标，切换时由框架 SetActive。</summary>
        protected virtual GameObject __frameBtnPin { get; } = null;

        /// <summary>所属窗口。</summary>
        public UIWindow Window { get; private set; }

        private UIWindowDrag _drag;
        private UIWindowResize _resize;

        public UIWindowFrame(GameObject container, UIObject parent)
        {
            Init(container, parent);          // 子类的 CreateChildren 在这里绑好 __frameTitleBar

            Window = parent as UIWindow;
            parent?.AddComponent(this);       // 合法的节点封装：进 Components 节点树

            if (Window == null)
            {
                Log.Error("窗口框架创建失败：父级不是 UIWindow。Frame[{0}]", GetType().Name);
                return;
            }

            var caps = Window.WindowCapabilities;

            // 无窗口模式：完全当作普通页面，框架即使被误放也不响应任何交互。
            if (caps == UIWindowCapability.None)
            {
                Log.Warning("窗口声明了 None 能力，UIWindowFrame 不会绑定交互。Frame[{0}] Window[{1}]",
                    GetType().Name, Window.GetType().Name);
                return;
            }

            if (caps.HasFlag(UIWindowCapability.Drag))
            {
                var handle = __frameTitleBar != null ? __frameTitleBar.gameObject : GameObject;
                _drag = new UIWindowDrag(Window, handle)
                {
                    SnapEnabled = caps.HasFlag(UIWindowCapability.Snap),
                    DragArea = Window.WindowDragArea,
                };
            }

            if (caps.HasFlag(UIWindowCapability.Resize))
                _resize = new UIWindowResize(Window);

            Window.TierChanged += OnTierChanged;
            BindFrameEvents();
        }

        /// <summary>当前拖拽算法实例（调试/测试用，可能为 null）。</summary>
        public UIWindowDrag Drag => _drag;

        /// <summary>当前缩放算法实例（调试/测试用，可能为 null）。</summary>
        public UIWindowResize Resize => _resize;

        protected override void OnShow()
        {
            base.OnShow();
            _drag?.Rebind();
            _resize?.Rebind();
            BindFrameEvents();   // 上屏时 UIEvent 订阅已被清空，按钮要重绑
        }

        protected override void OnHide()
        {
            base.OnHide();
            _drag?.Reset();
            _resize?.Reset();
        }

        protected override void OnDispose()
        {
            if (Window != null)
                Window.TierChanged -= OnTierChanged;

            _drag?.Dispose();
            _drag = null;
            _resize?.Dispose();
            _resize = null;
            base.OnDispose();
        }

        private void BindFrameEvents()
        {
            if (Window == null)
                return;

            var caps = Window.WindowCapabilities;

            // 无窗口模式：框架不响应任何交互（包括关闭）。
            if (caps == UIWindowCapability.None)
                return;

            if (__frameBtnClose != null)
                Window.RegisterClick(__frameBtnClose, OnCloseClick);

            if (__frameBtnPin != null)
            {
                Window.RegisterClick(__frameBtnPin, OnPinClick);
                UpdatePinVisual(Window.WindowTier);
            }

            if (__frameBtnMin != null)
            {
                if (caps.HasFlag(UIWindowCapability.Minimize))
                    Window.RegisterClick(__frameBtnMin, OnMinimizeClick);
                else
                    __frameBtnMin.SetActive(false);   // 能力未开：隐藏，避免“能点但没反应”
            }

            if (__frameBtnMax != null)
            {
                if (caps.HasFlag(UIWindowCapability.Maximize))
                    Window.RegisterClick(__frameBtnMax, OnMaximizeClick);
                else
                    __frameBtnMax.SetActive(false);
            }

            if (__frameTitleBar != null && caps.HasFlag(UIWindowCapability.Maximize))
                Window.RegisterMultipleClick(__frameTitleBar.gameObject, OnTitleDoubleClick);
        }

        private void OnCloseClick()
        {
            Window.Hide();
        }

        private void OnPinClick()
        {
            Window.ToggleTopmost();
        }

        private void OnTierChanged(UIWindowTier tier)
        {
            UpdatePinVisual(tier);
        }

        /// <summary>约定：置顶按钮下放 on / off 两个子节点，按状态切换显示。</summary>
        private void UpdatePinVisual(UIWindowTier tier)
        {
            if (__frameBtnPin == null)
                return;

            var on = __frameBtnPin.transform.Find("on");
            var off = __frameBtnPin.transform.Find("off");

            if (on != null)
                on.gameObject.SetActive(tier == UIWindowTier.AlwaysOnTop);

            if (off != null)
                off.gameObject.SetActive(tier != UIWindowTier.AlwaysOnTop);
        }

        private void OnMinimizeClick()
        {
            Window.Minimize();
        }

        private void OnMaximizeClick()
        {
            ToggleMaximize();
        }

        private void OnTitleDoubleClick(UIEventContext ctx)
        {
            ToggleMaximize();
        }

        private void ToggleMaximize()
        {
            if (Window.LayoutState == WindowState.Maximized)
                Window.Restore();
            else
                Window.Maximize();
        }
    }
}
