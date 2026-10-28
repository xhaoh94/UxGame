using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    /// <summary>可选能力：由控制器传入视口尺寸，布局可据此改变每行/列的容量。</summary>
    public interface IUGUIVirtualListViewportLayout
    {
        bool SetViewportSize(Vector2 viewportSize);
    }

    /// <summary>
    /// 统一的单轴流式布局。一列/行就是线性列表，多列/行按视口交叉轴空间自动换行。
    /// 固定尺寸走公式；可变尺寸缓存主轴尺寸，每行取最大高度或每列取最大宽度。
    /// </summary>
    public sealed class UGUIFlowLayout : IUGUIVirtualListLayout, IUGUIVirtualListViewportLayout, IUGUIVirtualListMutableLayout
    {
        private readonly UGUIVirtualListAxis _axis;
        private readonly Vector2 _itemSize;
        private readonly bool _variableSize;
        private readonly bool _allowMultiple;
        private readonly int _maxCountPerLine;
        private readonly Vector2 _spacing;
        private readonly float _paddingStart;
        private readonly float _paddingEnd;
        private readonly List<float> _sizes;
        private readonly List<float> _lineSizes;
        private readonly List<float> _lineStarts;
        private UGUIFixedGridLayout _fixedLayout;
        private int _count;
        private int _countPerLine = 1;
        private bool _positionsDirty = true;
        private float _contentLength;

        public UGUIFlowLayout(UGUIVirtualListAxis axis, Vector2 itemSize,
            bool variableSize = false, bool allowMultiple = false, int maxCountPerLine = 0,
            Vector2 spacing = default, float paddingStart = 0f, float paddingEnd = 0f)
        {
            if (maxCountPerLine < 0)
                throw new ArgumentOutOfRangeException(nameof(maxCountPerLine));
            // 复用固定单元格的参数校验；单行同样走该快速路径，而不是另建一套算法。
            _fixedLayout = new UGUIFixedGridLayout(axis, itemSize, 1, spacing, paddingStart, paddingEnd);
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

        public bool IsVertical => _axis == UGUIVirtualListAxis.Vertical;
        public bool SupportsDynamicSize => _variableSize;
        public int CountPerLine => _countPerLine;
        public float CrossSize => _fixedLayout.CrossSize;
        private float MainSpacing => IsVertical ? _spacing.y : _spacing.x;
        private float DefaultMainSize => IsVertical ? _itemSize.y : _itemSize.x;

        public float ContentLength
        {
            get
            {
                if (!_variableSize)
                    return _fixedLayout.ContentLength;
                EnsurePositions();
                return _contentLength;
            }
        }

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
            var layout = new UGUIFixedGridLayout(_axis, _itemSize, count, _spacing, _paddingStart, _paddingEnd);
            layout.SetItemCount(_count, false);
            _fixedLayout = layout;
            _countPerLine = count;
            _positionsDirty = true;
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
            _positionsDirty = true;
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
            _positionsDirty = true;
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
            _positionsDirty = true;
        }

        public void InvalidateItemSize(int index)
        {
            if (index < 0 || index >= _count)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (_variableSize)
            {
                _sizes[index] = DefaultMainSize;
                _positionsDirty = true;
            }
        }

        public UGUIVirtualListItemLayout GetItemLayout(int index)
        {
            if (!_variableSize)
                return _fixedLayout.GetItemLayout(index);
            if (index < 0 || index >= _count)
                throw new ArgumentOutOfRangeException(nameof(index));
            EnsurePositions();
            int line = index / _countPerLine;
            int crossIndex = index % _countPerLine;
            double crossSize = IsVertical ? _itemSize.x : _itemSize.y;
            double crossSpacing = IsVertical ? _spacing.x : _spacing.y;
            float cross = (float)(crossIndex * (crossSize + crossSpacing));
            return new UGUIVirtualListItemLayout(
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
            _positionsDirty = true;
            return true;
        }

        public UGUIVirtualListRange GetVisibleRange(float scrollPosition, float viewportLength, float bufferSize)
        {
            if (!_variableSize)
                return _fixedLayout.GetVisibleRange(scrollPosition, viewportLength, bufferSize);
            EnsurePositions();
            if (_count == 0 || viewportLength <= 0f)
                return new UGUIVirtualListRange(0, -1);
            float start = scrollPosition - Mathf.Max(0f, bufferSize);
            float end = scrollPosition + viewportLength + Mathf.Max(0f, bufferSize);
            int first = FindFirstLineEndingAfter(start);
            int last = FindFirstLineStartingAtOrAfter(end) - 1;
            if (first >= _lineSizes.Count || first > last)
                return new UGUIVirtualListRange(0, -1);
            return new UGUIVirtualListRange(first * _countPerLine,
                (int)Math.Min(_count - 1L, ((long)last + 1) * _countPerLine - 1));
        }

        private void EnsurePositions()
        {
            if (!_positionsDirty)
                return;
            _lineSizes.Clear();
            _lineStarts.Clear();
            double start = _paddingStart;
            for (int first = 0; first < _count;)
            {
                int end = (int)Math.Min(_count, (long)first + _countPerLine);
                float main = 0f;
                for (int i = first; i < end; i++)
                    main = Mathf.Max(main, _sizes[i]);
                if (start + main + _paddingEnd > float.MaxValue)
                    throw new InvalidOperationException("流式布局的内容总尺寸超出 RectTransform 范围。");
                _lineStarts.Add((float)start);
                _lineSizes.Add(main);
                start += main + (double)MainSpacing;
                first = end;
            }
            _contentLength = (float)(_count == 0 ? (double)_paddingStart + _paddingEnd
                : start - MainSpacing + _paddingEnd);
            _positionsDirty = false;
        }

        private int FindFirstLineEndingAfter(float position)
        {
            int low = 0, high = _lineSizes.Count;
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
