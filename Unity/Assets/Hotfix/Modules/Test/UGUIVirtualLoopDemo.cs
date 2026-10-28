using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ux.UI
{
    /// <summary>无限循环示例：固定高度卡片，滚动到首尾时静默换周期；可选按页吸附。</summary>
    public sealed class UGUIVirtualLoopDemo : MonoBehaviour
    {
        [SerializeField] private UGUIVirtualListComponent _list;
        [SerializeField] private bool _snapPerPage = true;
        [Min(1)] [SerializeField] private int _itemCount = 8;

        private static readonly Color[] Palette =
        {
            new Color(0.20f, 0.42f, 0.72f, 0.98f),
            new Color(0.24f, 0.55f, 0.42f, 0.98f),
            new Color(0.62f, 0.38f, 0.22f, 0.98f),
            new Color(0.46f, 0.30f, 0.62f, 0.98f),
            new Color(0.66f, 0.26f, 0.36f, 0.98f),
            new Color(0.22f, 0.52f, 0.62f, 0.98f),
            new Color(0.55f, 0.48f, 0.20f, 0.98f),
            new Color(0.30f, 0.36f, 0.52f, 0.98f)
        };

        private readonly Dictionary<GameObject, Image> _backgrounds = new();
        private readonly Dictionary<GameObject, Text> _texts = new();
        private bool _subscribed;
        private Func<int, object, int> _typeProvider;

        private void Start()
        {
            if (_list == null) _list = GetComponent<UGUIVirtualListComponent>();
            if (_list == null || _list.ListType != UGUIVirtualListType.Flat)
            {
                Debug.LogWarning("请使用 Create UGUI Loop Demo 菜单创建循环示例。", this);
                return;
            }
            _list.ItemRenderer = BindItem;
            _typeProvider = (index, data) => 0;
            _list.ItemTypeProvider = _typeProvider;
            _list.ItemClicked += OnClicked;
            _list.ItemRecycled += OnRecycled;
            _subscribed = true;
            _list.SetDatas(CreateRows());
            if (_snapPerPage)
            {
                float page = _list.ResolvedItemSize.y + 8f;
                if (page > 0f) _list.EnableCarousel(page, 0.18f);
            }
            Debug.Log($"[UGUIVirtualLoopDemo] 循环已启用：{_itemCount} 条数据 × 3 周期。上下拖动可无限滚动，点击卡片打印真实索引。", this);
        }

        private void BindItem(int index, object data, GameObject view)
        {
            if (!_texts.TryGetValue(view, out var text) || text == null)
            {
                text = view.GetComponentInChildren<Text>(true);
                _backgrounds[view] = view.GetComponent<Image>();
                _texts[view] = text;
            }
            var row = (Row)data;
            var image = _backgrounds[view];
            if (image != null) image.color = Palette[row.Index % Palette.Length];
            if (text != null)
            {
                text.text = $"{row.Index + 1} / {_itemCount}\n真实索引 {row.Index}（虚拟行循环复用）";
                text.alignment = TextAnchor.MiddleCenter;
                text.color = Color.white;
            }
            view.name = $"Loop Card {row.Index}";
        }

        private void OnClicked(int index, object data)
        {
            Debug.Log($"[UGUIVirtualLoopDemo] 点击真实索引 {index}（第 {index + 1} 张）", this);
        }

        private void OnRecycled(int index, object data, GameObject view)
        {
            if (_texts.TryGetValue(view, out var text) && text != null) text.text = string.Empty;
        }

        [ContextMenu("测试/下一页")]
        public void NextPage()
        {
            if (Ready()) _list.SnapToNextPage();
        }

        [ContextMenu("测试/上一页")]
        public void PreviousPage()
        {
            if (Ready()) _list.SnapToPreviousPage();
        }

        [ContextMenu("测试/回到第一张")]
        public void BackToFirst()
        {
            if (Ready()) _list.ScrollTo(0, UGUIVirtualListAlignment.Start);
        }

        private bool Ready()
        {
            if (Application.isPlaying && _list != null && _list.IsInitialized) return true;
            Debug.LogWarning("请先运行循环 Demo。", this);
            return false;
        }

        private List<Row> CreateRows()
        {
            var rows = new List<Row>(_itemCount);
            for (int i = 0; i < _itemCount; i++) rows.Add(new Row(i));
            return rows;
        }

        private sealed class Row
        {
            public readonly int Index;
            public Row(int index) { Index = index; }
        }

        private void OnDestroy()
        {
            if (_subscribed && _list != null)
            {
                _list.ItemClicked -= OnClicked;
                _list.ItemRecycled -= OnRecycled;
                if (_list.ItemRenderer == BindItem) _list.ItemRenderer = null;
                if (_list.ItemTypeProvider == _typeProvider) _list.ItemTypeProvider = null;
                _typeProvider = null;
                _list.DisableCarousel();
                _list.Release();
            }
            _texts.Clear();
            _backgrounds.Clear();
        }
    }
}
