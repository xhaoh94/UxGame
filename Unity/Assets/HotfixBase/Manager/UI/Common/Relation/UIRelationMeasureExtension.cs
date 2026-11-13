using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Ux
{
    /// <summary>
    /// 虚拟列表测量扩展：先让 UGUI 完成布局，再同步应用 Item 子树内的 UIRelation，
    /// 然后重新排一次让 LayoutGroup / ContentSizeFitter 吸收关联结果，最后返回内层链的尺寸。
    /// 单例、无状态；关系数组按 RectTransform 缓存，每个 item 实例只收集一次。
    /// 边界：只处理 Item 子树内的关系。跨 Item 的目标会随复用/回收变化，结果不稳定，不支持。
    /// </summary>
    internal sealed class UIRelationMeasureExtension : IUIVirtualListMeasureExtension
    {
        /// <summary>
        /// 外层最多四轮（关系链深 4 + 确认）：每轮只推进一次关系、再重排一次，
        /// 而不是“内层 4 轮 × 外层 3 轮”的乘积。无变化时第二轮就退出。
        /// </summary>
        private const int MaxPasses = 4;

        // 弱键：item 回收/销毁后条目随托管对象回收，不需要在 Dispose 里手工清。
        private static readonly ConditionalWeakTable<RectTransform, UIRelation[]>.CreateValueCallback CreateEntries =
            Collect;

        private readonly ConditionalWeakTable<RectTransform, UIRelation[]> _cache = new();

        public Vector2 Measure(in UIVirtualListMeasureContext context, Func<int, Vector2> next)
        {
            var rect = context.Rect;
            if (rect == null) return next(context.Index);

            var relations = _cache.GetValue(rect, CreateEntries);
            // 模板没有关联：完全不碰额外路径。
            if (relations.Length == 0) return next(context.Index);

            var size = next(context.Index); // 内层链的返回值始终是权威尺寸
            for (int pass = 0; pass < MaxPasses; pass++)
            {
                // 每轮只跑一遍关系；有变化才重排一次，不再嵌套完整的多轮收敛。
                if (!UIRelationDriver.ApplyPass(relations)) break;
                // 关联改了子树尺寸：让内层链用新几何重算，默认 Measure 得到新的 rect.size，
                // 自定义 Measure 仍用自己的算法，两边都不会被覆盖。
                size = next(context.Index);
            }
            return size;
        }

        /// <summary>模板在运行时动态增删 UIRelation 后调用，丢弃该实例的收集结果。</summary>
        public void Invalidate(RectTransform rect)
        {
            if (rect != null && _cache.TryGetValue(rect, out _)) _cache.Remove(rect);
        }

        private static UIRelation[] Collect(RectTransform rect)
        {
            var found = rect.GetComponentsInChildren<UIRelation>(true);
            for (int i = 0; i < found.Length; i++)
                found[i].ExternalDriven = true;
            return found;
        }
    }
}
