using UnityEngine;
using UnityEngine.EventSystems;

namespace Ux
{
    /// <summary>
    /// UI 事件上下文。普通 UI 节点通过 GameObject 绑定。
    /// </summary>
    public readonly struct UIEventContext
    {
        public GameObject Target { get; }
        public PointerEventData PointerEventData { get; }

        public UIEventContext(GameObject target, PointerEventData pointerEventData = null)
        {
            Target = target;
            PointerEventData = pointerEventData;
        }
    }
}
