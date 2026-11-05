using UnityEngine;

namespace Ux
{
    /// <summary>
    /// 窗口拖拽算法。纯逻辑类：**不继承 UIObject，也不进节点树**。
    ///
    /// 由持有者负责生命周期：
    /// - <see cref="UIWindowFrame"/>（框架预制路径，热区来自框架节点）
    /// - <see cref="UIWindow"/> 的退化路径（没有框架预制时）
    ///
    /// 关键点：
    /// - 全程世界坐标移动，不依赖 Body 的 pivot/锚点；
    /// - 屏幕点用 RectTransformUtility 换算，正确处理 CanvasScaler 缩放；
    /// - 拖拽过程零 GC（复用 Vector3[4]）。
    /// </summary>
    public sealed class UIWindowDrag
    {
        /// <summary>允许拖出父容器的可见尺寸（UI 单位），保证窗口还能被拖回来。</summary>
        public float MinVisible = 96f;

        /// <summary>是否在松手时贴父容器边缘。</summary>
        public bool SnapEnabled;

        /// <summary>贴边阈值（UI 单位）。</summary>
        public float SnapThreshold = 16f;

        /// <summary>是否可拖动。只影响拖动本身，不影响点击置顶（见 <see cref="ActivateOnPointerDown"/>）。</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>按下即激活（置顶 + 焦点）。与 <see cref="Enabled"/> 独立：锁住拖动时点击仍要置顶。</summary>
        public bool ActivateOnPointerDown { get; set; } = true;

        /// <summary>
        /// 拖拽/吸附的范围节点；null 时用父容器（Window 层，全屏）。
        /// 指定后窗口被限制在该节点的矩形内（世界角点会换算到父容器局部系）。
        /// </summary>
        public RectTransform DragArea { get; set; }

        public UIWindow Window { get; }
        public GameObject Handle { get; }

        private readonly RectTransform _body;
        private readonly RectTransform _parent;
        private readonly Vector3[] _corners = new Vector3[4];
        private readonly Vector3[] _areaCorners = new Vector3[4];

        private bool _dragging;
        private Vector2 _pressLocal;
        private Vector3 _startWorldPos;
        private bool _dragAreaChecked;
        private bool _dragAreaValid = true;

        /// <param name="window">所属窗口</param>
        /// <param name="handle">拖拽热区；传 null 时优先取标题栏槽位，没有则整窗</param>
        public UIWindowDrag(UIWindow window, GameObject handle = null)
        {
            Window = window;
            Handle = handle != null ? handle : ResolveDefaultHandle(window);

            if (window == null)
            {
                Log.Error("窗口拖拽创建失败：所属窗口为空。");
                return;
            }

            _body = window.BodyRect;
            if (_body == null)
            {
                Log.Error("窗口拖拽无法工作：窗口没有 Body。Window[{0}]", window.GetType().Name);
                return;
            }

            _parent = window.LayoutSpace;
            if (_parent == null)
            {
                Log.Error("窗口拖拽无法工作：Body 的父节点不是 RectTransform。Window[{0}]", window.GetType().Name);
                return;
            }

            if (Handle == null)
            {
                Log.Error("窗口拖拽无法工作：找不到拖拽热区。Window[{0}]", window.GetType().Name);
                return;
            }

            Rebind();

            // 拖拽热区不要点击缩放：按下缩到 0.92 会让拖动跟着抖。
            UIButtonScale.Configure(Handle, false, 1f, 0f);
        }

        public bool IsDragging => _dragging;

        /// <summary>绑回事件。窗口每次上屏都会先清空 UIEvent 订阅，所以必须重绑。</summary>
        public void Rebind()
        {
            if (Handle == null || Window == null)
                return;

            Window.RegisterPointerDown(Handle, OnPointerDown);
            Window.RegisterDrag(Handle, OnDragMove, OnDragBegin, OnDragEnd);
        }

        /// <summary>停掉进行中的拖拽状态（窗口下屏/销毁时调用）。</summary>
        public void Reset()
        {
            _dragging = false;
        }

        public void Dispose()
        {
            _dragging = false;
        }

        private static GameObject ResolveDefaultHandle(UIWindow window)
        {
            if (window == null)
                return null;

            return window.BodyRect != null ? window.BodyRect.gameObject : null;
        }

        private void OnPointerDown(UIEventContext ctx)
        {
            if (!ActivateOnPointerDown)
                return;

            // 单击（不拖动）也要置顶，所以激活放在按下，而不是拖拽开始。
            Window.BringToFront();
        }

        private void OnDragBegin(UIEventContext ctx)
        {
            if (!Enabled || _body == null || _parent == null)
                return;

            var data = ctx.PointerEventData;
            if (data == null)
                return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _parent, data.position, data.pressEventCamera, out _pressLocal))
                return;

            _startWorldPos = _body.position;
            _dragging = true;
        }

        private void OnDragMove(UIEventContext ctx)
        {
            if (!Enabled || !_dragging || _body == null || _parent == null)
                return;

            var data = ctx.PointerEventData;
            if (data == null)
                return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _parent, data.position, data.pressEventCamera, out var local))
                return;

            var delta = local - _pressLocal;
            _body.position = _startWorldPos + _parent.TransformVector(new Vector3(delta.x, delta.y, 0f));
            ClampToParent();
        }

        private void OnDragEnd(UIEventContext ctx)
        {
            if (!_dragging)
                return;

            _dragging = false;
            ClampToParent();
            ApplySnap();
            Window.NotifyLayoutChanged();
        }

        /// <summary>
        /// 把窗口约束在父容器内：允许出界，但必须保留 MinVisible 的可抓区域。
        /// 用世界角点换算，因此不依赖 Body 的 pivot 与锚点。
        /// </summary>
        private void ClampToParent()
        {
            if (_body == null || _parent == null)
                return;

            _body.GetWorldCorners(_corners); // 0=左下 1=左上 2=右上 3=右下
            var areaRect = GetClampRect();

            var bl = _parent.InverseTransformPoint(_corners[0]);
            var tr = _parent.InverseTransformPoint(_corners[2]);

            var bodyW = tr.x - bl.x;
            var bodyH = tr.y - bl.y;

            // 窗口比限制区小：完全限制在区内（相当于不能出界）。
            // 窗口比限制区大：放不下，允许出界，但保留 MinVisible 的可抓区域。
            var visibleX = bodyW <= areaRect.width ? bodyW : Mathf.Min(bodyW, MinVisible);
            var visibleY = bodyH <= areaRect.height ? bodyH : Mathf.Min(bodyH, MinVisible);

            var dx = 0f;
            var dy = 0f;

            // 向左/向右拖出：至少保留 visible 在范围内。
            if (tr.x < areaRect.xMin + visibleX)
                dx = areaRect.xMin + visibleX - tr.x;
            else if (bl.x > areaRect.xMax - visibleX)
                dx = areaRect.xMax - visibleX - bl.x;

            if (tr.y < areaRect.yMin + visibleY)
                dy = areaRect.yMin + visibleY - tr.y;
            else if (bl.y > areaRect.yMax - visibleY)
                dy = areaRect.yMax - visibleY - bl.y;

            if (dx == 0f && dy == 0f)
                return;

            _body.position += _parent.TransformVector(new Vector3(dx, dy, 0f));
        }

        /// <summary>贴边：把与父容器四边的距离小于阈值的边吸齐。</summary>
        private void ApplySnap()
        {
            if (!SnapEnabled || _body == null || _parent == null)
                return;

            _body.GetWorldCorners(_corners);
            var parentRect = GetClampRect();

            var bl = _parent.InverseTransformPoint(_corners[0]);
            var tr = _parent.InverseTransformPoint(_corners[2]);

            var dx = 0f;
            var dy = 0f;

            if (Mathf.Abs(bl.x - parentRect.xMin) <= SnapThreshold)
                dx = parentRect.xMin - bl.x;
            else if (Mathf.Abs(tr.x - parentRect.xMax) <= SnapThreshold)
                dx = parentRect.xMax - tr.x;

            if (Mathf.Abs(bl.y - parentRect.yMin) <= SnapThreshold)
                dy = parentRect.yMin - bl.y;
            else if (Mathf.Abs(tr.y - parentRect.yMax) <= SnapThreshold)
                dy = parentRect.yMax - tr.y;

            if (dx == 0f && dy == 0f)
                return;

            _body.position += _parent.TransformVector(new Vector3(dx, dy, 0f));
        }

        /// <summary>
        /// 取限制矩形（父容器局部坐标）：优先 DragArea，否则用父容器本身的 rect。
        /// 用世界角点换算，因此 DragArea 可以位于任意层级/旋转/缩放下。
        /// </summary>
        private Rect GetClampRect()
        {
            if (DragArea == null)
                return _parent.rect;

            if (!_dragAreaChecked)
            {
                _dragAreaChecked = true;

                // 限制区如果放在被拖动 Body 的子树内，会跟着窗口一起移动，限制永远不生效。
                if (_body != null && (DragArea == _body || DragArea.IsChildOf(_body)))
                {
                    _dragAreaValid = false;
                    Log.Error("窗口拖动范围无效：DragArea({0}) 在 Body({1}) 子树内，会随窗口移动。" +
                              "请把限制区放到窗口外部（如 UIRoot 下的安全区节点）。Window[{2}]",
                        DragArea.name, _body.name, Window != null ? Window.GetType().Name : "null");
                }
            }

            if (!_dragAreaValid)
                return _parent.rect;

            DragArea.GetWorldCorners(_areaCorners);
            var min = _parent.InverseTransformPoint(_areaCorners[0]);
            var max = _parent.InverseTransformPoint(_areaCorners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
    }
}
