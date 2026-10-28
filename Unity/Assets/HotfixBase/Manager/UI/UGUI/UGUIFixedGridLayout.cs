using System;
using UnityEngine;

namespace Ux
{
    /// <summary>
    /// 固定单元格的单轴虚拟网格。垂直方向按行排列，countPerLine 是列数；
    /// 水平方向按列排列，countPerLine 是行数。位置和可见行列范围均按公式计算。
    /// </summary>
    public sealed class UGUIFixedGridLayout : IUGUIVirtualListLayout
    {
        private readonly UGUIVirtualListAxis _axis;
        private readonly Vector2 _itemSize;
        private readonly Vector2 _spacing;
        private readonly int _countPerLine;
        private readonly float _paddingStart;
        private readonly float _paddingEnd;
        private int _count;

        public UGUIFixedGridLayout(
            UGUIVirtualListAxis axis,
            Vector2 itemSize,
            int countPerLine,
            Vector2 spacing = default,
            float paddingStart = 0f,
            float paddingEnd = 0f)
        {
            if (axis != UGUIVirtualListAxis.Vertical && axis != UGUIVirtualListAxis.Horizontal)
                throw new ArgumentOutOfRangeException(nameof(axis));
            if (!IsPositiveFinite(itemSize.x) || !IsPositiveFinite(itemSize.y))
                throw new ArgumentOutOfRangeException(nameof(itemSize), "网格 Item 尺寸必须为有限正数。");
            if (countPerLine < 1)
                throw new ArgumentOutOfRangeException(nameof(countPerLine), "网格每行/列至少有一项。");
            if (!IsNonNegativeFinite(spacing.x) || !IsNonNegativeFinite(spacing.y))
                throw new ArgumentOutOfRangeException(nameof(spacing), "网格间距必须为有限非负数。");
            if (!IsNonNegativeFinite(paddingStart) || !IsNonNegativeFinite(paddingEnd) ||
                (double)paddingStart + paddingEnd > float.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(paddingStart), "网格首尾留白必须为有限非负数。");

            _axis = axis;
            _itemSize = itemSize;
            _countPerLine = countPerLine;
            _spacing = spacing;
            _paddingStart = paddingStart;
            _paddingEnd = paddingEnd;
            if (MainStep > float.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(spacing), "单元格尺寸与间距之和超出布局范围。");
        }

        public bool IsVertical => _axis == UGUIVirtualListAxis.Vertical;
        public bool SupportsDynamicSize => false;
        public int CountPerLine => _countPerLine;

        private int LineCount => _count == 0 ? 0 : (_count - 1) / _countPerLine + 1;
        private float MainSize => IsVertical ? _itemSize.y : _itemSize.x;
        private float MainSpacing => IsVertical ? _spacing.y : _spacing.x;
        private float CrossSpacing => IsVertical ? _spacing.x : _spacing.y;
        private double MainStep => (double)MainSize + MainSpacing;

        public float ContentLength => (float)((double)_paddingStart + _paddingEnd +
            (LineCount == 0 ? 0d : LineCount * MainStep - MainSpacing));

        public float CrossSize
        {
            get
            {
                int occupied = Math.Min(_count, _countPerLine);
                if (occupied == 0)
                    return 0f;
                float crossSize = IsVertical ? _itemSize.x : _itemSize.y;
                return (float)((double)occupied * crossSize + (occupied - 1d) * CrossSpacing);
            }
        }

        public void SetItemCount(int count, bool resetSizes)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            int lines = count == 0 ? 0 : (count - 1) / _countPerLine + 1;
            int occupied = Math.Min(count, _countPerLine);
            double main = (double)_paddingStart + _paddingEnd +
                (lines == 0 ? 0d : lines * MainStep - MainSpacing);
            double crossSize = IsVertical ? _itemSize.x : _itemSize.y;
            double cross = occupied == 0 ? 0d : occupied * crossSize + (occupied - 1d) * CrossSpacing;
            if (main > float.MaxValue || cross > float.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(count), "网格总尺寸超出 RectTransform 可表示范围。");
            _count = count;
        }

        public UGUIVirtualListItemLayout GetItemLayout(int index)
        {
            if (index < 0 || index >= _count)
                throw new ArgumentOutOfRangeException(nameof(index));

            int line = index / _countPerLine;
            int crossIndex = index % _countPerLine;
            float main = (float)(_paddingStart + line * MainStep);
            double crossSize = IsVertical ? _itemSize.x : _itemSize.y;
            float cross = (float)(crossIndex * (crossSize + CrossSpacing));
            return new UGUIVirtualListItemLayout(
                IsVertical ? new Vector2(cross, -main) : new Vector2(main, -cross),
                _itemSize);
        }

        public UGUIVirtualListRange GetVisibleRange(float scrollPosition, float viewportLength, float bufferSize)
        {
            if (_count == 0 || viewportLength <= 0f)
                return new UGUIVirtualListRange(0, -1);

            double buffer = Math.Max(0d, bufferSize);
            double start = scrollPosition - buffer;
            double end = scrollPosition + (double)viewportLength + buffer;
            double lastEnd = _paddingStart + (LineCount - 1d) * MainStep + MainSize;
            // 首尾留白或行间空隙没有实际 Item，允许返回空范围。
            if (end <= _paddingStart || start >= lastEnd)
                return new UGUIVirtualListRange(0, -1);

            int firstLine = ClampLine(Math.Floor((start - _paddingStart) / MainStep));
            if (_paddingStart + (double)firstLine * MainStep + MainSize <= start)
                firstLine++;
            int lastLine = ClampLine(Math.Ceiling((end - _paddingStart) / MainStep) - 1d);
            if (firstLine > lastLine)
                return new UGUIVirtualListRange(0, -1);

            // 用 long 计算末行边界，防止非完整末行或大数据量下乘法溢出。
            int firstIndex = (int)((long)firstLine * _countPerLine);
            int lastIndex = (int)Math.Min(_count - 1L, ((long)lastLine + 1L) * _countPerLine - 1L);
            return new UGUIVirtualListRange(firstIndex, lastIndex);
        }

        public bool SetItemSize(int index, Vector2 size) => false;

        private int ClampLine(double line)
        {
            if (line <= 0d)
                return 0;
            if (line >= LineCount - 1d)
                return LineCount - 1;
            return (int)line;
        }

        private static bool IsPositiveFinite(float value) => value > 0f && !float.IsInfinity(value);
        private static bool IsNonNegativeFinite(float value) => value >= 0f && !float.IsInfinity(value);
    }
}
