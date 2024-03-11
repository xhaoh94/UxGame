using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ux.Editor
{
    public class VeDrag
    {
        bool isDrag = false;
        Vector3 moveDelta = Vector3.zero;
        Action<Vector3> start;
        Action<Vector3> move;
        Action<Vector3> end;
        VisualElement ve;
        public VeDrag(VisualElement ve, Action<Vector3> start, Action<Vector3> move, Action<Vector3> end, int button = -1)
        {
            this.start = start;
            this.move = move;
            this.end = end;
            this.ve = ve;
            ve.RegisterCallback<PointerDownEvent>((e) =>
            {
                if (button == -1 || e.button == button)
                {
                    isDrag = true;
                    moveDelta = e.localPosition;
                    start?.Invoke(moveDelta);
                    ve.RegisterCallback<PointerMoveEvent>(OnMove);
                    ve.RegisterCallback<PointerUpEvent>(OnUp);
                    ve.RegisterCallback<PointerOutEvent>(OnOut);
                }
            });
        }

        void OnMove(PointerMoveEvent e)
        {
            if (isDrag)
            {
                move?.Invoke(e.localPosition - moveDelta);
                moveDelta = e.localPosition;
            }
        }
        void OnOut(PointerOutEvent e)
        {
            OnCancel();
            this.end?.Invoke(e.localPosition);
        }
        void OnUp(PointerUpEvent e)
        {
            OnCancel();
            this.end?.Invoke(e.localPosition);
        }
        void OnCancel()
        {
            isDrag = false;
            moveDelta = Vector3.zero;
            ve.UnregisterCallback<PointerMoveEvent>(OnMove);
            ve.UnregisterCallback<PointerUpEvent>(OnUp);
            ve.UnregisterCallback<PointerOutEvent>(OnOut);
        }
    }
}