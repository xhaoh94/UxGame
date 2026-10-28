using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ux.UI
{
    public sealed class UGUIVirtualGroupDemo : MonoBehaviour
    {
        [SerializeField] private UGUIVirtualListComponent _list;
        [Min(1)] [SerializeField] private int _groupCount = 20;
        [Min(0)] [SerializeField] private int _itemsPerGroup = 20;
        private readonly Dictionary<GameObject, Text> _texts = new();
        private UGUIVirtualGroupAdapter _adapter;
        private bool _subscribed;
        private int _headerRevision;
        private void Start()
        {
            if (_list == null) _list = GetComponent<UGUIVirtualListComponent>();
            if (_list == null || _list.ListType != UGUIVirtualListType.Grouped)
            {
                Debug.LogWarning("请使用 Create UGUI Grouped Demo 或选择分组列表。", this);
                return;
            }
            _list.ItemTypeProvider = GetItemType;
            _list.ItemRenderer = BindItem;
            _list.StickyHeaderRenderer = BindItem;
            _list.ItemClicked += OnItemClicked;
            _list.ItemRecycled += OnRecycled;
            _list.StickyHeaderRecycled += OnRecycled;
            _subscribed = true;
            _adapter = _list.GroupAdapter;
            _list.SetGroups(CreateGroups());
        }
        private int GetItemType(int index, object data) => ((UGUIVirtualGroupItem)data).IsHeader ? 0 : 1;
        private void BindItem(int index, object data, GameObject view)
        {
            var row = (UGUIVirtualGroupItem)data;
            if (!_texts.TryGetValue(view, out var text) || text == null)
            {
                text = view.GetComponentInChildren<Text>(true);
                if (text == null) throw new InvalidOperationException("分组模板需要 UGUI Text。");
                _texts[view] = text;
            }
            text.text = row.IsHeader ? row.Data?.ToString() : ((DemoData)row.Data).Text;
            text.color = row.IsSticky ? new Color(1f, 0.9f, 0.6f) : Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            text.verticalOverflow = _list.LayoutMode == UGUIVirtualListLayoutMode.VariableLinear
                ? VerticalWrapMode.Overflow : VerticalWrapMode.Truncate;
            view.name = row.IsSticky ? "Sticky " + row.GroupId : $"Group Item [{index}] {row.GroupId}";
            if (_list.LayoutMode == UGUIVirtualListLayoutMode.VariableLinear)
                ((RectTransform)view.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                    Mathf.Max(row.IsHeader ? 64f : 76f, Mathf.Ceil(text.preferredHeight) + 20f));
        }
        private void OnItemClicked(int index, object data)
        {
            var row = (UGUIVirtualGroupItem)data;
            Debug.Log($"[UGUIVirtualGroupDemo] 点击 {row.GroupId} / {row.ItemIndex}，组头={row.IsHeader}", this);
        }
        private void OnRecycled(int index, object data, GameObject view)
        {
            if (_texts.TryGetValue(view, out var text) && text != null) text.text = string.Empty;
        }
        [ContextMenu("测试/定位第二组")]
        public void ScrollToSecondGroup()
        {
            if (Ready()) _list.ScrollToGroup("group-1");
        }
        [ContextMenu("测试/更新当前吸顶组头")]
        public void UpdateStickyGroup()
        {
            if (!Ready()) return;
            string id = _list.CurrentStickyGroupId ?? "group-0";
            _list.UpdateGroupHeader(id, $"已更新组头 {id} / {++_headerRevision}");
        }
        [ContextMenu("测试/重新设置分组")]
        public void ReloadGroups()
        {
            if (Ready()) _list.SetGroups(CreateGroups());
        }
        private bool Ready() => Application.isPlaying && _list != null && _list.IsInitialized && _adapter != null && _adapter.OwnsCurrentData;
        private List<UGUIVirtualGroup> CreateGroups()
        {
            var result = new List<UGUIVirtualGroup>();
            for (int g = 0; g < _groupCount; g++)
            {
                var rows = new List<DemoData>();
                for (int i = 0; i < _itemsPerGroup; i++)
                {
                    string text = $"分组 {g:D2} 的数据 {i:D2}";
                    for (int n = 0; n < i % 4; n++) text += " 不同高度的 Item 也支持分组吸顶。";
                    rows.Add(new DemoData(text));
                }
                result.Add(UGUIVirtualGroup.Create("group-" + g, $"分组 {g:D2}", rows));
            }
            return result;
        }
        private void OnDestroy()
        {
            bool owns = _subscribed && _adapter != null && _adapter.OwnsCurrentData;
            if (_subscribed && _list != null)
            {
                _list.ItemClicked -= OnItemClicked;
                _list.ItemRecycled -= OnRecycled;
                _list.StickyHeaderRecycled -= OnRecycled;
                if (_list.ItemRenderer == BindItem) _list.ItemRenderer = null;
                if (_list.StickyHeaderRenderer == BindItem) _list.StickyHeaderRenderer = null;
                if (_list.ItemTypeProvider == GetItemType) _list.ItemTypeProvider = null;
            }
            _adapter = null;
            _texts.Clear();
            if (owns && _list != null) _list.Release();
        }
        private sealed class DemoData
        {
            public readonly string Text;
            public DemoData(string text) { Text = text; }
        }
    }
}
