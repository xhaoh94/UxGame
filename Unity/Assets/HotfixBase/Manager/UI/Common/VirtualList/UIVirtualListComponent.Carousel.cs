using System;
using UnityEngine;

namespace Ux
{
    public sealed partial class UIVirtualListComponent
    {
        [Tooltip("惯性结束后的对齐策略；页长 = 子项高 + 垂直间距。仅竖向单列。")]
        [SerializeField] private UIVirtualCarouselSnapMode _carouselSnapMode;
        [Min(0f)] [SerializeField] private float _carouselSnapDuration = 0.2f;
        private UIVirtualCarousel _carousel;

        /// <summary>运行时切换对齐策略；None 等价于关闭吸附。</summary>
        public UIVirtualCarouselSnapMode CarouselSnapMode
        {
            get => _carouselSnapMode;
            set
            {
                if (_carouselSnapMode == value) return;
                _carouselSnapMode = value;
                if (_list == null) return;
                ApplyConfiguredCarousel();
            }
        }

        /// <summary>按 Inspector 配置应用吸附：对齐按实际项边界，仅竖向单列。</summary>
        private void ApplyConfiguredCarousel()
        {
            if (_carouselSnapMode == UIVirtualCarouselSnapMode.None)
            {
                DisableCarousel();
                return;
            }
            if (_list == null || _axis != UIVirtualListAxis.Vertical || AllowMultiple) return;
            if (_listType != UIVirtualListType.Flat && _listType != UIVirtualListType.Paged) return;
            EnableCarousel(_carouselSnapDuration, _carouselSnapMode);
        }

        public bool IsCarouselEnabled => _carousel != null;
        public bool IsSnapping => _carousel != null && _carousel.IsSnapping;
        public int CarouselPage => _carousel?.CurrentPage ?? 0;

        /// <summary>启用轮播吸附；拖拽惯性结束后按 SnapMode 对齐到项边界。</summary>
        public void EnableCarousel(float snapDuration = -1f,
            UIVirtualCarouselSnapMode snapMode = UIVirtualCarouselSnapMode.Nearest)
        {
            if (_listType == UIVirtualListType.Tree || _listType == UIVirtualListType.Grouped)
                throw new InvalidOperationException("轮播吸附只适用于普通/分页列表的竖向单列。");
            Initialize();
            if (!Controller.IsVertical)
                throw new InvalidOperationException("轮播吸附仅支持竖向列表。");
            DisableCarousel();
            _carousel = new UIVirtualCarousel(Controller,
                snapDuration >= 0f ? snapDuration : _carouselSnapDuration)
            {
                SnapMode = snapMode
            };
        }

        public void DisableCarousel()
        {
            var carousel = _carousel;
            _carousel = null;
            carousel?.Dispose();
        }

        public bool SnapToPage(int page) => _carousel != null && _carousel.SnapTo(page);
        public bool SnapToNextPage() => _carousel != null && _carousel.SnapToNext();
        public bool SnapToPreviousPage() => _carousel != null && _carousel.SnapToPrevious();

        private void TickCarousel()
        {
            if (_carousel != null) _carousel.Tick();
        }

        private void DisposeOwnedCarousel()
        {
            var carousel = _carousel;
            _carousel = null;
            carousel?.Dispose();
        }
    }
}
