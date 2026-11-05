using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Ux
{
    /// <summary>
    /// UI 指针事件桥接组件。
    /// 只负责把 Unity EventSystem 事件转发给纯 C# UIObject/UIEvent，不能承载业务逻辑。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIEventRelay : MonoBehaviour,
        IPointerClickHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerExitHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        public event Action<PointerEventData> Clicked;
        public event Action<PointerEventData> PointerDown;
        public event Action<PointerEventData> PointerUp;
        public event Action<PointerEventData> PointerExit;
        public event Action<PointerEventData> DragBegan;
        public event Action<PointerEventData> Dragged;
        public event Action<PointerEventData> DragEnded;

        private Func<bool> _longPressHandler;
        private float _longPressFirst;
        private float _longPressDelay;
        private int _longPressLoopCount;
        private int _longPressCurrentCount;
        private float _longPressHoldRange;
        private float _longPressStartTime;
        private float _longPressNextTime;
        private Vector2 _longPressStartPosition;
        private bool _longPressPressed;

        public void ConfigureLongPress(Func<bool> handler, float first, float delay, int loopCount, float holdRange)
        {
            _longPressHandler = handler;
            _longPressFirst = first < 0 ? 0f : first;
            _longPressDelay = Mathf.Max(0.01f, delay);
            _longPressLoopCount = loopCount;
            _longPressHoldRange = Mathf.Max(0f, holdRange);
        }

        public void ClearLongPress()
        {
            _longPressHandler = null;
            _longPressPressed = false;
            _longPressCurrentCount = 0;
        }

        private void Update()
        {
            if (!_longPressPressed || _longPressHandler == null)
                return;

            if (_longPressHoldRange > 0f &&
                (Input.mousePosition - (Vector3)_longPressStartPosition).sqrMagnitude >
                _longPressHoldRange * _longPressHoldRange)
            {
                _longPressPressed = false;
                return;
            }

            if (Time.unscaledTime < _longPressNextTime)
                return;

            if (_longPressCurrentCount == 0 && Time.unscaledTime - _longPressStartTime < _longPressFirst)
                return;

            _longPressCurrentCount++;
            _longPressNextTime = Time.unscaledTime + _longPressDelay;
            if (_longPressHandler.Invoke() ||
                (_longPressLoopCount > 0 && _longPressCurrentCount >= _longPressLoopCount))
            {
                _longPressPressed = false;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Clicked?.Invoke(eventData);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _longPressPressed = _longPressHandler != null;
            _longPressCurrentCount = 0;
            _longPressStartTime = Time.unscaledTime;
            _longPressNextTime = _longPressStartTime + _longPressFirst;
            _longPressStartPosition = eventData.position;
            PointerDown?.Invoke(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _longPressPressed = false;
            PointerUp?.Invoke(eventData);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _longPressPressed = false;
            PointerExit?.Invoke(eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            // 拖拽开始后不再触发长按：按下与拖动是互斥的手势。
            _longPressPressed = false;
            DragBegan?.Invoke(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            Dragged?.Invoke(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            DragEnded?.Invoke(eventData);
        }

        public void ClearListeners()
        {
            ClearLongPress();
            Clicked = null;
            PointerDown = null;
            PointerUp = null;
            PointerExit = null;
            DragBegan = null;
            Dragged = null;
            DragEnded = null;
        }
    }
}
