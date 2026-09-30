using System;

namespace Ux
{
    /// <summary>
    /// 阶段 Timeline 插件：把生成事件变成真实体，并回收已经死掉的投射物。
    ///
    /// 排在 CombatTimelineSystem 之后（Order 更大），读它刚写好的帧事件。
    /// 去重落在 Runner 上（与 TryAcceptHit 同构）—— 窗口是区间语义、每帧都成立，
    /// 边沿判定只能由消费方承担。
    /// </summary>
    public sealed class CombatSpawnSystem : ICombatSystem
    {
        /// <summary>输出每次生成与回收的日志。只影响日志，不影响模拟结果。</summary>
        public static bool Verbose = true;

        /// <summary>投射物 ID 基址。给大值可以让它稳定排在所有小 ID 的角色之后（遍历序 = Id 升序）。</summary>
        public const long ProjectileIdBase = 1_000_000_000L;

        /// <summary>每帧预留给投射物的 ID 段长度。超过只告警，不会与下一帧重叠。</summary>
        public const long ProjectileIdFrameStride = 1024L;

        public BattlePhase Phase => BattlePhase.Timeline;

        /// <summary>必须大于 CombatTimelineSystem.Order，否则读不到本帧的生成事件。</summary>
        public int Order => 10;

        public void Tick(BattleWorld world, long frame)
        {
            RecycleDeadProjectiles(world, frame);

            var events = world.FrameEvents;
            var spawnIndex = 0L;
            for (var i = 0; i < events.Count; i++)
            {
                var set = events[i];
                if (!set.HasSpawnEvents)
                {
                    continue;
                }

                if (!world.TryGetEntity(set.EntityId, out var owner))
                {
                    continue;
                }

                if (owner is ICombatProjectile)
                {
                    // 第一版不让投射物再生成投射物（分裂 / 子母弹）：递归生成会让 ID 推导复杂化。
                    continue;
                }

                var runner = owner.Controller.ActionRunner;
                for (var e = 0; e < set.SpawnEvents.Count; e++)
                {
                    var frameEvent = set.SpawnEvents[e];
                    if (!runner.TryAcceptSpawn(frameEvent.ActionInstanceId, frameEvent.EventId))
                    {
                        continue;
                    }

                    var profile = frameEvent.SpawnProfile;
                    if (profile == null || profile.FlightProfile == null || profile.FlightActionId <= 0)
                    {
                        Log.Error("[Spawn] 生成事件缺少可用的飞行配置，已跳过: " +
                                  $"frame={frame}, owner={set.EntityId}, event={frameEvent.EventId}");
                        continue;
                    }

                    if (spawnIndex >= ProjectileIdFrameStride)
                    {
                        Log.Error($"[Spawn] 单帧生成数超过 ID 预留段: frame={frame}, limit={ProjectileIdFrameStride}");
                        break;
                    }

                    var id = ProjectileIdBase + frame * ProjectileIdFrameStride + spawnIndex++;
                    try
                    {
                        var projectile = new CombatProjectile(id, profile, owner.Position, owner.Rotation, frame, owner.Id);
                        if (world.Register(projectile) && Verbose)
                        {
                            Log.Debug($"[Spawn] frame={frame} owner={set.EntityId} → projectile={id} " +
                                      $"action={profile.FlightActionId} 速度={profile.SpeedMillimetersPerFrame}mm/帧 " +
                                      $"寿命={profile.MaxLifetimeFrames}帧");
                        }
                    }
                    catch (Exception exception)
                    {
                        // 配置错误不该炸掉整个战斗循环：报告后跳过这一条，其余事件继续。
                        Log.Error($"[Spawn] 投射物创建失败，已跳过: spawn={profile.name}, {exception.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 回收上一帧判死的投射物。找不到就返回 false，所以重复调用是安全的。
        /// 遍历的是本帧的遍历快照，注销不会影响这次循环。
        /// </summary>
        private static void RecycleDeadProjectiles(BattleWorld world, long frame)
        {
            var entities = world.OrderedEntities;
            for (var i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                if (entity is not ICombatProjectile)
                {
                    continue;
                }
                if (!entity.IsCombatActive || entity.Controller.StateMachine.Life == LifeState.Alive)
                {
                    continue;
                }

                // 必须在同一帧遍历快照内注销：_ordered 是 Tick 开头的快照，
                // 注销只改 _entities 与 dirty 标记，本帧后续阶段继续用旧快照是安全的。
                if (world.Unregister(entity.Id) && Verbose)
                {
                    Log.Debug($"[Spawn] frame={frame} 回收投射物 {entity.Id}");
                }
            }
        }
    }
}
