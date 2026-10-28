using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    public readonly struct UGUIVirtualStickyHeaderToken
    {
        internal readonly UGUIVirtualStickyHeader Owner;
        internal readonly ulong Version;
        public bool IsCurrent => Owner != null && Owner.IsTokenCurrent(Version);
        public GameObject GameObject => IsCurrent ? Owner.CurrentObject : null;
        internal UGUIVirtualStickyHeaderToken(UGUIVirtualStickyHeader owner, ulong version) { Owner = owner; Version = version; }
    }

    /// <summary>纯 C# 吸顶视图控制器。使用独立副本，不移动 Content 中的真实 Header，不参与普通 Item 索引。</summary>
    public sealed class UGUIVirtualStickyHeader : IDisposable
    {
        private readonly UGUIVirtualList _list;
        private readonly UGUIVirtualGroupAdapter _groups;
        private readonly IUGUIVirtualListItemFactory _factory;
        private readonly Dictionary<int, Stack<IUGUIVirtualListItem>> _pools = new();
        private RectTransform _layer;
        private IUGUIVirtualListItem _view;
        private int _viewType;
        private UGUIVirtualGroupItem _header;
        private UGUIVirtualGroupItem _stickyData;
        private ulong _headerRevision;
        private ulong _version;
        private float _length;
        private Vector2 _layoutSize;
        private bool _visible;
        private bool _updating;
        private bool _disposePending;
        private bool _disposed;
        private bool _forceBind;
        private bool _needsMeasure;
        private bool _faulted;

        public UGUIVirtualStickyHeader(UGUIVirtualList list, UGUIVirtualGroupAdapter groups, IUGUIVirtualListItemFactory factory)
        {
            _list = list ?? throw new ArgumentNullException(nameof(list));
            _groups = groups ?? throw new ArgumentNullException(nameof(groups));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }
        public bool IsVisible => !_disposed && !_disposePending && _visible && Alive(_view);
        public bool IsDisposalComplete { get; private set; }
        public string CurrentGroupId => IsVisible ? _header.GroupId : null;
        public GameObject CurrentObject => IsVisible ? _view.GameObject : null;
        public float PushOffset { get; private set; }
        public float HeaderLength => _length;
        public Exception LastError { get; private set; }
        public bool TryGetToken(out UGUIVirtualStickyHeaderToken token)
        {
            token = default;
            if (!IsTokenCurrent(_version)) return false;
            token = new UGUIVirtualStickyHeaderToken(this, _version);
            return true;
        }
        internal bool IsTokenCurrent(ulong version)
        {
            return !_faulted && IsVisible && version == _version && _groups.OwnsCurrentData &&
                _groups.TryGetHeader(_header.GroupId, out var header) && ReferenceEquals(header, _header) &&
                header.Revision == _headerRevision;
        }
        public bool NotifySizeChanged(UGUIVirtualStickyHeaderToken token)
        {
            if (!ReferenceEquals(token.Owner, this) || !token.IsCurrent || !_list.SupportsDynamicItemSize) return false;
            _needsMeasure = true;
            Tick();
            return true;
        }
        public void Refresh()
        {
            if (_disposed || _disposePending) return;
            _faulted = false;
            LastError = null;
            _forceBind = true;
            Tick();
        }

        public void Tick()
        {
            if (_disposed || _disposePending || _updating || _faulted) return;
            if (_list.IsDisposedOrDisposing) { Dispose(); return; }
            if (!_list.CanMutateData) return;
            _updating = true;
            bool entered = false;
            try
            {
                _list.BeginExternalRender();
                entered = true;
                if (!_groups.OwnsCurrentData || _list.ViewportSize.x <= 0 || _list.ViewportSize.y <= 0)
                { ReleaseView(); return; }
                var selected = _groups.GetStickyHeader(_list.ScrollPosition);
                if (!selected.IsVisible) { ReleaseView(); return; }
                var layout = _list.GetItemLayout(selected.HeaderIndex);
                bool bind = !Alive(_view) || _forceBind || !ReferenceEquals(_header, selected.Header) ||
                    _headerRevision != selected.Header.Revision;
                if (bind)
                {
                    var data = selected.Header.StickyCopy();
                    int type = _factory.GetItemType(selected.HeaderIndex, data);
                    if (_view != null && type != _viewType) ReleaseView();
                    EnsureLayer();
                    if (!Alive(_view)) _view = GetView(type);
                    _viewType = type;
                    _header = selected.Header;
                    _headerRevision = _header.Revision;
                    _stickyData = data;
                    _version++;
                    _visible = true;
                    _layoutSize = layout.Size;
                    PrepareGeometry(layout.Size, true);
                    _view.SetClickHandler(null); // 吸顶副本仅显示；输入穿透到 ScrollRect / 真实行。
                    _view.GameObject.SetActive(true);
                    _view.Bind(selected.HeaderIndex, data);
                    _needsMeasure = _list.SupportsDynamicItemSize;
                    _forceBind = false;
                }
                if (_disposePending || _list.IsDisposedOrDisposing) return;
                if (_layoutSize != layout.Size)
                {
                    _layoutSize = layout.Size;
                    PrepareGeometry(layout.Size, _needsMeasure);
                }
                if (_needsMeasure)
                {
                    _needsMeasure = false;
                    var size = _view.Measure();
                    if (_disposePending || _list.IsDisposedOrDisposing) return;
                    float main = _list.IsVertical ? size.y : size.x;
                    if (main <= 0f || float.IsNaN(main) || float.IsInfinity(main))
                        throw new InvalidOperationException("吸顶 Header 测量结果必须为有限正数。");
                    _length = main;
                }
                else if (!_list.SupportsDynamicItemSize)
                    _length = _list.IsVertical ? layout.Size.y : layout.Size.x;
                if (_length <= 0f) _length = _list.IsVertical ? layout.Size.y : layout.Size.x;
                PushOffset = 0f;
                if (selected.NextHeaderIndex >= 0)
                {
                    var next = _list.GetItemLayout(selected.NextHeaderIndex).Position;
                    float start = _list.IsVertical ? -next.y : next.x;
                    PushOffset = UGUIVirtualGroupAdapter.CalculatePushOffset(start - _list.ScrollPosition, _length);
                }
                var rect = _view.RectTransform;
                var sizeNow = _list.IsVertical ? new Vector2(layout.Size.x, _length) : new Vector2(_length, layout.Size.y);
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, sizeNow.x);
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, sizeNow.y);
                var position = _list.IsVertical ? new Vector2(0, -PushOffset) : new Vector2(PushOffset, 0);
                if (rect.anchoredPosition != position) rect.anchoredPosition = position;
                if (_layer != null) _layer.SetAsLastSibling();
            }
            catch (Exception ex)
            {
                LastError = ex;
                _faulted = true;
                _version++;
                try { ReleaseView(); } catch { /* 首个渲染错误保留在 LastError。 */ }
                throw;
            }
            finally
            {
                try { if (entered) _list.EndExternalRender(); }
                finally
                {
                    _updating = false;
                    if (_disposePending) Dispose();
                }
            }
        }

        private void EnsureLayer()
        {
            if (_layer != null) return;
            var go = new GameObject("[VirtualList Sticky Header]", typeof(RectTransform), typeof(CanvasGroup));
            _layer = (RectTransform)go.transform;
            _layer.SetParent(_list.ViewportTransform, false);
            _layer.anchorMin = _layer.anchorMax = _layer.pivot = new Vector2(0, 1);
            _layer.anchoredPosition = Vector2.zero;
            _layer.sizeDelta = Vector2.zero;
            var group = go.GetComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;
        }
        private void PrepareGeometry(Vector2 size, bool pendingMeasure)
        {
            var rect = _view.RectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = Vector2.zero;
            if (!_list.SupportsDynamicItemSize || !_list.IsVertical || !pendingMeasure)
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
            if (!_list.SupportsDynamicItemSize || _list.IsVertical || !pendingMeasure)
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
        }
        private IUGUIVirtualListItem GetView(int type)
        {
            if (!_pools.TryGetValue(type, out var pool)) { pool = new Stack<IUGUIVirtualListItem>(); _pools.Add(type, pool); }
            while (pool.Count > 0)
            {
                var view = pool.Pop();
                if (Alive(view)) { view.GameObject.transform.SetParent(_layer, false); return view; }
            }
            return _factory.Create(type, _layer);
        }
        private void ReleaseView()
        {
            var view = _view;
            int type = _viewType;
            _view = null;
            _visible = false;
            _header = null;
            _stickyData = null;
            _length = 0;
            _version++;
            PushOffset = 0;
            if (!Alive(view)) return;
            var root = view.GameObject;
            try { view.SetClickHandler(null); view.OnRecycle(); }
            finally
            {
                if (root != null)
                {
                    root.SetActive(false);
                    if (!_pools.TryGetValue(type, out var pool)) { pool = new Stack<IUGUIVirtualListItem>(); _pools.Add(type, pool); }
                    pool.Push(view);
                }
            }
        }
        private static bool Alive(IUGUIVirtualListItem view) => view != null &&
            (!(view is UnityEngine.Object native) || native != null) && view.GameObject != null;
        public void Dispose()
        {
            if (_disposed) return;
            _disposePending = true;
            if (_updating) return;
            _disposed = true;
            Exception failure = null;
            try { ReleaseView(); } catch (Exception ex) { failure = ex; }
            foreach (var pool in _pools.Values)
            {
                while (pool.Count > 0)
                {
                    var item = pool.Pop();
                    try { _factory.Destroy(item); } catch (Exception ex) { failure ??= ex; }
                }
            }
            _pools.Clear();
            if (_layer != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(_layer.gameObject);
                else UnityEngine.Object.DestroyImmediate(_layer.gameObject);
                _layer = null;
            }
            IsDisposalComplete = true;
            if (failure != null) throw new InvalidOperationException("吸顶视图清理回调失败，其余对象已继续清理。", failure);
        }
    }
}
