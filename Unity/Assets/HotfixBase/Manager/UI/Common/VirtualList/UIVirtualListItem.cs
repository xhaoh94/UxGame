using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ux
{
    /// <summary>
    /// UI 虚拟列表 Item 基类。列表控制器本身不依赖 MonoBehaviour，Prefab Item 仍可使用 Unity 组件。
    /// </summary>
    public abstract class UIVirtualListItem : MonoBehaviour, IUIVirtualListItem
    {
        private UnityAction _clickAction;
        private Action _clickHandler;
        private Button _button;
        private bool _buttonSearched;
        private RectTransform _rectTransform;

        public GameObject GameObject => gameObject;
        // 场景模板通常是 inactive，克隆对象的 Awake 未必已经执行，不能依赖 Awake 缓存引用。
        public RectTransform RectTransform
        {
            get
            {
                if (_rectTransform == null)
                    _rectTransform = transform as RectTransform;
                if (_rectTransform == null)
                    throw new InvalidOperationException("UIVirtualListItem 必须挂在 RectTransform 节点上。");
                return _rectTransform;
            }
        }

        protected virtual void Awake()
        {
            _rectTransform = RectTransform;
            _button = GetComponent<Button>();
            // 模板可能运行时才补 Button；没找到时不要置位，下次 Bind 继续尝试。
            _buttonSearched = _button != null;
        }

        public abstract void Bind(int index, object data);

        public virtual Vector2 Measure()
        {
            if (RectTransform == null)
                return Vector2.zero;

            LayoutRebuilder.ForceRebuildLayoutImmediate(RectTransform);
            return RectTransform.rect.size;
        }

        public virtual void OnRecycle()
        {
        }

        public void SetClickHandler(Action callback)
        {
            if (!_buttonSearched)
            {
                _button = GetComponent<Button>();
                _buttonSearched = _button != null;
            }
            if (_button == null)
                return;

            // 委托由 owner 按 item 实例常驻，同一实例复用时不重建 UnityAction、不增删监听。
            if (_clickAction == null)
            {
                _clickAction = InvokeClick;
                _clickHandler = callback;
                _button.onClick.AddListener(_clickAction);
            }
            else if (!ReferenceEquals(_clickHandler, callback))
            {
                // 换委托时重挂一次，同时修复监听被外部清空的场景。
                _clickHandler = callback;
                _button.onClick.RemoveListener(_clickAction);
                _button.onClick.AddListener(_clickAction);
            }
        }

        private void InvokeClick() => _clickHandler?.Invoke();
    }
}
