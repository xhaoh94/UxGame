using System;
using System.Collections.Generic;

namespace Ux
{
    /// <summary>无渲染战斗使用的二维逻辑坐标，单位为整数毫米。</summary>
    public readonly struct CombatFixedPoint : IEquatable<CombatFixedPoint>
    {
        public CombatFixedPoint(int xMillimeters, int zMillimeters)
        {
            XMillimeters = xMillimeters;
            ZMillimeters = zMillimeters;
        }

        public int XMillimeters { get; }
        public int ZMillimeters { get; }

        public bool Equals(CombatFixedPoint other) =>
            XMillimeters == other.XMillimeters && ZMillimeters == other.ZMillimeters;

        public override bool Equals(object obj) =>
            obj is CombatFixedPoint other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(XMillimeters, ZMillimeters);
    }

    /// <summary>
    /// 命中查询的攻击者输入。坐标必须由上层从本帧逻辑状态提供，核心不读取 Transform 或 Physics。
    /// </summary>
    public readonly struct CombatHitQuerySource
    {
        public CombatHitQuerySource(long entityId, CombatFixedPoint position)
        {
            EntityId = entityId;
            Position = position;
        }

        public long EntityId { get; }
        public CombatFixedPoint Position { get; }
    }

    /// <summary>上层筛选后的逻辑目标快照。目标顺序不作为规则，Resolver 会按 Id 排序。</summary>
    public readonly struct CombatHitTarget
    {
        public CombatHitTarget(long entityId, CombatFixedPoint position)
        {
            EntityId = entityId;
            Position = position;
        }

        public long EntityId { get; }
        public CombatFixedPoint Position { get; }
    }

    /// <summary>一次确定性命中候选，不包含伤害数值或目标属性。</summary>
    public readonly struct CombatHitCandidate
    {
        public CombatHitCandidate(
            long sourceEntityId,
            long targetEntityId,
            CombatActiveHitWindow window)
        {
            SourceEntityId = sourceEntityId;
            TargetEntityId = targetEntityId;
            ActionInstanceId = window.ActionInstanceId;
            ActionId = window.ActionId;
            ActionFrame = window.ActionFrame;
            WindowIndex = window.WindowIndex;
            WindowId = window.WindowId;
        }

        public long SourceEntityId { get; }
        public long TargetEntityId { get; }
        public long ActionInstanceId { get; }
        public int ActionId { get; }
        public int ActionFrame { get; }
        public int WindowIndex { get; }
        public string WindowId { get; }
    }

    /// <summary>Runner 内部的“同一动作实例/窗口/目标”去重键。</summary>
    public readonly struct CombatHitKey : IEquatable<CombatHitKey>
    {
        public CombatHitKey(long actionInstanceId, string windowId, long targetId)
        {
            ActionInstanceId = actionInstanceId;
            WindowId = windowId ?? string.Empty;
            TargetId = targetId;
        }

        public long ActionInstanceId { get; }
        public string WindowId { get; }
        public long TargetId { get; }

        public bool Equals(CombatHitKey other) =>
            ActionInstanceId == other.ActionInstanceId &&
            TargetId == other.TargetId &&
            string.Equals(WindowId, other.WindowId, StringComparison.Ordinal);

        public override bool Equals(object obj) =>
            obj is CombatHitKey other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(ActionInstanceId, WindowId, TargetId);
    }

    /// <summary>
    /// 逻辑命中查询适配层。它只使用固定坐标和已筛选目标，不调用 Unity Physics。
    /// 当前形状集合只有圆形；新增形状必须保持整数/确定性计算，并补充独立校验。
    /// </summary>
    public static class CombatHitResolver
    {
        public static int AppendResolvedHits(
            CombatActionRunner runner,
            in CombatHitQuerySource source,
            IReadOnlyList<CombatHitTarget> targets,
            List<CombatHitCandidate> output)
        {
            if (runner == null)
            {
                throw new ArgumentNullException(nameof(runner));
            }
            if (source.EntityId <= 0)
            {
                throw new InvalidOperationException(
                    $"命中查询攻击者 ID 必须为正数：{source.EntityId}");
            }
            if (targets == null)
            {
                throw new ArgumentNullException(nameof(targets));
            }
            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }
            var targetOrder = new List<int>(targets.Count);
            var targetIds = new HashSet<long>();
            for (var i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                if (target.EntityId <= 0)
                {
                    throw new InvalidOperationException(
                        $"命中查询目标 ID 必须为正数：{target.EntityId}");
                }
                if (!targetIds.Add(target.EntityId))
                {
                    throw new InvalidOperationException(
                        $"命中查询目标 ID 重复：{target.EntityId}");
                }
                if (target.EntityId != source.EntityId)
                {
                    targetOrder.Add(i);
                }
            }

            if (!runner.HasAction)
            {
                return 0;
            }

            var windows = new List<CombatActiveHitWindow>();
            runner.AppendActiveHitWindows(windows);
            if (windows.Count == 0 || targetOrder.Count == 0)
            {
                return 0;
            }
            targetOrder.Sort((left, right) =>
            {
                var leftId = targets[left].EntityId;
                var rightId = targets[right].EntityId;
                return leftId.CompareTo(rightId);
            });

            var added = 0;
            for (var windowIndex = 0; windowIndex < windows.Count; windowIndex++)
            {
                var window = windows[windowIndex];
                if (window.Shape != ActionHitShape.Circle || window.RadiusMillimeters <= 0)
                {
                    continue;
                }

                for (var targetIndex = 0; targetIndex < targetOrder.Count; targetIndex++)
                {
                    var target = targets[targetOrder[targetIndex]];
                    if (!IsInsideCircle(source.Position, target.Position, window.RadiusMillimeters))
                    {
                        continue;
                    }
                    if (!runner.TryAcceptHit(window.ActionInstanceId, window.WindowId, target.EntityId))
                    {
                        continue;
                    }

                    output.Add(new CombatHitCandidate(source.EntityId, target.EntityId, window));
                    added++;
                }
            }
            return added;
        }

        static bool IsInsideCircle(
            CombatFixedPoint source,
            CombatFixedPoint target,
            int radiusMillimeters)
        {
            // 坐标是 int、半径有资产上限；decimal 避免 long 平方溢出并保持精确整数比较。
            var dx = (decimal)target.XMillimeters - source.XMillimeters;
            var dz = (decimal)target.ZMillimeters - source.ZMillimeters;
            var radius = radiusMillimeters;
            return dx * dx + dz * dz <= (decimal)radius * radius;
        }
    }
}
