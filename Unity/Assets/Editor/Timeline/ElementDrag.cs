using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ux.Editor
{
    public class ElementDrag
    {
        bool isDrag = false;
        Vector2 moveDelta = Vector2.zero;
        Action start;
        Action<Vector2> move;
        Action end;
        VisualElement ve;
        VisualElement content;

        public static ElementDrag Add(VisualElement element, VisualElement content, Action<Vector2> move, int button = -1)
        {
            return new ElementDrag(element, content, null, move, null, button);
        }
        public static ElementDrag Add(VisualElement element, VisualElement content, Action start, Action<Vector2> move, int button = -1)
        {
            return new ElementDrag(element, content, start, move, null, button);
        }
        public static ElementDrag Add(VisualElement element, VisualElement content, Action<Vector2> move, Action end, int button = -1)
        {
            return new ElementDrag(element, content, null, move, end, button);
        }
        public static ElementDrag Add(VisualElement element, VisualElement content, Action start, Action<Vector2> move, Action end, int button = -1)
        {
            return new ElementDrag(element, content, start, move, end, button);
        }
        public ElementDrag(VisualElement ve, VisualElement content, Action start, Action<Vector2> move, Action end, int button = -1)
        {
            this.start = start;
            this.move = move;
            this.end = end;
            this.ve = ve;
            this.content = content;
            ve.RegisterCallback<PointerDownEvent>((e) =>
            {
                if (!isDrag && (button == -1 || e.button == button))
                {
                    isDrag = true;
                    start?.Invoke();
                    moveDelta = (Vector2)e.position;
                    content.RegisterCallback<PointerMoveEvent>(OnMove);                    
                    content.RegisterCallback<PointerUpEvent>(OnUp);
                    content.RegisterCallback<MouseLeaveEvent>(OnOut);                    
                }
            });
        }


        void OnMove(PointerMoveEvent e)
        {
            if (isDrag)
            {
                var pointerPosition = (Vector2)e.position;
                if (!pointerPosition.Equals(moveDelta))
                {
                    move?.Invoke(pointerPosition - moveDelta);
                    moveDelta = pointerPosition;
                }
            }
        }
        void OnUp(PointerUpEvent e)
        {
            if (isDrag)
            {
                isDrag = false;
                end?.Invoke();
                moveDelta = Vector3.zero;
                content.UnregisterCallback<PointerMoveEvent>(OnMove);
                content.UnregisterCallback<PointerUpEvent>(OnUp);
                content.UnregisterCallback<MouseLeaveEvent>(OnOut);
            }
        }
        void OnOut(MouseLeaveEvent e)
        {
            OnUp(null);
        }
    }
}