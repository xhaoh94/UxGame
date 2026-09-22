using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    /// <summary>
    /// 阶段 Hitbox 插件：本帧帧事件 → 命中候选，写进 BattleWorld.PendingHits 交伤害阶段。
    ///
    /// 只做几何：帧区间由 Timeline 阶段求值、同窗去重由 Runner.TryAcceptHit，这里都不重算。
    /// 前提：本阶段不改生命和位置，所以目标快照才敢延迟到第一次查询前才收集。
    /// </summary>
    public sealed class HitboxSystem : ICombatSystem
    {
        /// <summary>世界坐标 → 逻辑毫米：1 世界单位 = 1000 毫米。改它等于改所有 radiusMillimeters 的含义。</summary>
        public const float MillimetersPerUnit = 1000f;

        /// <summary>输出"窗口激活但无人中招"的日志，用来判断半径是否配小、坐标是否错位。</summary>
        public static bool Verbose = true;

        public BattlePhase Phase => BattlePhase.Hitbox;

        public int Order => 0;

        private readonly List<CombatHitTarget> _targets = new();
        private readonly List<CombatHitCandidate> _hits = new();

        public void Tick(BattleWorld world, long frame)
        {
            var events = world.FrameEvents;
            var pending = world.PendingHits;

            // 局部量而不是字段：生命周期锁在一次 Tick 内，不存在漏复位。
            var targetsReady = false;

            // 顺序 = Timeline 写入顺序（Id 升序），是确定性的一部分，也决定伤害结算的先后。
            for (var i = 0; i < events.Count; i++)
            {
                var set = events[i];
                if (!set.HasHitWindows)
                {
                    continue;
                }

                if (!world.TryGetEntity(set.EntityId, out var entity))
                {
                    continue;
                }

                if (!targetsReady)
                {
                    CollectTargets(world);
                    targetsReady = true;
                }

                // 必须在循环内清：_hits 跨攻击者复用，上一位的命中不能带进下一位。
                _hits.Clear();
                var added = CombatHitResolver.AppendResolvedHits(
                    entity.Controller.ActionRunner,
                    new CombatHitQuerySource(set.EntityId, ToFixedPoint(entity.Position)),
                    set.HitWindows,
                    _targets,
                    _hits);

                for (var hitIndex = 0; hitIndex < added; hitIndex++)
                {
                    var hit = _hits[hitIndex];
                    pending.Add(hit);
                    Log.Debug($"[Hitbox] frame={frame} {hit.SourceEntityId} → {hit.TargetEntityId} " +
                              $"action={hit.ActionId} actionFrame={hit.ActionFrame} window={hit.WindowId}");
                }

                if (Verbose && added == 0)
                {
                    LogActiveWindowWithoutHit(set, frame, _targets.Count);
                }
            }
        }

        /// <summary>
        /// 收集本帧目标：活着的单位，按 Id 升序，只在本帧第一次查询之前收一次。
        ///
        /// 前提：位置在第 2 槽就定稿、PendingHits 要到第 5 槽才消费，所以本阶段内位置和生命不变，
        /// 收集时刻不影响结果。谁要在本阶段改位置（比如把击退做进来），必须挪回 Tick 开头无条件收集。
        /// </summary>
        private void CollectTargets(BattleWorld world)
        {
            _targets.Clear();

            // 按索引遍历，不用 foreach world.Entities（接口枚举器会装箱，每帧一个堆对象）。
            var entities = world.OrderedEntities;
            for (var i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                if (!entity.IsCombatActive || entity.Id <= 0)
                {
                    // Id <= 0 会让 CombatHitResolver 抛异常。
                    continue;
                }

                if (entity.Controller.StateMachine.Life == LifeState.Alive)
                {
                    _targets.Add(new CombatHitTarget(entity.Id, ToFixedPoint(entity.Position)));
                }
            }
        }

        /// <summary>窗口激活但一个目标都没进圈时的诊断输出。</summary>
        private static void LogActiveWindowWithoutHit(CombatFrameEventSet set, long frame, int targetCount)
        {
            for (var i = 0; i < set.HitWindows.Count; i++)
            {
                var window = set.HitWindows[i];
                Log.Debug($"[Hitbox] frame={frame} {set.EntityId} 窗口激活 " +
                          $"actionFrame={window.ActionFrame} [{window.StartFrame},{window.EndFrame}) " +
                          $"shape={window.Shape} radius={window.RadiusMillimeters}mm " +
                          $"候选目标={targetCount} 命中=0");
            }
        }

        private static CombatFixedPoint ToFixedPoint(Vector3 position)
        {
            return new CombatFixedPoint(
                (int)Math.Round(position.x * MillimetersPerUnit),
                (int)Math.Round(position.z * MillimetersPerUnit));
        }
    }
}
