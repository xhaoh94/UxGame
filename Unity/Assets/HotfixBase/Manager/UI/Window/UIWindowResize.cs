using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ux
{
    /// <summary>
    /// 窗口缩放算法。纯逻辑类：不继承 UIObject、不进节点树，由框架或窗口持有。
    ///
    /// 8 向把手两种来源：
    /// 1. 传入 handleRoot（框架预制里的把手父节点）：按固定顺序取 0..7 号子节点；
    /// 2. 不传：在 Body 下运行时生成 8 个透明 Image 把手（不依赖美术出图）。
    ///
    /// 尺寸计算全程在父容器的局部坐标里完成，再用 pivot 反推世界位置，
    /// 因此不依赖 Body 的 pivot；但要求 Body 的锚点不拉伸（anchorMin == anchorMax）。
    /// </summary>
    public sealed class UIWindowResize
    {
        [Flags]
        private enum Edge
        {
            None = 0,
            Left = 1 << 0,
            Right = 1 << 1,
            Bottom = 1 << 2,
            Top = 1 << 3,
        }

        /// <summary>把手子节点的固定顺序。框架预制若自带把手，必须按这个顺序摆放。</summary>
        private static readonly Edge[] EdgeOrder =
        {
            Edge.Left, Edge.Right, Edge.Top, Edge.Bottom,
            Edge.Left | Edge.Top, Edge.Right | Edge.Top,
            Edge.Left | Edge.Bottom, Edge.Right | Edge.Bottom,
        };

        /// <summary>最小尺寸（UI 单位）。</summary>
        public Vector2 MinSize = new Vector2(320f, 240f);

        /// <summary>把手厚度（UI 单位）。</summary>
        public float HandleThickness = 8f;

        public bool Enabled { get; set; } = true;

        public UIWindow Window { get; }
        public GameObject HandleRoot { get; }

        private readonly RectTransform _body;
        private readonly RectTransform _parent;
        private readonly Vector3[] _corners = new Vector3[4];
        private readonly Vector3[] _areaCorners = new Vector3[4];
        private readonly List<GameObject> _runtimeHandles = new List<GameObject>(8);

        private bool _resizing;
        private Edge _edge;
        private Vector2 _pressLocal;
        private Rect _startRect;

        public UIWindowResize(UIWindow window, GameObject handleRoot = null)
        {
            Window = window;
            HandleRoot = handleRoot;

            if (window == null)
            {
                Log.Error("窗口缩放创建失败：所属窗口为空。");
                return;
            }

            _body = window.BodyRect;
            if (_body == null)
            {
                Log.Error("窗口缩放无法工作：窗口没有 Body。Window[{0}]", window.GetType().Name);
                return;
            }

            _parent = window.LayoutSpace;
            if (_parent == null)
            {
                Log.Error("窗口缩放无法工作：Body 的父节点不是 RectTransform。Window[{0}]", window.GetType().Name);
                return;
            }

            if (_body.anchorMin != _body.anchorMax)
            {
                Log.Warning("窗口 Body 使用了拉伸锚点，scaleDelta 不等于尺寸，缩放结果会偏。Window[{0}]，" +
                            "请把 panelBody 的 anchorMin/anchorMax 设为同一个点（约定顶左）。", window.GetType().Name);
            }

            CreateHandles();
            Rebind();
        }

        public bool IsResizing => _resizing;

        /// <summary>绑回事件。窗口每次上屏都会先清空 UIEvent 订阅。</summary>
        public void Rebind()
        {
            if (Window == null || _body == null)
                return;

            if (HandleRoot != null && HandleRoot.transform.childCount >= EdgeOrder.Length)
            {
                for (int i = 0; i < EdgeOrder.Length; i++)
                    BindHandle(HandleRoot.transform.GetChild(i).gameObject, EdgeOrder[i]);
                return;
            }

            for (int i = 0; i < _runtimeHandles.Count; i++)
                BindHandle(_runtimeHandles[i], EdgeOrder[i]);
        }

        public void Reset()
        {
            _resizing = false;
        }

        public void Dispose()
        {
            _resizing = false;

            for (int i = 0; i < _runtimeHandles.Count; i++)
            {
                if (_runtimeHandles[i] != null)
                    UnityEngine.Object.Destroy(_runtimeHandles[i]);
            }
            _runtimeHandles.Clear();
        }

        // —— 把手 ——

        private void CreateHandles()
        {
            // 框架预制自带把手：不生成，由 Rebind 绑定。
            if (HandleRoot != null && HandleRoot.transform.childCount >= EdgeOrder.Length)
                return;

            var thickness = Mathf.Max(2f, HandleThickness);
            var cornerSize = thickness * 2f;

            for (int i = 0; i < EdgeOrder.Length; i++)
            {
                var edge = EdgeOrder[i];
                var go = new GameObject("Resize_" + edge, typeof(RectTransform), typeof(Image));
                var rect = (RectTransform)go.transform;
                rect.SetParent(_body, false);
                rect.SetAsLastSibling();

                var image = go.GetComponent<Image>();
                image.color = new Color(0f, 0f, 0f, 0f);
                image.raycastTarget = true;

                LayoutHandle(rect, edge, thickness, cornerSize);
                UIButtonScale.Configure(go, false, 1f, 0f);

                _runtimeHandles.Add(go);
            }
        }

        private static void LayoutHandle(RectTransform rect, Edge edge, float thickness, float cornerSize)
        {
            var left = (edge & Edge.Left) != 0;
            var right = (edge & Edge.Right) != 0;
            var bottom = (edge & Edge.Bottom) != 0;
            var top = (edge & Edge.Top) != 0;

            rect.pivot = new Vector2(0.5f, 0.5f);

            // 角：锚在对应顶点
            if ((left || right) && (top || bottom))
            {
                var ax = left ? 0f : 1f;
                var ay = bottom ? 0f : 1f;
                rect.anchorMin = new Vector2(ax, ay);
                rect.anchorMax = new Vector2(ax, ay);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(cornerSize, cornerSize);
                return;
            }

            // 左右边：垂直拉伸
            if (left || right)
            {
                var ax = left ? 0f : 1f;
                rect.anchorMin = new Vector2(ax, 0f);
                rect.anchorMax = new Vector2(ax, 1f);
                rect.offsetMin = new Vector2(-thickness * 0.5f, cornerSize * 0.5f);
                rect.offsetMax = new Vector2(thickness * 0.5f, -cornerSize * 0.5f);
                return;
            }

            // 上下边：水平拉伸
            var ayEdge = bottom ? 0f : 1f;
            rect.anchorMin = new Vector2(0f, ayEdge);
            rect.anchorMax = new Vector2(1f, ayEdge);
            rect.offsetMin = new Vector2(cornerSize * 0.5f, -thickness * 0.5f);
            rect.offsetMax = new Vector2(-cornerSize * 0.5f, thickness * 0.5f);
        }

        private void BindHandle(GameObject handle, Edge edge)
        {
            if (handle == null)
                return;

            Window.RegisterDrag(handle,
                ctx => OnResizeMove(ctx, edge),
                ctx => OnResizeBegin(ctx, edge),
                OnResizeEnd);
        }

        // —— 缩放手势 ——

        private void OnResizeBegin(UIEventContext ctx, Edge edge)
        {
            if (!Enabled || _body == null || _parent == null)
                return;

            if (!TryGetParentLocal(ctx.PointerEventData, out _pressLocal))
                return;

            Window.BringToFront();

            _startRect = GetBodyRect();
            _edge = edge;
            _resizing = true;
        }

        private void OnResizeMove(UIEventContext ctx, Edge edge)
        {
            if (!Enabled || !_resizing || _body == null || _parent == null)
                return;

            if (!TryGetParentLocal(ctx.PointerEventData, out var local))
                return;

            var delta = local - _pressLocal;
            var rect = _startRect;

            if ((_edge & Edge.Left) != 0) rect.xMin = _startRect.xMin + delta.x;
            if ((_edge & Edge.Right) != 0) rect.xMax = _startRect.xMax + delta.x;
            if ((_edge & Edge.Bottom) != 0) rect.yMin = _startRect.yMin + delta.y;
            if ((_edge & Edge.Top) != 0) rect.yMax = _startRect.yMax + delta.y;

            ApplyMinSize(ref rect);
            ClampToArea(ref rect);
            ApplyRect(rect);
        }

        private void OnResizeEnd(UIEventContext ctx)
        {
            if (!_resizing)
                return;

            _resizing = false;
            Window.NotifyLayoutChanged();
        }

        /// <summary>
        /// 把被拖拽的那条边约束在限制区内（与拖拽用同一个 DragArea）。
        /// 与最小尺寸冲突时，最小尺寸优先（宁可超出区，也不能把窗口压没）。
        /// </summary>
        private void ClampToArea(ref Rect rect)
        {
            var area = GetAreaRect();
            if (area.width <= 0f || area.height <= 0f)
                return;

            if ((_edge & Edge.Left) != 0)
                rect.xMin = Mathf.Max(rect.xMin, Mathf.Min(area.xMin, rect.xMax - MinSize.x));
            if ((_edge & Edge.Right) != 0)
                rect.xMax = Mathf.Min(rect.xMax, Mathf.Max(area.xMax, rect.xMin + MinSize.x));
            if ((_edge & Edge.Bottom) != 0)
                rect.yMin = Mathf.Max(rect.yMin, Mathf.Min(area.yMin, rect.yMax - MinSize.y));
            if ((_edge & Edge.Top) != 0)
                rect.yMax = Mathf.Min(rect.yMax, Mathf.Max(area.yMax, rect.yMin + MinSize.y));
        }

        private Rect GetAreaRect()
        {
            var area = Window != null ? Window.WindowDragArea : null;
            if (area == null || area == _body || area.IsChildOf(_body))
                return _parent.rect;

            area.GetWorldCorners(_areaCorners);
            var min = _parent.InverseTransformPoint(_areaCorners[0]);
            var max = _parent.InverseTransformPoint(_areaCorners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private void ApplyMinSize(ref Rect rect)
        {
            if (rect.width < MinSize.x)
            {
                if ((_edge & Edge.Left) != 0)
                    rect.xMin = rect.xMax - MinSize.x;
                else
                    rect.xMax = rect.xMin + MinSize.x;
            }

            if (rect.height < MinSize.y)
            {
                if ((_edge & Edge.Bottom) != 0)
                    rect.yMin = rect.yMax - MinSize.y;
                else
                    rect.yMax = rect.yMin + MinSize.y;
            }
        }

        /// <summary>取 Body 在父容器局部系里的矩形。</summary>
        private Rect GetBodyRect()
        {
            _body.GetWorldCorners(_corners); // 0=左下 2=右上
            var bl = _parent.InverseTransformPoint(_corners[0]);
            var tr = _parent.InverseTransformPoint(_corners[2]);
            return Rect.MinMaxRect(bl.x, bl.y, tr.x, tr.y);
        }

        /// <summary>把目标矩形写回 Body：按 pivot 反推世界位置，尺寸用 sizeDelta。</summary>
        private void ApplyRect(Rect rect)
        {
            var size = rect.size;
            var pivot = _body.pivot;
            var pivotLocal = new Vector2(
                rect.xMin + size.x * pivot.x,
                rect.yMin + size.y * pivot.y);

            _body.position = _parent.TransformPoint(pivotLocal);
            _body.sizeDelta = size;
        }

        private bool TryGetParentLocal(PointerEventData data, out Vector2 local)
        {
            local = default;
            return data != null && _parent != null &&
                   RectTransformUtility.ScreenPointToLocalPointInRectangle(
                       _parent, data.position, data.pressEventCamera, out local);
        }
    }
}
