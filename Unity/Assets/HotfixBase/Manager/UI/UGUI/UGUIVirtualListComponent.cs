using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ux
{
    public enum UGUIVirtualListLayoutMode
    {
        FixedLinear = 0,
        VariableLinear = 1,
        // 只保留旧序列化值和代码兼容；组件会迁移为 FixedLinear + 多列开关。
        FixedGrid = 2
    }

    public enum UGUIVirtualListItemSizeMode
    {
        Manual = 0,
        FromTemplate = 1
    }

    /// <summary>
    /// 可选的 Inspector 适配层。虚拟化逻辑仍由纯 C# UGUIVirtualList 执行。
    /// 模板既可以是 Project Prefab，也可以是场景中的 RectTransform 对象。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Ux/UI/UGUI Virtual List")]
    public sealed partial class UGUIVirtualListComponent : MonoBehaviour
    {
        [Serializable]
        public sealed class ItemTemplate
        {
            [Tooltip("模板类型 ID，由 ItemTypeProvider 返回；不同模板不能重复。")]
            public int TypeId;

            [Tooltip("可拖入 Hierarchy 场景对象或 Project Prefab，根节点必须是 RectTransform。")]
            public GameObject Template;
        }

        [Header("滚动组件")]
        [SerializeField] private ScrollRect _scrollRect;

        [Header("布局（运行前配置）")]
        [SerializeField] private UGUIVirtualListLayoutMode _layoutMode;
        [SerializeField] private UGUIVirtualListAxis _axis;
        [Tooltip("Manual 手动指定；FromTemplate 在初始化时读取默认模板根节点的 RectTransform 尺寸。")]
        [SerializeField] private UGUIVirtualListItemSizeMode _itemSizeMode;
        [Tooltip("固定布局为精确宽高；可变布局中主轴为初始估算尺寸，交叉轴为固定尺寸。")]
        [SerializeField] private Vector2 _itemSize = new Vector2(620f, 80f);
        [Min(0f)] [SerializeField] private float _spacing = 8f;
        [Tooltip("垂直方向允许多列，水平方向允许多行；容量由视口交叉轴空间自动计算。")]
        [SerializeField] private bool _allowMultiple;
        [SerializeField] private bool _limitGridCount;
        [Tooltip("启用数量上限时才生效，实际数量仍受视口空间限制。")]
        [Min(1)] [SerializeField] private int _gridCount = 4;
        [Tooltip("启用多行/列时使用。X 为水平间距，Y 为垂直间距。")]
        [SerializeField] private Vector2 _gridSpacing = new Vector2(8f, 8f);
        [Min(0f)] [SerializeField] private float _paddingStart;
        [Min(0f)] [SerializeField] private float _paddingEnd;
        [Min(0f)] [SerializeField] private float _bufferSize = 200f;
        [Tooltip("启用后单列/单行内容按周期无限循环；需要 3 个周期，内容长度必须大于视口。")]
        [SerializeField] private bool _loopEnabled;
        [Min(3)] [SerializeField] private int _loopRepeat = 3;
        [Tooltip("可变尺寸布局重排时，保持首个可见 Item 相对视口的位置。")]
        [SerializeField] private bool _preserveScrollAnchor = true;
        [Tooltip("尺寸变化前已停在末尾时，变化后继续贴住末尾。适用于聊天列表。")]
        [SerializeField] private bool _keepEndOnSizeChange;
        [Tooltip("增量增删/更新前已位于末尾时，变更后继续贴住末尾；浏览历史内容时不强制跳转。")]
        [SerializeField] private bool _keepEndOnDataChange;

        [Header("Item 模板")]
        [SerializeField] private int _defaultItemType;
        [SerializeField] private ItemTemplate[] _itemTemplates = Array.Empty<ItemTemplate>();

        private readonly List<SceneTemplateState> _sceneTemplates = new();
        private readonly Dictionary<int, GameObject> _runtimeTemplates = new();
        private UGUIVirtualList _list;
        private UGUIVirtualList _retiringList;
        private UGUIFlowLayout _flowLayout;
        private bool _initializing;
        private bool _destroyed;
        private bool _releasing;

        public ScrollRect ScrollRect => _scrollRect;
        public UGUIVirtualListLayoutMode LayoutMode => _layoutMode == UGUIVirtualListLayoutMode.FixedGrid
            ? UGUIVirtualListLayoutMode.FixedLinear : _layoutMode;
        public bool AllowMultiple => (_listType == UGUIVirtualListType.Flat || _listType == UGUIVirtualListType.Paged) &&
            (_allowMultiple || _layoutMode == UGUIVirtualListLayoutMode.FixedGrid);
        public int ActualCountPerLine => _flowLayout?.CountPerLine ?? 1;
        /// <summary>当前是否启用了无限循环（初始化后才有意义）。</summary>
        public bool IsLoop => _list != null && _list.IsLoop;
        public int LoopCycleCount => _loopRepeat;
        /// <summary>最近一次成功初始化使用的实际尺寸；初始化前为零。</summary>
        public Vector2 ResolvedItemSize { get; private set; }
        public bool IsInitialized => _list != null;
        public int Count => _list?.Count ?? 0;

        /// <summary>可选：返回模板 TypeId。未设置时使用 Default Item Type。</summary>
        public Func<int, object, int> ItemTypeProvider { get; set; }

        /// <summary>
        /// 可选：绑定数据。没有 Item 脚本时也可通过该回调设置克隆对象的文本、图片及尺寸。
        /// 如模板实现了 IUGUIVirtualListItem，则先执行其 Bind，再执行此回调。
        /// </summary>
        public Action<int, object, GameObject> ItemRenderer { get; set; }

        /// <summary>回收时清理异步任务、资源引用等。此时索引和数据仍对应上次绑定。</summary>
        public event Action<int, object, GameObject> ItemRecycled;
        public event Action<int, object> ItemClicked;

        /// <summary>需要底层高级 API 时使用。首次访问会初始化。</summary>
        public UGUIVirtualList Controller
        {
            get
            {
                Initialize();
                return _list;
            }
        }

        private void Reset()
        {
            _scrollRect = GetComponent<ScrollRect>();
            // 新增组件默认从模板读取尺寸。老场景缺省枚举值仍是 Manual，保留原来的尺寸配置。
            _itemSizeMode = UGUIVirtualListItemSizeMode.FromTemplate;
            _listTypeConfigured = true;
        }

        private void OnValidate() => MigrateLegacyLayout();

        private void MigrateLegacyLayout()
        {
            if (_layoutMode != UGUIVirtualListLayoutMode.FixedGrid)
                return;
            _layoutMode = UGUIVirtualListLayoutMode.FixedLinear;
            _allowMultiple = true;
            _limitGridCount = true;
        }

        private void Start()
        {
            Initialize();
        }

        private void LateUpdate()
        {
            // 消费视口缩放/旋转及上一帧未收敛的布局；换行不重新 Bind 或让令牌失效。
            _list?.RefreshViewportIfChanged();
            TickPaging();
            TickGrouping();
            TickNavigation();
            TickCarousel();
        }

        private void OnEnable()
        {
            // 禁用组件时保留数据；重新显示时按当前视口重新布局。
            _list?.Refresh();
        }

        /// <summary>
        /// 幂等初始化。SetDatas 会主动调用，不依赖 Start 顺序。
        /// 初始化前完成 Inspector 配置；布局参数修改后需 Release 再重新 SetDatas。
        /// </summary>
        public void Initialize()
        {
            if (_destroyed)
                throw new ObjectDisposedException(nameof(UGUIVirtualListComponent));
            if (_releasing || (_retiringList != null && !_retiringList.IsDisposalComplete) ||
                (_retiringSticky != null && !_retiringSticky.IsDisposalComplete))
                throw new InvalidOperationException("列表仍在释放，不能在回收回调中重新初始化；请延后执行。");
            if (_list != null)
                return;
            _retiringList = null;
            _retiringSticky = null;
            if (_initializing)
                throw new InvalidOperationException("虚拟列表正在初始化，请勿在模板生命周期回调中重入初始化。");
            if (!Application.isPlaying)
                throw new InvalidOperationException("UGUIVirtualListComponent 仅在运行时初始化。");

            _initializing = true;
            try
            {
                if (_scrollRect == null)
                    _scrollRect = GetComponent<ScrollRect>();
                MigrateLegacyLayout();
                ValidateConfiguration();

                var prefabs = new Dictionary<int, GameObject>();
                foreach (var entry in _itemTemplates)
                    prefabs.Add(entry.TypeId, entry.Template);
                foreach (var pair in _runtimeTemplates)
                    prefabs[pair.Key] = pair.Value;

                Vector2 itemSize = ResolveItemSize(prefabs[_defaultItemType]);
                var layout = CreateLayout(itemSize);

                // 只修改场景实例，绝不 SetActive/Destroy Project 资源。
                foreach (var entry in _itemTemplates)
                {
                    var template = entry.Template;
                    if (!template.scene.IsValid())
                        continue;
                    _sceneTemplates.Add(new SceneTemplateState(template, template.activeSelf));
                    template.SetActive(false);
                }

                var factory = new UGUIVirtualListPrefabFactory(
                    prefabs,
                    (index, data) => ItemTypeProvider?.Invoke(index, data) ?? _defaultItemType,
                    (type, instance) => new ComponentItem(this, instance));

                _scrollRect.horizontal = EffectiveAxis == UGUIVirtualListAxis.Horizontal;
                _scrollRect.vertical = EffectiveAxis == UGUIVirtualListAxis.Vertical;
                _list = new UGUIVirtualList(_scrollRect, layout, factory)
                {
                    BufferSize = _bufferSize,
                    PreserveScrollAnchor = _layoutMode != UGUIVirtualListLayoutMode.VariableLinear || _preserveScrollAnchor,
                    KeepEndOnSizeChange = _listType == UGUIVirtualListType.Flat &&
                        _layoutMode == UGUIVirtualListLayoutMode.VariableLinear && _keepEndOnSizeChange,
                    KeepEndOnDataChange = _listType == UGUIVirtualListType.Flat && _keepEndOnDataChange
                };
                if (_listType == UGUIVirtualListType.Tree)
                {
                    _treeAdapter = new UGUIVirtualTreeAdapter(_list);
                    _treeAdapter.SetRoots(null);
                }
                if (_listType == UGUIVirtualListType.Grouped)
                {
                    _groupAdapter = new UGUIVirtualGroupAdapter(_list, _showEmptyGroups);
                    _groupAdapter.SetGroups(null);
                    if (_stickyGroupHeaders)
                    {
                        var stickyFactory = new UGUIVirtualListPrefabFactory(prefabs,
                            (index, data) => ItemTypeProvider?.Invoke(index, data) ?? _defaultItemType,
                            (type, instance) => new ComponentItem(this, instance, true));
                        _stickyHeader = new UGUIVirtualStickyHeader(_list, _groupAdapter, stickyFactory);
                        _scrollRect.onValueChanged.AddListener(OnGroupScrolled);
                    }
                }
                _list.ItemClicked += ForwardClick;
                ResolvedItemSize = itemSize;
                _flowLayout = layout as UGUIFlowLayout;
            }
            catch
            {
                var failed = _list;
                _list = null;
                _flowLayout = null;
                DisposeOwnedAdapters();
                DisposeOwnedNavigation();
                DisposeOwnedCarousel();
                DisposeOwnedSelection();
                try { failed?.Dispose(); }
                finally { RestoreSceneTemplates(); }
                throw;
            }
            finally
            {
                _initializing = false;
            }
        }

        /// <summary>
        /// 注入运行时模板（通常由 UGUIVirtualListTemplateCache 异步加载后写入）。
        /// 必须在 Initialize 前调用；已初始化时请先 Release。
        /// </summary>
        public void SetRuntimeTemplate(int typeId, GameObject template)
        {
            if (_destroyed) throw new ObjectDisposedException(nameof(UGUIVirtualListComponent));
            if (template == null) throw new ArgumentNullException(nameof(template));
            if (!(template.transform is RectTransform))
                throw new ArgumentException("运行时模板根节点必须是 RectTransform。", nameof(template));
            if (_list != null)
                throw new InvalidOperationException("列表已初始化，设置运行时模板前请先 Release。");
            _runtimeTemplates[typeId] = template;
        }

        /// <summary>清空运行时模板（资源释放由加载器负责）。</summary>
        public void ClearRuntimeTemplates()
        {
            if (_list != null && _runtimeTemplates.Count > 0)
                throw new InvalidOperationException("列表已初始化，清空运行时模板前请先 Release。");
            _runtimeTemplates.Clear();
        }

        public void SetDatas<T>(IEnumerable<T> datas, int scrollToIndex = -1)
        {
            RequireFlatMode();
            Controller.SetDatas(datas, scrollToIndex);
        }

        public int AddData<T>(T data) { RequireFlatMode(); return Controller.AddData(data); }
        public void AddDatas<T>(IEnumerable<T> datas) { RequireFlatMode(); Controller.AddDatas(datas); }
        public void InsertData<T>(int index, T data) { RequireFlatMode(); Controller.InsertData(index, data); }
        public void InsertDatas<T>(int index, IEnumerable<T> datas) { RequireFlatMode(); Controller.InsertDatas(index, datas); }
        public void RemoveAt(int index) { RequireFlatMode(); Controller.RemoveAt(index); }
        public void RemoveRange(int index, int count) { RequireFlatMode(); Controller.RemoveRange(index, count); }
        public void UpdateData<T>(int index, T data) { RequireFlatMode(); Controller.UpdateData(index, data); }
        public void Clear()
        {
            if (_listType == UGUIVirtualListType.Tree) TreeAdapter.SetRoots(null);
            else if (_listType == UGUIVirtualListType.Grouped) GroupAdapter.SetGroups(null);
            else if (_listType == UGUIVirtualListType.Paged)
                throw new InvalidOperationException("分页列表请使用 RefreshPagesAsync 刷新，或 Release 释放。");
            else Controller.Clear();
        }

        public object GetData(int index) => _list?.GetData(index);
        public void Refresh() => Controller.Refresh();
        public void RefreshLayout() => Controller.RefreshLayout();
        public void RefreshItem(int index) => Controller.RefreshItem(index);

        // 通知入口不自动 Initialize，避免晚到的异步回调在 Release 后重新创建列表。
        public bool NotifyItemSizeChanged(int index) => _list != null && _list.NotifyItemSizeChanged(index);
        public bool NotifyItemSizeChanged(UGUIVirtualListItemToken token) => _list != null && _list.NotifyItemSizeChanged(token);
        public int NotifyVisibleItemSizesChanged() => _list?.NotifyVisibleItemSizesChanged() ?? 0;
        public bool TryGetItemToken(int index, out UGUIVirtualListItemToken token)
        {
            if (_list != null)
                return _list.TryGetItemToken(index, out token);
            token = default;
            return false;
        }

        public void ScrollToView(int index,
            UGUIVirtualListAlignment alignment = UGUIVirtualListAlignment.Nearest)
        {
            Controller.ScrollToView(index, alignment);
        }

        /// <summary>销毁生成的 Item，恢复场景模板的原始激活状态；不销毁模板和 Prefab。</summary>
        public void Release()
        {
            if (_releasing) return;
            if (_list == null)
            {
                DisposeOwnedNavigation();
                DisposeOwnedCarousel();
                DisposeOwnedSelection();
                var cleanup = DisposeOwnedAdapters();
                if (cleanup != null) throw new InvalidOperationException("数据适配器清理失败。", cleanup);
                return;
            }
            _releasing = true;
            var list = _list;
            _retiringList = list;
            _list = null;
            _flowLayout = null;
            try
            {
                var failure = DisposeOwnedAdapters();
                try { DisposeOwnedNavigation(); } catch (Exception ex) { failure ??= ex; }
                try { DisposeOwnedCarousel(); } catch (Exception ex) { failure ??= ex; }
                try { DisposeOwnedSelection(); } catch (Exception ex) { failure ??= ex; }
                list.ItemClicked -= ForwardClick;
                try { list.Dispose(); } catch (Exception ex) { failure ??= ex; }
                if (failure != null) throw new InvalidOperationException("列表释放回调失败，其余资源已继续清理。", failure);
            }
            finally
            {
                try { RestoreSceneTemplates(); }
                finally
                {
                    _releasing = false;
                    if (list.IsDisposalComplete) _retiringList = null;
                }
            }
        }

        private void OnDestroy()
        {
            _destroyed = true;
            try { Release(); }
            finally
            {
                ItemTypeProvider = null;
                ItemRenderer = null;
                StickyHeaderRenderer = null;
                ItemClicked = null;
                ItemRecycled = null;
                StickyHeaderRecycled = null;
            }
        }

        private void ForwardClick(int index, object data)
        {
            var core = _list;
            if (core == null) return;
            if (_listType == UGUIVirtualListType.Paged && _pageStatusClick && data is UGUIVirtualPageItem page && page.IsStatus)
            {
                if (_pagedAdapter != null)
                {
                    if (page.Kind == UGUIVirtualPageItemKind.Error) ObservePageTask(_pagedAdapter.RetryAsync());
                    else if (page.Kind == UGUIVirtualPageItemKind.LoadMore) ObservePageTask(_pagedAdapter.LoadNextPageAsync());
                }
                return; // 状态点击可能同步追加并移动尾项，不能再发送旧 index 的业务点击。
            }
            if (_listType == UGUIVirtualListType.Tree && _treeToggleOnClick && _treeAdapter != null &&
                _treeAdapter.OwnsCurrentData && data is UGUIVirtualTreeItem item && item.HasChildren)
                _treeAdapter.Toggle(item.Id);
            if (ReferenceEquals(_list, core) && !core.IsDisposedOrDisposing)
                ItemClicked?.Invoke(index, data);
        }

        private IUGUIVirtualListLayout CreateLayout(Vector2 size)
        {
            bool multiple = AllowMultiple;
            if (multiple && _limitGridCount && _gridCount < 1)
                throw new InvalidOperationException("最大行/列数至少为 1。");
            Vector2 spacing = multiple ? _gridSpacing
                : EffectiveAxis == UGUIVirtualListAxis.Vertical ? new Vector2(0f, _spacing) : new Vector2(_spacing, 0f);
            var flow = new UGUIFlowLayout(EffectiveAxis, size,
                variableSize: _layoutMode == UGUIVirtualListLayoutMode.VariableLinear,
                allowMultiple: multiple,
                maxCountPerLine: multiple && _limitGridCount ? _gridCount : 0,
                spacing: spacing,
                paddingStart: _loopEnabled ? 0f : _paddingStart,
                paddingEnd: _loopEnabled ? 0f : _paddingEnd);
            if (!_loopEnabled) return flow;
            if (multiple) throw new InvalidOperationException("无限循环只支持单列/单行，请关闭多列。");
            if (_loopRepeat < 3) throw new InvalidOperationException("循环周期数至少为 3。");
            return new UGUIVirtualLoopLayout(flow, _loopRepeat);
        }

        private Vector2 ResolveItemSize(GameObject defaultTemplate)
        {
            Vector2 size = _itemSizeMode == UGUIVirtualListItemSizeMode.FromTemplate
                ? ((RectTransform)defaultTemplate.transform).rect.size
                : _itemSize;
            if (size.x <= 0f || size.y <= 0f || float.IsNaN(size.x) || float.IsNaN(size.y) ||
                float.IsInfinity(size.x) || float.IsInfinity(size.y))
                throw new InvalidOperationException(_itemSizeMode == UGUIVirtualListItemSizeMode.FromTemplate
                    ? "默认模板的 RectTransform 尺寸无效。请设置模板根节点宽高，或切换 Manual 手动指定尺寸。"
                    : "Item Size 必须为有限正数。");
            return size;
        }

        private void ValidateConfiguration()
        {
            if (_scrollRect == null || _scrollRect.viewport == null || _scrollRect.content == null)
                throw new InvalidOperationException("请配置 ScrollRect 及其 Viewport / Content。");
            if (_listType != UGUIVirtualListType.Flat && _listType != UGUIVirtualListType.Tree &&
                _listType != UGUIVirtualListType.Paged && _listType != UGUIVirtualListType.Grouped)
                throw new InvalidOperationException("不支持的列表类型。");
            if (_layoutMode != UGUIVirtualListLayoutMode.FixedLinear &&
                _layoutMode != UGUIVirtualListLayoutMode.VariableLinear &&
                _layoutMode != UGUIVirtualListLayoutMode.FixedGrid)
                throw new InvalidOperationException("不支持的虚拟列表布局方式。");
            if ((_listType == UGUIVirtualListType.Flat || _listType == UGUIVirtualListType.Paged) &&
                _axis != UGUIVirtualListAxis.Vertical && _axis != UGUIVirtualListAxis.Horizontal)
                throw new InvalidOperationException("不支持的滚动方向。");
            if (_itemSizeMode != UGUIVirtualListItemSizeMode.Manual &&
                _itemSizeMode != UGUIVirtualListItemSizeMode.FromTemplate)
                throw new InvalidOperationException("不支持的 Item 尺寸来源。");
            if (float.IsNaN(_bufferSize) || float.IsInfinity(_bufferSize))
                throw new InvalidOperationException("视口外缓冲距离必须是有限数值。");
            if (_loopEnabled && _listType != UGUIVirtualListType.Flat)
                throw new InvalidOperationException("无限循环只支持普通列表类型。");
            if (_loopEnabled && _loopRepeat < 3)
                throw new InvalidOperationException("循环周期数至少为 3。");
            if (_listType == UGUIVirtualListType.Paged && (_pageSize < 1 || (_autoLoadPages && _pagePreloadItems < 0)))
                throw new InvalidOperationException("分页每页数量至少为 1，预加载项数不能为负数。");
            if (_itemTemplates == null || _itemTemplates.Length == 0)
                throw new InvalidOperationException("请至少配置一个 Item Template。");
            if (_scrollRect.content.GetComponent<LayoutGroup>() != null ||
                _scrollRect.content.GetComponent<ContentSizeFitter>() != null)
                throw new InvalidOperationException("Content 不能挂 LayoutGroup / ContentSizeFitter，列表会自行排布。");

            var ids = new HashSet<int>();
            var templates = new HashSet<GameObject>();
            foreach (var entry in _itemTemplates)
            {
                if (entry == null || entry.Template == null)
                    throw new InvalidOperationException("Item Template 不能为空。");
                if (!ids.Add(entry.TypeId))
                    throw new InvalidOperationException($"Item Template TypeId 重复：{entry.TypeId}");
                if (!templates.Add(entry.Template))
                    throw new InvalidOperationException("同一模板对象不能重复配置。");
                if (!(entry.Template.transform is RectTransform))
                    throw new InvalidOperationException($"模板 {entry.Template.name} 的根节点必须为 RectTransform。");
                var root = entry.Template.transform;
                if (transform.IsChildOf(root) || _scrollRect.transform.IsChildOf(root) ||
                    _scrollRect.viewport.IsChildOf(root) || _scrollRect.content.IsChildOf(root))
                    throw new InvalidOperationException("Item 模板不能是组件、ScrollRect、Viewport、Content 或它们的祖先。");
            }
            if (!ids.Contains(_defaultItemType))
                throw new InvalidOperationException($"Default Item Type 没有对应模板：{_defaultItemType}");
        }

        private void RestoreSceneTemplates()
        {
            foreach (var state in _sceneTemplates)
            {
                if (state.Template != null)
                    state.Template.SetActive(state.ActiveSelf);
            }
            _sceneTemplates.Clear();
        }

        private readonly struct SceneTemplateState
        {
            public readonly GameObject Template;
            public readonly bool ActiveSelf;
            public SceneTemplateState(GameObject template, bool activeSelf)
            {
                Template = template;
                ActiveSelf = activeSelf;
            }
        }

        /// <summary>普通 C# 包装器，允许模板不挂任何自定义 Item MonoBehaviour。</summary>
        private sealed class ComponentItem : IUGUIVirtualListItem
        {
            private readonly UGUIVirtualListComponent _owner;
            private readonly IUGUIVirtualListItem _inner;
            private readonly Button _button;
            private readonly bool _isSticky;
            private UnityAction _clickAction;
            private int _index = -1;
            private object _data;

            public GameObject GameObject { get; }
            public RectTransform RectTransform { get; }

            public ComponentItem(UGUIVirtualListComponent owner, GameObject instance, bool sticky = false)
            {
                _owner = owner;
                _isSticky = sticky;
                GameObject = instance;
                RectTransform = (RectTransform)instance.transform;
                _inner = instance.GetComponent<IUGUIVirtualListItem>();
                _button = instance.GetComponent<Button>();
            }

            public void Bind(int index, object data)
            {
                _index = index;
                _data = data;
                _inner?.Bind(index, data);
                if (_isSticky) (_owner.StickyHeaderRenderer ?? _owner.ItemRenderer)?.Invoke(index, data, GameObject);
                else _owner.ItemRenderer?.Invoke(index, data, GameObject);
            }

            public Vector2 Measure()
            {
                if (_inner != null)
                    return _inner.Measure();
                LayoutRebuilder.ForceRebuildLayoutImmediate(RectTransform);
                return RectTransform.rect.size;
            }

            public void OnRecycle()
            {
                if (GameObject != null)
                {
                    if (_isSticky) _owner.StickyHeaderRecycled?.Invoke(_index, _data, GameObject);
                    else _owner.ItemRecycled?.Invoke(_index, _data, GameObject);
                    _inner?.OnRecycle();
                }
                _index = -1;
                _data = null;
            }

            public void SetClickHandler(Action callback)
            {
                if (GameObject == null)
                    return;
                if (_inner != null)
                {
                    _inner.SetClickHandler(callback);
                    return;
                }
                if (_button == null)
                    return;
                if (_clickAction != null)
                    _button.onClick.RemoveListener(_clickAction);
                _clickAction = callback == null ? null : new UnityAction(callback);
                if (_clickAction != null)
                    _button.onClick.AddListener(_clickAction);
            }
        }
    }
}
