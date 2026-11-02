using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ux
{
    /// <summary>
    /// 绑定一个 UIController：按页面覆盖可见性、位置、尺寸、颜色、文本、透明度、旋转、缩放。
    /// 未勾选的属性回落到对象进入控制器前的默认状态，等价于 FairyGUI 的各类 Gear。
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("Ux/UI/UI Controller Binding")]
    public sealed class UIControllerBinding : MonoBehaviour
    {
        [SerializeField] private UIController _controller;
        [SerializeField] private ControllerState[] _states = Array.Empty<ControllerState>();

        private RectTransform _rect;
        private Graphic _graphic;
        private TMP_Text _tmpText;
        private Text _legacyText;
        private CanvasGroup _canvasGroup;
        // 默认状态随 Prefab 保存：域重载或重新启用时不会把预览状态误当成基准。
        [SerializeField] private ControllerState _defaultState;
        [SerializeField] private bool _captured;

        // 不依赖 isActiveAndEnabled 判断注册状态：控制器可以通过 SetActive(false)
        // 隐藏绑定对象，但隐藏后仍必须继续参与后续切页。
        private UIController _registeredController;

        public UIController Controller
        {
            get => _controller;
            set
            {
                if (_controller == value)
                {
                    SyncControllerRegistration();
                    return;
                }

                var shouldRegister = isActiveAndEnabled || _registeredController != null;
                UnregisterController();
                _controller = value;
                if (shouldRegister) RegisterController();
            }
        }

        public ControllerState[] States
        {
            get => _states;
            set
            {
                _states = value ?? Array.Empty<ControllerState>();
                ApplyCurrentPageIfRegistered();
            }
        }

        private RectTransform Rect
        {
            get
            {
                if (_rect == null) _rect = transform as RectTransform;
                return _rect;
            }
        }

        private Graphic Graphic
        {
            get
            {
                if (_graphic != null) return _graphic;
                // Image 优先：同一节点同时有 Image 与文本时，颜色/透明度以背景图为准。
                _graphic = GetComponent<Image>();
                if (_graphic == null) _graphic = GetComponent<TMP_Text>();
                if (_graphic == null) _graphic = GetComponent<Graphic>();
                return _graphic;
            }
        }

        private TMP_Text TmpText
        {
            get
            {
                if (_tmpText == null) _tmpText = GetComponent<TMP_Text>();
                return _tmpText;
            }
        }

        private Text LegacyText
        {
            get
            {
                if (_legacyText == null) _legacyText = GetComponent<Text>();
                return _legacyText;
            }
        }

        private CanvasGroup CanvasGroup
        {
            get
            {
                if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
                return _canvasGroup;
            }
        }

        private void OnEnable()
        {
            if (!_captured) CaptureDefault();
            RegisterController();
        }

        private void OnDisable()
        {
            // 不在 OnDisable 反注册：控制器可能正是通过 SetVisible 关闭本对象，
            // 反注册会导致之后切页再也应用不上。绑定关系由 _registeredController 保留。
        }

        private void OnDestroy()
        {
            UnregisterController();
        }

        private void RegisterController()
        {
            if (_controller == null) return;
            if (_registeredController == _controller) return;

            UnregisterController();
            _registeredController = _controller;
            _registeredController.Register(this);
        }

        private void UnregisterController()
        {
            if (_registeredController == null) return;
            _registeredController.Unregister(this);
            _registeredController = null;
        }

        private void SyncControllerRegistration()
        {
            // 尚未启用且从未注册过时，等待 OnEnable，避免编辑器序列化阶段提前改场景对象。
            if (!isActiveAndEnabled && _registeredController == null) return;

            if (_registeredController != _controller)
            {
                UnregisterController();
                RegisterController();
            }
            else
            {
                ApplyCurrentPageIfRegistered();
            }
        }

        private void ApplyCurrentPageIfRegistered()
        {
            if (_registeredController != null)
                ApplyPage(_registeredController.SelectedIndex);
        }

        internal void MarkRegistered(UIController controller)
        {
            _registeredController = controller;
        }

        internal void MarkUnregistered(UIController controller)
        {
            if (_registeredController == controller)
                _registeredController = null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Inspector 通过 SerializedProperty 修改 _controller / _states 时不会调用 C# setter。
            // 延迟到当前 Inspector 绘制结束后同步，避免在 OnGUI 中直接 SetActive 或改 RectTransform。
            if (!isActiveAndEnabled && _registeredController == null) return;
            UnityEditor.EditorApplication.delayCall -= DelayedValidate;
            UnityEditor.EditorApplication.delayCall += DelayedValidate;
        }

        private void DelayedValidate()
        {
            if (this == null) return;
            SyncControllerRegistration();
        }
#endif

        /// <summary>节点是否具备 RectTransform，位置/尺寸/旋转/缩放需要它。</summary>
        public bool HasRectTransform => transform is RectTransform;

        /// <summary>节点是否带 Graphic（Image / Text / TMP 等），颜色需要它。</summary>
        public bool HasGraphic => Graphic != null;

        /// <summary>节点是否带文本组件，文本需要它。</summary>
        public bool HasText => TmpText != null || LegacyText != null;

        /// <summary>透明度需要 CanvasGroup 或 Graphic（退化为顶点色 alpha）。</summary>
        public bool HasAlphaControl => CanvasGroup != null || Graphic != null;

        /// <summary>读取目标当前实际外观，不修改默认状态或页面状态。</summary>
        public ControllerState CaptureCurrentState()
        {
            var rect = Rect;
            var graphic = Graphic;
            return new ControllerState
            {
                Visible = gameObject.activeSelf,
                Position = rect != null ? rect.anchoredPosition : Vector2.zero,
                Size = rect != null ? rect.sizeDelta : Vector2.zero,
                Color = graphic != null ? graphic.color : Color.white,
                Text = TmpText != null ? TmpText.text
                    : LegacyText != null ? LegacyText.text : null,
                Alpha = CanvasGroup != null
                    ? CanvasGroup.alpha
                    : graphic != null ? graphic.color.a : 1f,
                Rotation = rect != null ? rect.localEulerAngles.z : 0f,
                Scale = rect != null ? rect.localScale : Vector3.one,
            };
        }

        /// <summary>记录当前外观为默认状态，未配置的页面会回落到这里。</summary>
        public void CaptureDefault()
        {
            _defaultState = CaptureCurrentState();
            _captured = true;
        }

        /// <summary>手动重新记录默认状态（编辑器“记录当前状态”按钮）。</summary>
        public void RecaptureDefault() => CaptureDefault();

        /// <summary>应用某一页的配置；找不到该页时全部回落到默认状态。</summary>
        public void ApplyPage(int pageIndex)
        {
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isCompiling ||
                UnityEditor.EditorApplication.isUpdating ||
                UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                return;
#endif
            if (!_captured) CaptureDefault();

            var state = _defaultState;
            if (TryGetState(pageIndex, out var page))
            {
                if (page.SetVisible) state.Visible = page.Visible;
                if (page.SetPosition) state.Position = page.Position;
                if (page.SetSize) state.Size = page.Size;
                if (page.SetColor) state.Color = page.Color;
                if (page.SetText) state.Text = page.Text;
                if (page.SetAlpha) state.Alpha = page.Alpha;
                if (page.SetRotation) state.Rotation = page.Rotation;
                if (page.SetScale) state.Scale = page.Scale;
            }

            Apply(state);
        }

        private bool TryGetState(int pageIndex, out ControllerState state)
        {
            if (_states != null)
            {
                for (int i = 0; i < _states.Length; i++)
                {
                    if (_states[i].PageIndex != pageIndex) continue;
                    state = _states[i];
                    return true;
                }
            }
            state = default;
            return false;
        }

        private void Apply(in ControllerState state)
        {
            var rect = Rect;
            if (rect != null)
            {
                if (rect.anchoredPosition != state.Position)
                    rect.anchoredPosition = state.Position;
                if (rect.sizeDelta != state.Size)
                    rect.sizeDelta = state.Size;

                var scale = state.Scale;
                if (scale.x <= 0f && scale.y <= 0f && scale.z <= 0f) scale = Vector3.one;
                if (rect.localScale != scale)
                    rect.localScale = scale;

                var euler = rect.localEulerAngles;
                if (Mathf.Abs(Mathf.DeltaAngle(euler.z, state.Rotation)) > 0.01f)
                    rect.localEulerAngles = new Vector3(euler.x, euler.y, state.Rotation);
            }

            var graphic = Graphic;
            if (graphic != null)
            {
                var color = state.Color;
                // 没有 CanvasGroup 时把透明度并入顶点色。
                if (CanvasGroup == null) color.a = state.Alpha;
                if (graphic.color != color)
                    graphic.color = color;
            }

            var group = CanvasGroup;
            if (group != null && !Mathf.Approximately(group.alpha, state.Alpha))
                group.alpha = state.Alpha;

            if (TmpText != null)
            {
                var text = state.Text ?? string.Empty;
                if (TmpText.text != text) TmpText.text = text;
            }
            else if (LegacyText != null)
            {
                var text = state.Text ?? string.Empty;
                if (LegacyText.text != text) LegacyText.text = text;
            }

            if (gameObject.activeSelf != state.Visible)
                gameObject.SetActive(state.Visible);
        }
    }
}
