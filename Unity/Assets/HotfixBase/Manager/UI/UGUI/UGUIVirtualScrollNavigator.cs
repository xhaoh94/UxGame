using System;
using UnityEngine;

namespace Ux
{
    public readonly struct UGUIVirtualScrollTarget
    {
        public readonly int Index;
        public readonly UGUIVirtualListAlignment Alignment;
        public readonly float Padding;
        public bool IsValid => Index >= 0;
        public UGUIVirtualScrollTarget(int index, UGUIVirtualListAlignment alignment, float padding = 0f)
        {
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
            if (float.IsNaN(padding) || float.IsInfinity(padding))
                throw new ArgumentOutOfRangeException(nameof(padding));
            Index = index;
            Alignment = alignment;
            Padding = padding;
        }
    }

    /// <summary>
    /// 滚动导航任务。目标位置随测量结果重新计算；用户拖拽、数据变更、新导航或释放会取消。
    /// 不拥有 Core，也不注册 Unity 生命周期，宿主需在更新中调用 Tick。
    /// </summary>
    public sealed class UGUIVirtualScrollNavigator : IDisposable
    {
        private const int MaxSettleTicks = 10;
        private readonly UGUIVirtualList _list;
        private readonly bool _vertical;
        private UGUIVirtualScrollTarget _target;
        private float _start;
        private float _elapsed;
        private float _duration;
        private AnimationCurve _curve;
        private ulong _revision;
        private bool _active;
        private bool _disposed;
        private int _settleTicks;
        private int _stableTicks;

        public UGUIVirtualScrollNavigator(UGUIVirtualList list)
        {
            _list = list ?? throw new ArgumentNullException(nameof(list));
            _vertical = list.IsVertical;
            if (!_vertical)
                throw new NotSupportedException("本版平滑导航仅支持竖向列表；横向请直接使用 ScrollToView。");
            _list.ScrollStarted += OnUserScrollStarted;
            _list.DataChanged += OnDataChanged;
        }

        public bool IsActive => _active && !_disposed;
        public UGUIVirtualScrollTarget Target => _target;
        public bool IsSettling => IsActive && _duration <= 0f;
        public event Action Completed;
        public event Action Cancelled;

        /// <summary>durationSeconds=0 会立即定位，并在后续几帧用测量结果校正；&gt;0 使用无缩放时间平滑滚动。</summary>
        public bool ScrollTo(int index, UGUIVirtualListAlignment alignment = UGUIVirtualListAlignment.Nearest,
            float padding = 0f, float durationSeconds = 0f, AnimationCurve curve = null)
        {
            if (_disposed) return false;
            var target = new UGUIVirtualScrollTarget(index, alignment, padding);
            if (!_list.CanMutateData) return false;
            if (index < 0 || index >= _list.Count) throw new ArgumentOutOfRangeException(nameof(index));
            if (durationSeconds < 0f || float.IsNaN(durationSeconds) || float.IsInfinity(durationSeconds))
                throw new ArgumentOutOfRangeException(nameof(durationSeconds));
            if (curve != null && !IsCurveSafe(curve)) throw new ArgumentException("滚动曲线必须返回有限值。", nameof(curve));
            if (_active) Cancel(); // 新请求视为取消上一个任务，监听方可停止旧动画。

            _target = target;
            _start = _list.ScrollPositionValue;
            _elapsed = 0f;
            _duration = durationSeconds;
            _curve = curve;
            _revision = _list.DataRevision;
            _settleTicks = 0;
            _stableTicks = 0;
            _active = true;
            // 先让目标进入缓冲区并完成测量，再取几何。
            _list.RefreshForNavigation(index, alignment);
            if (!_active || _disposed || !_list.CanMutateData) return _active;
            if (_duration <= 0f)
            {
                ApplyTarget();
                return true;
            }
            return true;
        }

        public void Cancel(bool notify = true)
        {
            if (!_active) return;
            _active = false;
            if (notify) Cancelled?.Invoke();
        }

        /// <summary>宿主每帧调用；deltaSeconds 仅供测试/自定义时间源，默认使用 unscaledDeltaTime。</summary>
        public bool Tick(float? deltaSeconds = null)
        {
            if (_disposed || !_active) return false;
            if (_list.IsDisposedOrDisposing) { Cancel(false); return false; }
            if (_list.FirstVisibleIndex < 0 && _list.Count == 0) { Cancel(); return false; }
            if (_revision != _list.DataRevision) { Cancel(); return false; }
            if (_target.Index >= _list.Count) { Cancel(); return false; }

            if (_duration > 0f)
            {
                float delta = deltaSeconds ?? Time.unscaledDeltaTime;
                if (delta < 0f || float.IsNaN(delta) || float.IsInfinity(delta)) delta = 0f;
                _elapsed += delta;
                float normalized = _duration <= 0f ? 1f : Mathf.Clamp01(_elapsed / _duration);
                float evaluated = _curve == null ? normalized : _curve.Evaluate(normalized);
                if (float.IsNaN(evaluated) || float.IsInfinity(evaluated)) evaluated = normalized;
                float end = ResolveTargetPosition();
                _list.SetScrollPositionInternal(Mathf.LerpUnclamped(_start, end, evaluated));
                if (!_active) return false;
                if (normalized >= 1f) Finish();
                return _active;
            }

            ApplyTarget();
            if (!_active) return false;
            float desired = ResolveTargetPosition();
            float current = _list.ScrollPositionValue;
            if (Mathf.Abs(desired - current) <= 0.5f)
                _stableTicks++;
            else
            {
                _stableTicks = 0;
                _list.SetScrollPositionInternal(desired);
            }
            if (_stableTicks >= 2) { Finish(); return false; }
            if (++_settleTicks > MaxSettleTicks) { Finish(); return false; }
            return true;
        }

        private void ApplyTarget() => _list.SetScrollPositionInternal(ResolveTargetPosition());

        private void Finish()
        {
            _active = false;
            _stableTicks = 0;
            Completed?.Invoke();
        }

        private float ResolveTargetPosition()
        {
            var layout = _list.GetItemLayout(_target.Index);
            float viewport = _list.ViewportSize.y;
            float start = -layout.Position.y;
            float length = layout.Size.y;
            float position;
            switch (_target.Alignment)
            {
                case UGUIVirtualListAlignment.Start:
                    position = start - _target.Padding;
                    break;
                case UGUIVirtualListAlignment.Center:
                    position = start - (viewport - length) * 0.5f;
                    break;
                case UGUIVirtualListAlignment.End:
                    position = start + length - viewport + _target.Padding;
                    break;
                default:
                    float current = _list.ScrollPositionValue;
                    float end = start + length;
                    position = start < current ? start : end > current + viewport ? end - viewport : current;
                    break;
            }
            float max = Mathf.Max(0f, _list.ContentLength - viewport);
            return Mathf.Clamp(position, 0f, max);
        }

        private void OnUserScrollStarted()
        {
            if (_active) Cancel();
        }
        private void OnDataChanged()
        {
            if (_active) Cancel();
        }
        private static bool IsCurveSafe(AnimationCurve curve)
        {
            var keys = curve.keys;
            if (keys == null) return true;
            foreach (var key in keys)
                if (float.IsNaN(key.value) || float.IsInfinity(key.value) ||
                    float.IsNaN(key.time) || float.IsInfinity(key.time)) return false;
            return true;
        }
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _active = false;
            try { _list.ScrollStarted -= OnUserScrollStarted; } catch { }
            try { _list.DataChanged -= OnDataChanged; } catch { }
        }
    }
}
