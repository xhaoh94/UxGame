using System;
using UnityEngine;

namespace Ux
{
    public sealed partial class UGUIVirtualListComponent
    {
        [SerializeField] private bool _smoothScroll;
        [Min(0f)] [SerializeField] private float _smoothScrollDuration = 0.25f;
        private UGUIVirtualScrollNavigator _navigator;

        public bool IsScrolling => _navigator != null && _navigator.IsActive;

        /// <summary>
        /// 可选平滑导航。durationSeconds&lt;0 使用 Inspector 时长，0 立即定位并在后续帧修正；
        /// 导航会在用户拖拽、数据变化、重复请求或释放时取消。
        /// </summary>
        public void ScrollTo(int index, UGUIVirtualListAlignment alignment = UGUIVirtualListAlignment.Nearest,
            float padding = 0f, float durationSeconds = -1f)
        {
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
            Controller.ValidateDataMutation();
            if (index >= Count) throw new ArgumentOutOfRangeException(nameof(index));
            if (!_smoothScroll)
            {
                _navigator?.Cancel();
                Controller.ScrollToView(index, alignment);
                return;
            }
            if (!Controller.IsVertical)
            {
                Debug.LogWarning("平滑导航仅支持竖向列表，已直接定位。", this);
                Controller.ScrollToView(index, alignment);
                return;
            }
            if (_navigator == null) _navigator = new UGUIVirtualScrollNavigator(Controller);
            float duration = durationSeconds >= 0f ? durationSeconds : _smoothScrollDuration;
            _navigator.ScrollTo(index, alignment, padding, duration);
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
