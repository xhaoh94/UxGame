using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ux.UI
{
    /// <summary>
    /// 只负责提供测试数据和绑定内容；布局、模板与对象池由 Inspector 上的列表组件管理。
    /// </summary>
    public sealed class UGUIVirtualListDemo : MonoBehaviour
    {
        [SerializeField] private UGUIVirtualListComponent _list;
        [Min(1)] [SerializeField] private int _dataCount = 1000;

        private readonly Dictionary<GameObject, Text> _texts = new();
        private bool _subscribed;
        private int _nextRowId;

        private void Start()
        {
            if (_list == null)
                _list = GetComponent<UGUIVirtualListComponent>();
            if (_list == null)
            {
                Debug.LogError("请先执行 UxGame/Test/Create UGUI VirtualList Demo 配置列表组件。", this);
                return;
            }

            if (_list.ListType != UGUIVirtualListType.Flat)
            {
                Debug.LogWarning("当前不是普通列表类型，请使用对应的树形、分页或分组数据源 Demo。", this);
                return;
            }
            _list.ItemRenderer = BindItem;
            _list.ItemClicked += OnItemClicked;
            _list.ItemRecycled += OnItemRecycled;
            _subscribed = true;
            _list.SetDatas(CreateDatas());
        }

        private void OnDestroy()
        {
            // 组件负责 Release，Demo 不再重复创建或销毁底层列表。
            if (_subscribed && _list != null)
            {
                _list.ItemClicked -= OnItemClicked;
                _list.ItemRecycled -= OnItemRecycled;
                if (_list.ItemRenderer == BindItem)
                    _list.ItemRenderer = null;
            }
            _texts.Clear();
        }

        private void BindItem(int index, object data, GameObject item)
        {
            var row = (DemoRow)data;
            if (!_texts.TryGetValue(item, out var text) || text == null)
            {
                text = item.GetComponentInChildren<Text>(true);
                if (text == null)
                    throw new System.InvalidOperationException("Demo Item 模板需要包含 UGUI Text 子节点。");
                _texts[item] = text;
            }

            item.name = $"Demo Item [{index}] id={row.Id}";
            text.verticalOverflow = _list.LayoutMode == UGUIVirtualListLayoutMode.VariableLinear
                ? VerticalWrapMode.Overflow : VerticalWrapMode.Truncate;
            if (_list.AllowMultiple && _list.LayoutMode == UGUIVirtualListLayoutMode.FixedLinear)
            {
                text.text = $"道具 {row.Id:D4}\n数量：{row.Id % 99 + 1 + row.Revision}";
                text.alignment = TextAnchor.MiddleCenter;
                // 网格的尺寸完全由布局决定，不根据聊天文本撑高单元格。
                return;
            }
            text.text = row.Text;
            text.alignment = TextAnchor.MiddleLeft;
            var background = item.GetComponent<UnityEngine.UI.Image>();
            if (background != null)
                background.color = _list.IsSelected(index)
                    ? new Color(0.24f, 0.44f, 0.72f, 0.98f)
                    : new Color(0.12f, 0.16f, 0.23f, 0.96f);
            if (_list.LayoutMode != UGUIVirtualListLayoutMode.VariableLinear)
                return;

            // 列表已在 Bind 前设置行宽。这里使用 UGUI 的文本布局结果，而不是按字数猜测高度。
            // FixedLinear 会使用 Inspector 的固定尺寸；VariableLinear 会测量这里设置的实际高度。
            float height = Mathf.Max(76f, Mathf.Ceil(text.preferredHeight) + 20f);
            ((RectTransform)item.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }

        private void OnItemRecycled(int index, object data, GameObject item)
        {
            if (_texts.TryGetValue(item, out var text) && text != null)
                text.text = string.Empty;
        }

        private void OnItemClicked(int index, object data)
        {
            var row = (DemoRow)data;
            if (_list.SelectionMode != UGUIVirtualSelectionMode.None)
            {
                _list.ToggleSelection(index);
                Debug.Log($"[UGUIVirtualListDemo] 选择 {index} => {_list.IsSelected(index)}，共 {_list.SelectedCount} 项");
                return;
            }
            Debug.Log(_list.AllowMultiple && _list.LayoutMode == UGUIVirtualListLayoutMode.FixedLinear
                ? $"[UGUIVirtualListDemo] 点击道具 index={index}, id={row.Id}"
                : $"[UGUIVirtualListDemo] 点击 index={index}, id={row.Id}, text={row.Text}");
        }

        [ContextMenu("测试/延迟展开视口前的缓冲 Item")]
        public void SimulateAsyncSizeChange()
        {
            if (!Application.isPlaying || _list == null || !_list.IsInitialized ||
                _list.LayoutMode != UGUIVirtualListLayoutMode.VariableLinear)
            {
                Debug.LogWarning("请先运行可变尺寸 Demo，再执行此测试。", this);
                return;
            }
            int first = _list.Controller.FirstVisibleIndex;
            int index = Mathf.Max(0, first - _list.ActualCountPerLine);
            if (!_list.TryGetItemToken(index, out var token) && !_list.TryGetItemToken(first, out token))
                return;
            StartCoroutine(ExpandAfterDelay(token));
        }

        private IEnumerator ExpandAfterDelay(UGUIVirtualListItemToken token)
        {
            yield return new WaitForSecondsRealtime(1f);
            // 必须先检查令牌再修改对象。滚动复用、刷新或数据替换后，旧任务不再写回。
            if (!token.IsCurrent)
            {
                Debug.Log("[UGUIVirtualListDemo] 旧异步回调已忽略：Item 被回收或重新绑定。");
                yield break;
            }
            var row = (DemoRow)_list.GetData(token.Index);
            row.Text += "\n【模拟图片加载完成】\n新增内容第 1 行\n新增内容第 2 行\n新增内容第 3 行";
            BindItem(token.Index, row, token.GameObject);
            _list.NotifyItemSizeChanged(token);
            Debug.Log($"[UGUIVirtualListDemo] Item {token.Index} 尺寸已更新，列表没有重新绑定数据。", this);
        }

        [ContextMenu("测试/切换选择模式（无/单选/多选）")]
        public void CycleSelectionMode()
        {
            if (!CanRunDataTest()) return;
            var next = (UGUIVirtualSelectionMode)(((int)_list.SelectionMode + 1) % 3);
            _list.SelectionMode = next;
            _list.ClearSelection();
            _list.Refresh();
            Debug.Log($"[UGUIVirtualListDemo] 选择模式：{next}");
        }

        [ContextMenu("测试/选中前10项")]
        public void SelectFirstTen()
        {
            if (!CanRunDataTest()) return;
            _list.SelectionMode = UGUIVirtualSelectionMode.Multiple;
            _list.SelectRange(0, 9);
        }

        [ContextMenu("测试/平滑滚动到第500项（居中）")]
        public void SmoothScrollToMiddleItem()
        {
            if (!CanRunDataTest()) return;
            _list.ScrollTo(Mathf.Min(500, _list.Count - 1), UGUIVirtualListAlignment.Center);
        }

        [ContextMenu("测试/停止滚动")]
        public void StopScrolling()
        {
            if (!CanRunDataTest()) return;
            _list.StopScroll();
        }

        [ContextMenu("测试/增量追加10条")]
        public void AppendRows()
        {
            if (CanRunDataTest()) _list.AddDatas(CreateRows(10));
        }

        [ContextMenu("测试/头部插入10条历史数据")]
        public void PrependRows()
        {
            if (CanRunDataTest()) _list.InsertDatas(0, CreateRows(10));
        }

        [ContextMenu("测试/删除首个可见项")]
        public void RemoveFirstVisibleRow()
        {
            if (!CanRunDataTest()) return;
            int index = _list.Controller.FirstVisibleIndex;
            if (index >= 0) _list.RemoveAt(index);
        }

        [ContextMenu("测试/更新首个可见项")]
        public void UpdateFirstVisibleRow()
        {
            if (!CanRunDataTest()) return;
            int index = _list.Controller.FirstVisibleIndex;
            if (index < 0) return;
            var old = (DemoRow)_list.GetData(index);
            _list.UpdateData(index, new DemoRow(old.Id, old.Text + "\n【增量更新】这条数据已经被修改。", old.Revision + 1));
        }

        [ContextMenu("测试/清空数据（保留对象池）")]
        public void ClearRows()
        {
            if (CanRunDataTest()) _list.Clear();
        }

        private bool CanRunDataTest()
        {
            if (Application.isPlaying && _list != null && _list.IsInitialized)
                return true;
            Debug.LogWarning("请先运行列表 Demo。", this);
            return false;
        }

        private List<DemoRow> CreateDatas() => CreateRows(_dataCount);

        private List<DemoRow> CreateRows(int count)
        {
            var result = new List<DemoRow>(count);
            for (int i = 0; i < count; i++)
            {
                int id = _nextRowId++;
                string text = $"消息 ID {id}：这是一个 UGUIVirtualList 可变高度 Item 测试。";
                for (int j = 0; j < id % 5; j++)
                    text += " 这段文字会让 Item 高度随内容变化。";
                result.Add(new DemoRow(id, text));
            }
            return result;
        }

        private sealed class DemoRow
        {
            public readonly int Id;
            public readonly int Revision;
            public string Text;
            public DemoRow(int id, string text, int revision = 0) { Id = id; Text = text; Revision = revision; }
        }
    }
}
