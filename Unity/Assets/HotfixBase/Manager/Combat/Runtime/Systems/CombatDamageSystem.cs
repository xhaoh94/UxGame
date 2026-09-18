namespace Ux
{
    /// <summary>
    /// 阶段 Damage 插件：把 PendingHits 结算成血量变化——
    /// 给攻击方打命中确认标记、按动作配的固定伤害扣血、给目标挂上动作附带的增益。
    ///
    /// 排在 Hitbox 之后（否则没有输入）、Buff 与 Death 之前（它们要读扣完血的值）。
    /// ⚠ 最小实现：伤害是动作上的一个固定整数，没有修改器栈、暴击、减免、浮动区间。
    /// </summary>
    public sealed class CombatDamageSystem : ICombatSystem
    {
        /// <summary>打开后输出每次结算的详细数值。</summary>
        public static bool Verbose = true;

        public BattlePhase Phase => BattlePhase.Damage;

        public int Order => 0;

        public void Tick(BattleWorld world, long frame)
        {
            var pending = world.PendingHits;
            for (var i = 0; i < pending.Count; i++)
            {
                var hit = pending[i];

                if (!world.TryGetEntity(hit.SourceEntityId, out var source))
                {
                    continue;
                }

                var actions = source.Controller.Actions;

                // 与"扣没扣到血"无关：几何已经确认打到，即使目标 0 血、本次不再扣血，标记也要打上。
                actions.MarkHitConfirmed(hit.ActionInstanceId);

                if (!world.TryGetEntity(hit.TargetEntityId, out var target))
                {
                    continue;
                }

                if (target.Controller.States.Life != LifeState.Alive)
                {
                    // 同帧内先被打死的单位不会被重复结算。
                    continue;
                }

                var attributes = target.Controller.Attributes;
                if (attributes.IsDepleted)
                {
                    // HP 已归零但死亡阶段还没跑（它在后面）。
                    continue;
                }

                var action = actions.FindAction(hit.ActionId);
                if (action == null)
                {
                    Log.Warning($"[Damage] frame={frame} 动作不存在于攻击方动作表，跳过结算: " +
                                $"source={hit.SourceEntityId}, action={hit.ActionId}");
                    continue;
                }

                var applied = attributes.ApplyDamage(action.Damage);
                if (applied > 0)
                {
                    ApplyAttachedBuff(action, target, frame);
                }

                if (Verbose)
                {
                    Log.Debug($"[Damage] frame={frame} {hit.SourceEntityId} → {hit.TargetEntityId} " +
                              $"action={hit.ActionId} window={hit.WindowId} " +
                              $"damage={action.Damage} 实际扣血={applied} " +
                              $"剩余HP={attributes.Hp}/{attributes.MaxHp}");
                }
            }
        }

        /// <summary>把动作上配置的附带增益挂到目标身上。只负责挂，不负责怎么结算。</summary>
        private static void ApplyAttachedBuff(CombatActionAsset action, ICombatEntity target, long frame)
        {
            var apply = action.AppliedBuff;
            if (apply == null || !apply.IsEnabled)
            {
                return;
            }

            var buffs = target.Controller.Buffs;
            var buff = buffs.Apply(apply.BuffId, apply.DurationFrames, apply.DamagePerTick, frame);

            if (Verbose)
            {
                Log.Debug($"[Damage] frame={frame} 施加增益: target={target.Id} " +
                          $"buff={buff.BuffId} 持续={buff.RemainingFrames}帧 每帧扣血={buff.DamagePerTick}");
            }
        }
    }
}
