using System;
using UnityEngine;

namespace Ux
{
    public sealed partial class UIVirtualListComponent
    {
        private UIVirtualScrollNavigator _navigator;

        public bool IsScrolling => _navigator != null && _navigator.IsActive;

        /// <summary>
        /// 滚动定位。durationSeconds&lt;=0 立即定位并在后续帧修正；&gt;0 用无缩放时间平滑滚动。
        /// 导航会在用户拖拽、数据变化、重复请求或释放时取消。
        /// </summary>
        public void ScrollTo(int index, UIVirtualListAlignment alignment = UIVirtualListAlignment.Nearest,
            float padding = 0f, float durationSeconds = 0f)
        {
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
            Controller.ValidateDataMutation();
            if (index >= Count) throw new ArgumentOutOfRangeException(nameof(index));
            if (durationSeconds <= 0f)
            {
                _navigator?.Cancel();
                Controller.ScrollToView(index, alignment);
                return;
            }
            if (!Controller.IsVertical)
            {
                Debug.LogWarning("平滑滚动仅支持竖向列表，已直接定位。", this);
                Controller.ScrollToView(index, alignment);
                return;
            }
            if (_navigator == null) _navigator = new UIVirtualScrollNavigator(Controller);
            _navigator.ScrollTo(index, alignment, padding, durationSeconds);
        }

        public void StopScroll()
        {
            _navigator?.Cancel();
            _list?.StopScrollMotion();
        }

        private void TickNavigation()
        {
            if (_navigator != null && _navigator.IsActive) _navigator.Tick();
        }

        private void DisposeOwnedNavigation()
        {
            var navigator = _navigator;
            _navigator = null;
            navigator?.Dispose();
        }
    }
}
