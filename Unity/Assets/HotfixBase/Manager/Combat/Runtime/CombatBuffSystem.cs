namespace Ux
{
    /// <summary>
    /// 阶段 Buff 插件：推进每个单位身上的增益——结算周期扣血、寿命 -1、到期移除并回收实例。
    ///
    /// 排在伤害之后（读本帧最终血量）、死亡之前（持续伤害致死能在本帧判出来）。
    /// ⚠ 最小实现：增益只有身份/寿命/每帧扣血，没有叠层、修改器栈、来源追踪、驱散。
    /// </summary>
    public sealed class CombatBuffSystem : ICombatSystem
    {
        /// <summary>打开后输出增益结算与到期移除的日志。</summary>
        public static bool Verbose;

        public BattlePhase Phase => BattlePhase.Buff;

        public int Order => 0;

        public void Tick(BattleWorld world, long frame)
        {
            // 按索引遍历，不用 foreach world.Entities（接口枚举器会装箱，每帧一个堆对象）。
            var entities = world.OrderedEntities;
            for (var i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                if (!entity.IsCombatActive)
                {
                    continue;
                }

                var buffs = entity.Controller.Buffs;
                if (buffs.Count == 0)
                {
                    continue;
                }

                var attributes = entity.Controller.Attributes;

                // while + 手动推进下标而不是 for：移除时下标不动，下一轮检查补位上来的那一条，
                // 这样结算顺序严格等于施加顺序。
                var index = 0;
                while (index < buffs.Count)
                {
                    var buff = buffs[index];

                    if (buff.DamagePerTick > 0 && !attributes.IsDepleted)
                    {
                        var applied = attributes.ApplyDamage(buff.DamagePerTick);
                        if (Verbose && applied > 0)
                        {
                            Log.Debug($"[Buff] frame={frame} {entity.Id} buff={buff.BuffId} " +
                                      $"周期扣血={applied} 剩余HP={attributes.Hp}/{attributes.MaxHp}");
                        }
                    }

                    buff.TickDown();

                    if (buff.IsExpired)
                    {
                        if (Verbose)
                        {
                            Log.Debug($"[Buff] frame={frame} {entity.Id} buff={buff.BuffId} 到期移除");
                        }

                        buffs.RemoveAt(index);
                        continue;
                    }

                    index++;
                }
            }
        }
    }
}
