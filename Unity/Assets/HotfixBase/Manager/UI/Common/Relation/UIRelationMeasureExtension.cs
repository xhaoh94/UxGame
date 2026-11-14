using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    /// <summary>
    /// 虚拟列表测量扩展：先让 UGUI 完成布局，再同步应用 Item 子树内的 UIRelation，
    /// 然后重排吸收关联结果，最后返回内层链的尺寸。
    /// 关系数组按 RectTransform 缓存，每个 item 实例只收集一次；关闭开关时用 ResetAll 撤销 ExternalDriven。
    /// 边界：只处理 Item 子树内的关系。跨 Item 的目标会随复用/回收变化，结果不稳定，不支持。
    /// </summary>
    public sealed class UIRelationMeasureExtension : IUIVirtualListMeasureExtension
    {
        private readonly Dictionary<RectTransform, UIRelation[]> _relations = new();
        private int _lastWarnFrame = -1;

        /// <summary>上一次测量的内层链调用次数，测试/诊断用。</summary>
        internal int LastNextCalls { get; private set; }

        /// <summary>关系收集次数；Invalidate 后再次测量会递增，测试用。</summary>
        internal int CollectCount { get; private set; }

        /// <summary>触达轮数上限的次数，测试/诊断用。</summary>
        internal int ExhaustedCount { get; private set; }

        public Vector2 Measure(in UIVirtualListMeasureContext context, Func<int, Vector2> next)
        {
            var rect = context.Rect;
            if (rect == null)
            {
                LastNextCalls = 1;
                return next(context.Index);
            }

            if (!_relations.TryGetValue(rect, out var relations))
            {
                relations = Collect(rect);
                _relations.Add(rect, relations);
            }

            // 模板没有关联：完全不碰额外路径。
            if (relations.Length == 0)
            {
                LastNextCalls = 1;
                return next(context.Index);
            }

            LastNextCalls = 0;
            var size = next(context.Index); // 内层链的返回值始终是权威尺寸
            LastNextCalls++;
            for (int pass = 0; pass < UIRelationDriver.MaxPasses; pass++)
            {
                // 每轮只跑一遍关系；有变化才重排一次，不再嵌套完整的多轮收敛。
                if (!UIRelationDriver.ApplyPass(relations)) break;
                size = next(context.Index);
                LastNextCalls++;
                if (pass == UIRelationDriver.MaxPasses - 1) WarnExhausted();
            }
            return size;
        }

        /// <summary>模板在运行时动态增删 UIRelation 后调用，丢弃该实例的收集结果。</summary>
        public void Invalidate(RectTransform rect)
        {
            if (rect == null) return;
            if (_relations.TryGetValue(rect, out var relations))
            {
                ClearExternalDriven(relations);
                _relations.Remove(rect);
            }
        }

        /// <summary>关闭外部驱动时调用：撤回 ExternalDriven 并清缓存，避免关系系统继续跳过 relink。</summary>
        public void ResetAll()
        {
            foreach (var relations in _relations.Values)
                ClearExternalDriven(relations);
            _relations.Clear();
        }

        private static void ClearExternalDriven(UIRelation[] relations)
        {
            for (int i = 0; i < relations.Length; i++)
                if (relations[i] != null) relations[i].ExternalDriven = false;
        }

        private UIRelation[] Collect(RectTransform rect)
        {
            CollectCount++;
            var found = rect.GetComponentsInChildren<UIRelation>(true);
            for (int i = 0; i < found.Length; i++)
                found[i].ExternalDriven = true;
            return found;
        }

        private void WarnExhausted()
        {
            ExhaustedCount++;
            // 每帧最多报一次：链式关系未收敛时所有可见项都会触顶。
            if (Time.frameCount == _lastWarnFrame) return;
            _lastWarnFrame = Time.frameCount;
            Debug.LogWarning(
                $"虚拟列表关系测量达到 {UIRelationDriver.MaxPasses} 轮上限，链式关联可能未收敛；请检查 UIRelation 依赖深度。");
        }
    }
}
