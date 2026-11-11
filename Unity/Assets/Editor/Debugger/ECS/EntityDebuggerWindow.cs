#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ux.Editor.Debugger.ECS
{
    /// <summary>
    /// ECS 实体可视化窗口。
    ///
    /// 和旧的「每实体一个 GameObject + Inspector 画 IMGUI」相比：
    ///   1) 实体列表走 UIElements TreeView（虚拟化 + 按父子关系展示），实体上千也不卡；
    ///   2) 不依赖实体有 GameObject：纯逻辑实体同样能在这里查看；
    ///   3) 详情面板复用 <see cref="ReflectInspector"/>（反射结果已按类型缓存）；
    ///   4) 刷新做了节流，不会每帧重建列表。
    ///
    /// 实体可视化只在这里：运行时 Entity 与编辑器解耦。
    /// </summary>
    public class EntityDebuggerWindow : EditorWindow
    {
        [MenuItem("UxGame/调试/ECS 实体", false, 420)]
        public static void Open()
        {
            var window = GetWindow<EntityDebuggerWindow>("ECS 实体", true, DebuggerEditorDefine.DebuggerWindowTypes);
            window.minSize = new Vector2(900, 500);
        }

        private const double RefreshInterval = 0.5;

        private TreeView _tree;
        private TextField _search;
        private Label _countLabel;
        private ScrollView _detailHost;

        private readonly List<List<Entity>> _childBuffers = new();
        private readonly List<TreeViewItemData<TreeNode>> _roots = new();
        private readonly Dictionary<Entity, ReflectView> _detailCache = new();
        private ReflectView _detailView;

        /// <summary>
        /// 树节点。Entity 为 null 表示分组节点（Component / Entity），只用于分层，不参与选中。
        /// </summary>
        private readonly struct TreeNode
        {
            public readonly Entity Entity;
            public readonly string Label;

            public TreeNode(Entity entity, string label)
            {
                Entity = entity;
                Label = label;
            }

            public bool IsGroup => Entity == null;
        }


        private Entity _selected;
        private string _lastSearch = string.Empty;
        private int _lastCount = -1;
        private double _nextRefresh;
        private int _nextItemId;

        public void CreateGUI()
        {
            var root = new VisualElement();
            root.style.flexDirection = FlexDirection.Row;
            root.style.flexGrow = 1;

            // ---------- 左：工具栏 + 搜索 + 实体树 ----------
            var left = new VisualElement();
            left.style.width = 340;
            left.style.flexShrink = 0;
            left.style.borderRightWidth = 1;
            left.style.borderRightColor = new Color(0.15f, 0.15f, 0.15f);

            var toolbar = new VisualElement();
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.marginBottom = 2;

            toolbar.Add(new Button(() =>
            {
                RefreshTree(force: true);
                _detailView?.Refresh();
            })
            { text = "刷新" });

            toolbar.Add(new Button(() => _detailView?.Rebuild()) { text = "重建" });

            toolbar.Add(new Button(() =>
            {
                _detailCache.Clear();
                RebuildDetail();
            })
            { text = "全部重建" });

            left.Add(toolbar);

            _search = new TextField("搜索");
            _search.RegisterValueChangedCallback(_ => RefreshTree(true));
            left.Add(_search);

            _countLabel = new Label();
            _countLabel.style.marginLeft = 4;
            _countLabel.style.marginBottom = 2;
            left.Add(_countLabel);

            _tree = new TreeView
            {
                makeItem = () =>
                {
                    // 撑满 TreeView 行高，Label 才能垂直居中（否则只按文字高度居中，看起来偏下）。
                    var row = new VisualElement();
                    row.style.flexDirection = FlexDirection.Row;
                    row.style.flexGrow = 1;
                    row.style.height = Length.Percent(100);
                    row.style.alignItems = Align.Stretch;

                    var label = new Label();
                    label.style.flexGrow = 1;
                    label.style.marginTop = 0;
                    label.style.marginBottom = 0;
                    label.style.paddingLeft = 4;
                    label.style.unityTextAlign = TextAnchor.MiddleLeft;
                    row.Add(label);
                    return row;
                },
                bindItem = (element, index) =>
                {
                    var node = _tree.GetItemDataForIndex<TreeNode>(index);
                    var label = element.Q<Label>();
                    label.text = node.IsGroup ? node.Label : node.Entity.Name;
                    label.style.unityFontStyleAndWeight = node.IsGroup ? FontStyle.Bold : FontStyle.Normal;
                },
                selectionType = SelectionType.Single,
            };
            _tree.style.flexGrow = 1;
            _tree.selectionChanged += OnSelectionChanged;
            left.Add(_tree);

            // ---------- 右：详情 ----------
            _detailHost = new ScrollView();
            _detailHost.style.flexGrow = 1;
            _detailHost.style.paddingLeft = 6;

            root.Add(left);
            root.Add(_detailHost);
            rootVisualElement.Add(root);

            EditorApplication.update += OnUpdate;
            RefreshTree(true);
        }

        private void OnDestroy()
        {
            EditorApplication.update -= OnUpdate;
            _detailCache.Clear();
            _detailView = null;
        }

        private void OnUpdate()
        {
            if (EditorApplication.timeSinceStartup < _nextRefresh) return;

            _nextRefresh = EditorApplication.timeSinceStartup + RefreshInterval;
            RefreshTree(false);
            _detailView?.Refresh();
        }

        /// <summary>
        /// 只在「搜索词变化」或「实体总数变化」时重建树，避免定时刷新把展开状态和选中项冲掉。
        /// </summary>
        private void RefreshTree(bool force)
        {
            string keyword = _search?.value ?? string.Empty;
            int total = EditorEntityRegistry.Count;

            if (!force && keyword == _lastSearch && total == _lastCount) return;

            _lastSearch = keyword;
            _lastCount = total;

            _roots.Clear();
            _nextItemId = 0;

            var all = EditorEntityRegistry.All;

            if (keyword.Length == 0)
            {
                // 完整树：从根实体（无父）开始递归。
                for (int i = 0; i < all.Count; i++)
                {
                    var entity = all[i];
                    if (entity == null || entity.IsDestroy) continue;
                    if (entity.Parent != null) continue;
                    _roots.Add(BuildEntityNode(entity, 0));
                }
            }
            else
            {
                // 搜索模式：平铺显示匹配项。
                for (int i = 0; i < all.Count; i++)
                {
                    var entity = all[i];
                    if (entity == null || entity.IsDestroy) continue;
                    if (!Match(entity, keyword)) continue;
                    _roots.Add(new TreeViewItemData<TreeNode>(NextItemId(), new TreeNode(entity, null)));
                }
            }

            _tree.SetRootItems(_roots);
            _tree.Rebuild();
            _tree.selectedIndex = -1;
            _selected = null;
            RebuildDetail();

            _countLabel.text = keyword.Length == 0
                ? $"实体 {total}（根 {_roots.Count}）"
                : $"匹配 {_roots.Count} / {total}";
        }

        /// <summary>
        /// 按层级取子实体缓冲。BuildNode 是递归的，共用同一个 List 会被下一层 GetChildren 清空，
        /// 导致每层只渲染出第一个子实体，后面的全部丢失。
        /// </summary>
        private List<Entity> ChildBuffer(int depth)
        {
            while (_childBuffers.Count <= depth)
            {
                _childBuffers.Add(new List<Entity>());
            }
            return _childBuffers[depth];
        }

        /// <summary>
        /// 本项目没有 Component 基类，组件全是按命名约定派生的 Entity（xxxComponent），
        /// 所以只能按类型名区分；改名成别的后缀会落到 Entity 分组里。
        /// </summary>
        private static bool IsComponent(Entity entity)
        {
            return entity.GetType().Name.EndsWith("Component");
        }

        private TreeViewItemData<TreeNode> BuildEntityNode(Entity entity, int depth)
        {
            var children = ChildBuffer(depth);
            entity.GetChildren(children);

            List<TreeViewItemData<TreeNode>> componentItems = null;
            List<TreeViewItemData<TreeNode>> entityItems = null;

            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i];
                if (child == null || child.IsDestroy) continue;

                var item = BuildEntityNode(child, depth + 1);
                if (IsComponent(child))
                {
                    (componentItems ??= new List<TreeViewItemData<TreeNode>>()).Add(item);
                }
                else
                {
                    (entityItems ??= new List<TreeViewItemData<TreeNode>>()).Add(item);
                }
            }

            List<TreeViewItemData<TreeNode>> childItems;
            if (componentItems != null && entityItems != null)
            {
                // 只有两种子节点都存在时才分目录，避免单一类型多出一层没用的空壳。
                childItems = new List<TreeViewItemData<TreeNode>>(2);
                // 组件排在子实体前面，和 Add 的顺序一致。
                childItems.Add(GroupNode($"Component ({componentItems.Count})", componentItems));
                childItems.Add(GroupNode($"Entity ({entityItems.Count})", entityItems));
            }
            else
            {
                childItems = componentItems ?? entityItems;
            }

            return new TreeViewItemData<TreeNode>(NextItemId(), new TreeNode(entity, null), childItems);
        }

        private TreeViewItemData<TreeNode> GroupNode(string label, List<TreeViewItemData<TreeNode>> children)
        {
            return new TreeViewItemData<TreeNode>(NextItemId(), new TreeNode(null, label), children);
        }

        private int NextItemId() => ++_nextItemId;

        private static bool Match(Entity entity, string keyword)
        {
            string name = entity.Name;
            if (!string.IsNullOrEmpty(name) &&
                name.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            return entity.GetType().Name.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void OnSelectionChanged(IEnumerable<object> selection)
        {
            // 分组节点没有对应实体，选中它就清空详情。
            _selected = selection.FirstOrDefault() is TreeNode node ? node.Entity : null;
            RebuildDetail();
        }

        private void RebuildDetail()
        {
            _detailHost.Clear();
            _detailView = null;

            if (_selected == null || _selected.IsDestroy) return;

            var header = new VisualElement();
            header.Add(new Label($"名称：{_selected.Name}"));

            // Viewer 是 Link() 时挂到场景对象上的组件；被销毁后 Unity 的伪 null 会在这里判空。
            var viewer = _selected.Viewer;
            if (viewer != null)
            {
                var go = viewer.gameObject;
                var linkRow = new VisualElement();
                linkRow.style.flexDirection = FlexDirection.Row;
                linkRow.Add(new Label($"场景对象：{go.name}"));
                var locate = new Button(() =>
                {
                    Selection.activeGameObject = go;
                    EditorGUIUtility.PingObject(go);
                })
                {
                    text = "定位",
                };
                locate.style.marginLeft = 4;
                linkRow.Add(locate);
                header.Add(linkRow);
            }

            header.Add(new Label($"类型：{_selected.GetType().FullName}"));
            header.Add(new Label($"ID：{_selected.ID}"));
            header.Add(new Label($"父实体：{(_selected.Parent != null ? _selected.Parent.Name : "无")}"));

            var children = ChildBuffer(0);
            _selected.GetChildren(children);
            header.Add(new Label($"子实体：{children.Count}"));
            header.Add(new VisualElement { style = { height = 6 } });
            _detailHost.Add(header);

            if (!_detailCache.TryGetValue(_selected, out var view) || view == null)
            {
                view = ReflectInspector.Build(_selected);
                _detailCache[_selected] = view;
            }

            _detailView = view;
            _detailHost.Add(view);
        }
    }
}
#endif
