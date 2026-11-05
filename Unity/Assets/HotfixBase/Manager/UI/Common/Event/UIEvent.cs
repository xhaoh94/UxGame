using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ux
{
    /// <summary>
    /// UI 事件注册器。它是纯 C# 对象，实际的 Pointer 回调由 UIEventRelay 转发。
    /// </summary>
    public sealed class UIEvent
    {
        private readonly List<Binding> _bindings = new List<Binding>();

        public void AddClick(GameObject target, Action<UIEventContext> callback)
        {
            if (target == null || callback == null)
                return;

            var binding = GetOrCreate(target);
            Action<PointerEventData> handler = data => callback(new UIEventContext(target, data));
            binding.Relay.Clicked += handler;
            binding.Cleanup += () => binding.Relay.Clicked -= handler;
        }

        public void AddClick(GameObject target, Action callback)
        {
            if (target == null || callback == null)
                return;

            var binding = GetOrCreate(target);
            Action<PointerEventData> handler = _ => callback();
            binding.Relay.Clicked += handler;
            binding.Cleanup += () => binding.Relay.Clicked -= handler;
        }

        /// <summary>
        /// 注册按下：轻量回调，用于“点一下就把窗口提到最上”这类不需要拖动的场景。
        /// </summary>
        public void AddPointerDown(GameObject target, Action<UIEventContext> callback)
        {
            if (target == null || callback == null)
                return;

            var binding = GetOrCreate(target);
            Action<PointerEventData> handler = data => callback(new UIEventContext(target, data));
            binding.Relay.PointerDown += handler;
            binding.Cleanup += () => binding.Relay.PointerDown -= handler;
        }

        /// <summary>
        /// 注册拖拽：onBegin/onEnd 可选，onDrag 必填。
        /// 拖拽热区由 GetOrCreate 自动补齐（无 Graphic 时加透明 Image）。
        /// </summary>
        public void AddDrag(GameObject target,
            Action<UIEventContext> onDrag,
            Action<UIEventContext> onBegin = null,
            Action<UIEventContext> onEnd = null)
        {
            if (target == null || onDrag == null)
                return;

            var binding = GetOrCreate(target);

            if (onBegin != null)
            {
                Action<PointerEventData> begin = data => onBegin(new UIEventContext(target, data));
                binding.Relay.DragBegan += begin;
                binding.Cleanup += () => binding.Relay.DragBegan -= begin;
            }

            Action<PointerEventData> drag = data => onDrag(new UIEventContext(target, data));
            binding.Relay.Dragged += drag;
            binding.Cleanup += () => binding.Relay.Dragged -= drag;

            if (onEnd != null)
            {
                Action<PointerEventData> end = data => onEnd(new UIEventContext(target, data));
                binding.Relay.DragEnded += end;
                binding.Cleanup += () => binding.Relay.DragEnded -= end;
            }
        }

        public void AddMultipleClick(GameObject target, Action<UIEventContext> callback,
            int clickCount = 2, float gapTime = 0.3f)
        {
            if (target == null || callback == null)
                return;

            var binding = GetOrCreate(target);
            var count = 0;
            var lastClickTime = -999f;
            Action<PointerEventData> handler = data =>
            {
                if (Time.unscaledTime - lastClickTime > gapTime)
                    count = 0;

                lastClickTime = Time.unscaledTime;
                count++;
                if (count >= clickCount)
                {
                    count = 0;
                    callback(new UIEventContext(target, data));
                }
            };
            binding.Relay.Clicked += handler;
            binding.Cleanup += () => binding.Relay.Clicked -= handler;
        }

        public void AddLongPress(GameObject target, Func<bool> callback,
            float first, float delay, int loopCount, int holdRangeRadius)
        {
            if (target == null || callback == null)
                return;

            var binding = GetOrCreate(target);
            binding.Relay.ConfigureLongPress(callback, first, delay, loopCount, holdRangeRadius);
            binding.Cleanup += binding.Relay.ClearLongPress;
        }

        public void RemoveEvents()
        {
            for (int i = 0; i < _bindings.Count; i++)
                _bindings[i].Dispose();
            _bindings.Clear();
        }

        public void Release()
        {
            RemoveEvents();
        }

        private Binding GetOrCreate(GameObject target)
        {
            for (int i = 0; i < _bindings.Count; i++)
            {
                if (_bindings[i].Target == target)
                    return _bindings[i];
            }

            var relay = target.GetComponent<UIEventRelay>();
            if (relay == null)
                relay = target.AddComponent<UIEventRelay>();

            // 点击默认带缩放反馈，参数可在代码里改或关闭。
            if (UIButtonScale.DefaultEnabled)
                UIButtonScale.Ensure(target);

            // 没有任何 Graphic 的节点默认不参与射线检测，补一个透明 Image 才能收到点击。
            if (target.GetComponent<Graphic>() == null)
            {
                EnsureRaycastGraphic(target);
            }
            else
            {
                EnsureRaycastEnabled(target);
            }

            var binding = new Binding(target, relay);
            _bindings.Add(binding);
            return binding;
        }

        private static void EnsureRaycastGraphic(GameObject target)
        {
            var image = target.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = true;
        }

        /// <summary>
        /// 节点上的 Graphic 可能默认关掉了 raycastTarget，开启后才能响应点击。
        /// </summary>
        private static void EnsureRaycastEnabled(GameObject target)
        {
            var graphics = target.GetComponents<Graphic>();
            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i].raycastTarget)
                    return;
            }

            if (graphics.Length > 0)
                graphics[0].raycastTarget = true;
        }

        private sealed class Binding
        {
            public readonly GameObject Target;
            public readonly UIEventRelay Relay;
            public Action Cleanup;

            public Binding(GameObject target, UIEventRelay relay)
            {
                Target = target;
                Relay = relay;
            }

            public void Dispose()
            {
                Cleanup?.Invoke();
                Cleanup = null;
            }
        }
    }
}
