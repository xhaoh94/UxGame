using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ux
{
    /// <summary>
    /// UGUI 虚拟列表 Item 基类。列表控制器本身不依赖 MonoBehaviour，Prefab Item 仍可使用 Unity 组件。
    /// </summary>
    public abstract class UGUIVirtualListItem : MonoBehaviour, IUGUIVirtualListItem
    {
        private UnityAction _clickAction;
        private Button _button;
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
                    throw new InvalidOperationException("UGUIVirtualListItem 必须挂在 RectTransform 节点上。");
                return _rectTransform;
            }
        }

        protected virtual void Awake()
        {
            _rectTransform = RectTransform;
            _button = GetComponent<Button>();
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
            if (_button == null)
                _button = GetComponent<Button>();
            if (_button == null)
                return;

            if (_clickAction != null)
                _button.onClick.RemoveListener(_clickAction);

            _clickAction = callback == null
                ? null
                : new UnityAction(callback);

            if (_clickAction != null)
                _button.onClick.AddListener(_clickAction);
        }
    }
}
