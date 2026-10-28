using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ux.UI
{
    /// <summary>通过统一列表组件展示树，Item 仍由同一套虚拟化和对象池管理。</summary>
    public sealed class UGUIVirtualTreeDemo : MonoBehaviour
    {
        [SerializeField] private UGUIVirtualListComponent _list;
        [Min(1)] [SerializeField] private int _rootCount = 20;
        [Min(1)] [SerializeField] private int _branchesPerRoot = 5;
        [Min(1)] [SerializeField] private int _leavesPerBranch = 20;
        [Min(0f)] [SerializeField] private float _indent = 24f;

        private readonly Dictionary<GameObject, Text> _texts = new();
        private UGUIVirtualTreeAdapter _tree;
        private bool _subscribed;
        public UGUIVirtualTreeAdapter Adapter => _tree;

        private void Start()
        {
            if (_list == null) _list = GetComponent<UGUIVirtualListComponent>();
            if (_list == null)
            {
                Debug.LogError("请先执行 UxGame/Test/Create UGUI Tree Demo。", this);
                return;
            }
            // 兼容旧版树 Demo 场景：新加的列表类型字段尚未配置时，迁移为空树模式。
            if (!_list.IsListTypeConfigured && _list.ListType == UGUIVirtualListType.Flat)
                _list.ConfigureListType(UGUIVirtualListType.Tree);
            if (_list.ListType != UGUIVirtualListType.Tree)
            {
                Debug.LogWarning("树 Demo 需要把列表类型选为树形列表，或重新执行 Create UGUI Tree Demo。", this);
                return;
            }
            _list.ItemTypeProvider = GetItemType;
            _list.ItemRenderer = BindItem;
            _list.ItemClicked += OnItemClicked;
            _list.ItemRecycled += OnItemRecycled;
            _subscribed = true;
            _tree = _list.TreeAdapter;
            _list.SetTreeRoots(CreateRoots());
            Debug.Log($"[UGUIVirtualTreeDemo] 总节点 {_tree.TotalNodeCount}，当前展开行 {_tree.VisibleCount}。点击分类/分组展开折叠。", this);
        }

        private int GetItemType(int index, object data) => ((UGUIVirtualTreeItem)data).HasChildren ? 0 : 1;

        private void BindItem(int index, object data, GameObject item)
        {
            var row = (UGUIVirtualTreeItem)data;
            if (!_texts.TryGetValue(item, out var text) || text == null)
            {
                text = item.GetComponentInChildren<Text>(true);
                if (text == null) throw new System.InvalidOperationException("树 Item 模板需要包含 UGUI Text。");
                _texts[item] = text;
            }
            var rect = (RectTransform)item.transform;
            var offset = text.rectTransform.offsetMin;
            offset.x = 18f + Mathf.Min(row.Depth * _indent, Mathf.Max(0f, rect.rect.width - 140f));
            text.rectTransform.offsetMin = offset;
            text.text = (row.HasChildren ? row.IsExpanded ? "[-] " : "[+] " : "    ") + (row.Data?.ToString() ?? row.Id);
            text.alignment = TextAnchor.MiddleLeft;
            text.verticalOverflow = _list.LayoutMode == UGUIVirtualListLayoutMode.VariableLinear
                ? VerticalWrapMode.Overflow : VerticalWrapMode.Truncate;
            item.name = $"Tree Item [{index}] {row.Id}";
            if (_list.LayoutMode == UGUIVirtualListLayoutMode.VariableLinear)
            {
                float min = row.HasChildren ? 64f : 48f;
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(min, Mathf.Ceil(text.preferredHeight) + 20f));
            }
        }

        private void OnItemClicked(int index, object data)
        {
            var item = (UGUIVirtualTreeItem)data;
            // 是否点击展开由组件的“点击节点展开/折叠”开关控制，此处不能再 Toggle 一次。
            Debug.Log($"[UGUIVirtualTreeDemo] 节点 {item.Id}，层级 {item.Depth}，展开 {item.IsExpanded}，显示行 {_tree.VisibleCount}", this);
        }

        private void OnItemRecycled(int index, object data, GameObject item)
        {
            if (_texts.TryGetValue(item, out var text) && text != null) text.text = string.Empty;
        }

        [ContextMenu("测试/展开第一个分类")]
        public void ExpandFirstRoot()
        {
            if (CanRun()) _list.ExpandTreeNode("root-0");
        }

        [ContextMenu("测试/折叠第一个分类")]
        public void CollapseFirstRoot()
        {
            if (CanRun()) _list.CollapseTreeNode("root-0");
        }

        [ContextMenu("测试/展开祖先并定位最后一个叶子")]
        public void ScrollToLastLeaf()
        {
            if (CanRun()) _list.ScrollToTreeNode($"root-{_rootCount - 1}/branch-{_branchesPerRoot - 1}/leaf-{_leavesPerBranch - 1}");
        }

        [ContextMenu("测试/更新第一个叶子文本")]
        public void UpdateFirstLeaf()
        {
            if (CanRun()) _list.UpdateTreeNodeData("root-0/branch-0/leaf-0", "已更新的叶子\n不同 Item 类型与不定高仍由虚拟列表处理。");
        }

        private bool CanRun()
        {
            if (Application.isPlaying && _tree != null && _tree.OwnsCurrentData && _list != null && _list.IsInitialized)
                return true;
            Debug.LogWarning("请先运行树形 Demo。", this);
            return false;
        }

        private List<UGUIVirtualTreeNode> CreateRoots()
        {
            var roots = new List<UGUIVirtualTreeNode>();
            for (int r = 0; r < _rootCount; r++)
            {
                var branches = new List<UGUIVirtualTreeNode>();
                for (int b = 0; b < _branchesPerRoot; b++)
                {
                    var leaves = new List<UGUIVirtualTreeNode>();
                    for (int i = 0; i < _leavesPerBranch; i++)
                        leaves.Add(new UGUIVirtualTreeNode($"root-{r}/branch-{b}/leaf-{i}", $"条目 {r:D2}-{b:D2}-{i:D2}"));
                    branches.Add(new UGUIVirtualTreeNode($"root-{r}/branch-{b}", $"分组 {r:D2}-{b:D2}", leaves, r == 0 && b == 0));
                }
                roots.Add(new UGUIVirtualTreeNode($"root-{r}", $"分类 {r:D2}", branches, r == 0));
            }
            return roots;
        }

        private void OnDestroy()
        {
            bool owns = _subscribed && _tree != null && _tree.OwnsCurrentData;
            if (_subscribed && _list != null)
            {
                _list.ItemClicked -= OnItemClicked;
                _list.ItemRecycled -= OnItemRecycled;
                if (_list.ItemRenderer == BindItem) _list.ItemRenderer = null;
                if (_list.ItemTypeProvider == GetItemType) _list.ItemTypeProvider = null;
            }
            // 适配器由组件管理，Release 会统一释放它。
            _tree = null;
            _texts.Clear();
            if (owns && _list != null) _list.Release();
        }
    }
}
