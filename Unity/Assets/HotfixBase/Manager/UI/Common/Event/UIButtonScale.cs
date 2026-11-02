using UnityEngine;
using UnityEngine.EventSystems;

namespace Ux
{
    /// <summary>
    /// 按钮点击缩放反馈。
    /// 默认在 AddClick 时自动挂载，参数可在代码里调整或关闭。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIButtonScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        /// <summary>全局默认：是否自动启用缩放。</summary>
        public static bool DefaultEnabled = true;

        /// <summary>全局默认：按下时的缩放比例。</summary>
        public static float DefaultPressedScale = 0.92f;

        /// <summary>全局默认：缩放过渡时长（秒）。</summary>
        public static float DefaultDuration = 0.08f;

        [SerializeField] private bool _enabled = true;
        [SerializeField] private float _pressedScale = 0.92f;
        [SerializeField] private float _duration = 0.08f;
        [SerializeField] private bool _useUnscaledTime = true;

        private Vector3 _baseScale;
        private float _current = 1f;
        private float _target = 1f;
        private bool _cached;

        public bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                if (_enabled) return;
                _current = _target = 1f;
                Apply();
            }
        }

        public float PressedScale
        {
            get => _pressedScale;
            set => _pressedScale = Mathf.Clamp(value, 0.1f, 1f);
        }

        public float Duration
        {
            get => _duration;
            set => _duration = Mathf.Max(0f, value);
        }

        private void Awake() => Cache();

        private void OnEnable()
        {
            Cache();
            _current = _target = 1f;
            Apply();
        }

        private void Cache()
        {
            if (_cached) return;
            _baseScale = transform.localScale;
            _cached = true;
        }

        public void Configure(bool enabled, float pressedScale, float duration)
        {
            Cache();
            _enabled = enabled;
            PressedScale = pressedScale;
            Duration = duration;
            if (_enabled) return;
            _current = _target = 1f;
            Apply();
        }

        /// <summary>确保目标节点挂有缩放组件，使用全局默认参数。</summary>
        public static UIButtonScale Ensure(GameObject target)
        {
            if (target == null)
                return null;

            // 已存在时直接返回，尊重已有的启用/禁用状态。
            var scale = target.GetComponent<UIButtonScale>();
            if (scale != null)
                return scale;

            scale = target.AddComponent<UIButtonScale>();
            scale.Configure(DefaultEnabled, DefaultPressedScale, DefaultDuration);
            return scale;
        }

        /// <summary>代码配置某个按钮的缩放；enabled = false 表示关闭。</summary>
        public static void Configure(GameObject target, bool enabled, float pressedScale, float duration)
        {
            if (target == null)
                return;

            // 即使当前要禁用，也要先把组件建出来。
            // 否则在 AddClick 之前调用 DisableClickScale 时组件还不存在，
            // 随后的 AddClick 会重新挂上并默认启用，禁用就丢了。
            var scale = target.GetComponent<UIButtonScale>();
            if (scale == null)
                scale = target.AddComponent<UIButtonScale>();

            scale.Configure(enabled, pressedScale, duration);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_enabled) return;
            Cache();
            _target = _pressedScale;
        }

        public void OnPointerUp(PointerEventData eventData) => _target = 1f;

        public void OnPointerExit(PointerEventData eventData) => _target = 1f;

        private void Update()
        {
            if (!_enabled || Mathf.Approximately(_current, _target))
                return;

            var deltaTime = _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            _current = _duration <= 0f
                ? _target
                : Mathf.MoveTowards(_current, _target, deltaTime / _duration);
            Apply();
        }

        private void Apply() => transform.localScale = _baseScale * _current;
    }
}
