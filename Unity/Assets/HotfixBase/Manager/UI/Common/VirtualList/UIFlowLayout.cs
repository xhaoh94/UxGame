using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    /// <summary>可选能力：由控制器传入视口尺寸，布局可据此改变每行/列的容量。</summary>
    public interface IUIVirtualListViewportLayout
    {
        bool SetViewportSize(Vector2 viewportSize);
    }

    /// <summary>
    /// 统一的单轴流式布局。一列/行就是线性列表，多列/行按视口交叉轴空间自动换行。
    /// 固定尺寸走公式；可变尺寸缓存主轴尺寸，每行取最大高度或每列取最大宽度。
    /// </summary>
    public sealed class UIFlowLayout : IUIVirtualListLayout, IUIVirtualListViewportLayout, IUIVirtualListMutableLayout
    {
        private readonly UIVirtualListAxis _axis;
        private readonly Vector2 _itemSize;
        private readonly bool _variableSize;
        private readonly bool _allowMultiple;
        private readonly int _maxCountPerLine;
        private readonly Vector2 _spacing;
        private readonly float _paddingStart;
        private readonly float _paddingEnd;
        private readonly List<float> _sizes;
        // 行高缓存：数据/换行变化时全量重建，单项尺寸变化时只更新所在行。
        private readonly List<float> _lineSizes;
        // 行起点缓存：只按需推进到可见位置。
        private readonly List<float> _lineStarts;
        private UIFixedGridLayout _fixedLayout;
        private int _count;
        private int _countPerLine = 1;
        private double _totalLineSize;

        public UIFlowLayout(UIVirtualListAxis axis, Vector2 itemSize,
            bool variableSize = false, bool allowMultiple = false, int maxCountPerLine = 0,
            Vector2 spacing = default, float paddingStart = 0f, float paddingEnd = 0f)
        {
            if (maxCountPerLine < 0)
                throw new ArgumentOutOfRangeException(nameof(maxCountPerLine));
            // 复用固定单元格的参数校验；单行同样走该快速路径，而不是另建一套算法。
            _fixedLayout = new UIFixedGridLayout(axis, itemSize, 1, spacing, paddingStart, paddingEnd);
            _axis = axis;
            _itemSize = itemSize;
            _variableSize = variableSize;
            _allowMultiple = allowMultiple;
            _maxCountPerLine = maxCountPerLine;
            _spacing = spacing;
            _paddingStart = paddingStart;
            _paddingEnd = paddingEnd;
            if (variableSize)
            {
                _sizes = new List<float>();
                _lineSizes = new List<float>();
                _lineStarts = new List<float>();
            }
        }

        public bool IsVertical => _axis == UIVirtualListAxis.Vertical;
        public bool SupportsDynamicSize => _variableSize;
        public int CountPerLine => _countPerLine;
        public float CrossSize => _fixedLayout.CrossSize;
        /// <summary>主轴间距。循环布局拼接周期时要把这段补进周期间隙，否则首尾两项会贴在一起。</summary>
        internal float MainSpacing => IsVertical ? _spacing.y : _spacing.x;
        private float DefaultMainSize => IsVertical ? _itemSize.y : _itemSize.x;

        public float ContentLength
        {
            get
            {
                if (!_variableSize)
                    return _fixedLayout.ContentLength;
                int lines = LineCount;
                if (lines <= 0) return _paddingStart + _paddingEnd;
                // 增量总长：O(1)，不再依赖任何全量遍历。
                return _paddingStart + _paddingEnd + (float)_totalLineSize + MainSpacing * (lines - 1);
            }
        }

        private int LineCount => _count <= 0 ? 0 : (_count + _countPerLine - 1) / _countPerLine;

        /// <summary>最大数量为 0 时不设上限；空间不足一项时仍保留一项，不缩放 Item。</summary>
        public static int CalculateCountPerLine(float available, float itemCrossSize, float crossSpacing,
            bool allowMultiple, int maxCountPerLine = 0)
        {
            if (itemCrossSize <= 0f || float.IsNaN(itemCrossSize) || float.IsInfinity(itemCrossSize))
                throw new ArgumentOutOfRangeException(nameof(itemCrossSize));
            if (crossSpacing < 0f || float.IsNaN(crossSpacing) || float.IsInfinity(crossSpacing))
                throw new ArgumentOutOfRangeException(nameof(crossSpacing));
            if (maxCountPerLine < 0)
                throw new ArgumentOutOfRangeException(nameof(maxCountPerLine));
            if (!allowMultiple || available <= 0f || float.IsNaN(available) || float.IsInfinity(available))
                return 1;
            double count = Math.Floor(((double)available + crossSpacing) / ((double)itemCrossSize + crossSpacing));
            int result = count >= int.MaxValue ? int.MaxValue : Math.Max(1, (int)count);
            return maxCountPerLine > 0 ? Math.Min(result, maxCountPerLine) : result;
        }

        public bool SetViewportSize(Vector2 viewportSize)
        {
            int count = CalculateCountPerLine(IsVertical ? viewportSize.x : viewportSize.y,
                IsVertical ? _itemSize.x : _itemSize.y,
                IsVertical ? _spacing.x : _spacing.y, _allowMultiple, _maxCountPerLine);
            if (count == _countPerLine)
                return false;
            var layout = new UIFixedGridLayout(_axis, _itemSize, count, _spacing, _paddingStart, _paddingEnd);
            layout.SetItemCount(_count, false);
            _fixedLayout = layout;
            _countPerLine = count;
            RebuildLineMetrics();
            return true;
        }

        public void SetItemCount(int count, bool resetSizes)
        {
            _fixedLayout.SetItemCount(count, resetSizes);
            if (_variableSize)
            {
                if (resetSizes)
                    _sizes.Clear();
                if (_sizes.Count > count)
                    _sizes.RemoveRange(count, _sizes.Count - count);
                while (_sizes.Count < count)
                    _sizes.Add(DefaultMainSize);
            }
            _count = count;
            RebuildLineMetrics();
        }

        public void InsertItems(int index, int count)
        {
            if (index < 0 || index > _count)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (count < 0 || count > int.MaxValue - _count)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (count == 0)
                return;
            _fixedLayout.SetItemCount(_count + count, false);
            if (_variableSize)
            {
                var defaults = new float[count];
                for (int i = 0; i < count; i++) defaults[i] = DefaultMainSize;
                _sizes.InsertRange(index, defaults);
            }
            _count += count;
            RebuildLineMetrics();
        }

        public void RemoveItems(int index, int count)
        {
            if (index < 0 || index > _count)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (count < 0 || count > _count - index)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (count == 0)
                return;
            _fixedLayout.SetItemCount(_count - count, false);
            if (_variableSize)
                _sizes.RemoveRange(index, count);
            _count -= count;
            RebuildLineMetrics();
        }

        public void InvalidateItemSize(int index)
        {
            if (index < 0 || index >= _count)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (_variableSize)
            {
                _sizes[index] = DefaultMainSize;
                UpdateLineForSizeChange(index);
            }
        }

        public UIVirtualListItemLayout GetItemLayout(int index)
        {
            if (!_variableSize)
                return _fixedLayout.GetItemLayout(index);
            if (index < 0 || index >= _count)
                throw new ArgumentOutOfRangeException(nameof(index));
            int line = index / _countPerLine;
            EnsureLineStarts(line + 1);
            int crossIndex = index % _countPerLine;
            double crossSize = IsVertical ? _itemSize.x : _itemSize.y;
            double crossSpacing = IsVertical ? _spacing.x : _spacing.y;
            float cross = (float)(crossIndex * (crossSize + crossSpacing));
            return new UIVirtualListItemLayout(
                IsVertical ? new Vector2(cross, -_lineStarts[line]) : new Vector2(_lineStarts[line], -cross),
                IsVertical ? new Vector2(_itemSize.x, _sizes[index]) : new Vector2(_sizes[index], _itemSize.y));
        }

        public bool SetItemSize(int index, Vector2 size)
        {
            if (!_variableSize || index < 0 || index >= _count)
                return false;
            float main = IsVertical ? size.y : size.x;
            if (main <= 0f || float.IsNaN(main) || float.IsInfinity(main))
                throw new ArgumentOutOfRangeException(nameof(size));
            if (Mathf.Abs(_sizes[index] - main) < 0.01f)
                return false;
            _sizes[index] = main;
            UpdateLineForSizeChange(index);
            return true;
        }

        public UIVirtualListRange GetVisibleRange(float scrollPosition, float viewportLength, float bufferSize)
        {
            if (!_variableSize)
                return _fixedLayout.GetVisibleRange(scrollPosition, viewportLength, bufferSize);
            if (_count == 0 || viewportLength <= 0f)
                return new UIVirtualListRange(0, -1);
            float start = scrollPosition - Mathf.Max(0f, bufferSize);
            float end = scrollPosition + viewportLength + Mathf.Max(0f, bufferSize);
            // 二分需要行起点覆盖 end，否则范围不完整。
            EnsureLineStartsToCover(end);
            int first = FindFirstLineEndingAfter(start);
            int last = FindFirstLineStartingAtOrAfter(end) - 1;
            if (first >= _lineSizes.Count || first > last)
                return new UIVirtualListRange(0, -1);
            return new UIVirtualListRange(first * _countPerLine,
                (int)Math.Min(_count - 1L, ((long)last + 1) * _countPerLine - 1));
        }

        /// <summary>数据/换行变化后重建行高与总长；行起点按需再推进。</summary>
        private void RebuildLineMetrics()
        {
            if (!_variableSize)
            {
                _totalLineSize = 0;
                return;
            }
            _lineSizes.Clear();
            _lineStarts.Clear();
            int lines = LineCount;
            _lineSizes.Capacity = Mathf.Max(_lineSizes.Capacity, lines);
            double total = 0;
            for (int first = 0; first < _count; first += _countPerLine)
            {
                int end = (int)Math.Min(_count, (long)first + _countPerLine);
                float height = 0f;
                for (int i = first; i < end; i++)
                    height = Mathf.Max(height, _sizes[i]);
                _lineSizes.Add(height);
                total += height;
            }
            _totalLineSize = total;
        }

        /// <summary>单项尺寸变化后，只重算它所在行的行高和总长。</summary>
        private void UpdateLineForSizeChange(int index)
        {
            int line = index / _countPerLine;
            if (line >= _lineSizes.Count)
            {
                RebuildLineMetrics();
                return;
            }
            int first = line * _countPerLine;
            int end = Mathf.Min(_count, first + _countPerLine);
            float height = 0f;
            for (int i = first; i < end; i++)
                height = Mathf.Max(height, _sizes[i]);
            _totalLineSize += height - _lineSizes[line];
            _lineSizes[line] = height;
            // 本行起点仍然有效（只依赖前面的行），之后的行起点要重算。
            TrimLineStarts(line + 1);
        }

        private void EnsureLineStarts(int count)
        {
            int lines = LineCount;
            if (count > lines) count = lines;
            if (_lineStarts.Count >= count) return;

            _lineStarts.Capacity = Mathf.Max(_lineStarts.Capacity, lines);
            int line = _lineStarts.Count;
            float start = line == 0
                ? _paddingStart
                : _lineStarts[line - 1] + _lineSizes[line - 1] + MainSpacing;
            while (line < count)
            {
                _lineStarts.Add(start);
                start += _lineSizes[line] + MainSpacing;
                line++;
            }
        }

        /// <summary>丢弃 count 之后的行起点；它们依赖已变化的行高。</summary>
        private void TrimLineStarts(int count)
        {
            if (_lineStarts.Count <= count) return;
            _lineStarts.RemoveRange(count, _lineStarts.Count - count);
        }

        /// <summary>从当前水位往后推进，直到 position 落在已计算区间内。</summary>
        private void EnsureLineStartsToCover(float position)
        {
            while (_lineStarts.Count < LineCount)
            {
                int last = _lineStarts.Count - 1;
                if (last >= 0 && _lineStarts[last] + _lineSizes[last] + MainSpacing > position)
                    return;
                EnsureLineStarts(_lineStarts.Count + 1);
            }
        }

        private int FindFirstLineEndingAfter(float position)
        {
            // 只能二分已推进的起点区间（调用方保证它覆盖了 position）。
            int low = 0, high = _lineStarts.Count;
            while (low < high)
            {
                int middle = low + (high - low) / 2;
                if (_lineStarts[middle] + _lineSizes[middle] > position) high = middle;
                else low = middle + 1;
            }
            return low;
        }

        private int FindFirstLineStartingAtOrAfter(float position)
        {
            int low = 0, high = _lineStarts.Count;
            while (low < high)
            {
                int middle = low + (high - low) / 2;
                if (_lineStarts[middle] >= position) high = middle;
                else low = middle + 1;
            }
            return low;
        }
    }
}
