using System;
using UnityEngine;

namespace Ux
{
    public sealed partial class UGUIVirtualListComponent
    {
        [Min(0f)] [SerializeField] private float _carouselSnapDuration = 0.2f;
        private UGUIVirtualCarousel _carousel;

        public bool IsCarouselEnabled => _carousel != null;
        public bool IsSnapping => _carousel != null && _carousel.IsSnapping;
        public int CarouselPage => _carousel?.CurrentPage ?? 0;

        /// <summary>启用轮播吸附；pageLength 通常是视口长度或单页 Item 长度。</summary>
        public void EnableCarousel(float pageLength, float snapDuration = -1f)
        {
            if (_listType == UGUIVirtualListType.Tree || _listType == UGUIVirtualListType.Grouped)
                throw new InvalidOperationException("轮播吸附只适用于普通/分页列表的竖向单列。");
            Initialize();
            if (!Controller.IsVertical)
                throw new InvalidOperationException("轮播吸附仅支持竖向列表。");
            DisableCarousel();
            _carousel = new UGUIVirtualCarousel(Controller, pageLength,
                snapDuration >= 0f ? snapDuration : _carouselSnapDuration);
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
