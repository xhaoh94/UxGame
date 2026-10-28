using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ux
{
    /// <summary>
    /// 可选的 ScrollRect 适配器：布局补偿同步更新拖拽起点，并排除补偿产生的伪速度。
    /// 不修改 Unity 包源码、不反射私有字段。普通 ScrollRect 仍支持静止/惯性时的锚点补偿。
    /// </summary>
    [AddComponentMenu("Ux/UI/UGUI Virtual List Scroll Rect")]
    public class UGUIVirtualListScrollRect : ScrollRect
    {
        public event Action DragStarted;
        public event Action DragEnded;
        private bool _dragging;
        private Vector2 _previousSamplePosition;

        public override void OnBeginDrag(PointerEventData eventData)
        {
            base.OnBeginDrag(eventData);
            if (eventData.button != PointerEventData.InputButton.Left || !IsActive())
                return;
            _dragging = true;
            _previousSamplePosition = content.anchoredPosition;
            UpdatePrevData();
            DragStarted?.Invoke();
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            base.OnEndDrag(eventData);
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                _dragging = false;
                DragEnded?.Invoke();
            }
        }

        /// <summary>只移动布局参考系，不停止现有惯性，不额外触发一次 OnDrag。</summary>
        public void ApplyLayoutShift(Vector2 shift)
        {
            if (content == null || shift == Vector2.zero)
                return;
            content.anchoredPosition += shift;
            m_ContentStartPosition += shift;
            _previousSamplePosition += shift;
            UpdateBounds();
        }

        protected override void LateUpdate()
        {
            // ScrollRect 的内部上一帧位置是私有字段。独立维护同坐标系下的采样点，
            // 使用与原版相同的速度平滑公式，避免布局重建重置内部采样点而产生伪速度。
            float delta = Time.unscaledDeltaTime;
            bool sampleDrag = _dragging && inertia && content != null && delta > 0f;
            Vector2 dragVelocity = velocity;
            if (sampleDrag)
            {
                Vector2 movement = content.anchoredPosition - _previousSamplePosition;
                dragVelocity = Vector2.Lerp(velocity, movement / delta, delta * 10f);
            }
            base.LateUpdate();
            if (sampleDrag && _dragging)
                velocity = dragVelocity;
            if (content != null)
                _previousSamplePosition = content.anchoredPosition;
        }

        protected override void OnDisable()
        {
            _dragging = false;
            _previousSamplePosition = Vector2.zero;
            base.OnDisable();
        }
    }
}
