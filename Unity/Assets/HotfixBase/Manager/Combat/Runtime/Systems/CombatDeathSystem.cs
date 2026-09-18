namespace Ux
{
    /// <summary>
    /// 阶段 Death 插件：逻辑帧的收口。凡"还活着但血已打空"的判为死亡，并清空增益。
    /// SetLife 内部会打断动作、把状态压回 Idle，所以死亡时该清什么不用在这里重写。
    ///
    /// 必须是最后一个逻辑阶段：排在伤害与增益之后，同帧"瞬间打空"和"持续伤害耗死"才能一次判完。
    /// ⚠ 最小实现：判据只有 HP ≤ 0，没有濒死、无敌帧豁免、死亡动画时序。
    /// </summary>
    public sealed class CombatDeathSystem : ICombatSystem
    {
        /// <summary>打开后输出死亡判定日志。只影响日志，不影响模拟结果。</summary>
        public static bool Verbose = true;

        public BattlePhase Phase => BattlePhase.Death;

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

                var controller = entity.Controller;
                if (controller.States.Life != LifeState.Alive)
                {
                    continue;
                }

                var attributes = controller.Attributes;
                if (!attributes.IsInitialized || !attributes.IsDepleted)
                {
                    // 未初始化的单位不会被误判：IsDepleted 在未初始化时恒为 false。
                    continue;
                }

                controller.SetLife(LifeState.Dead);
                controller.Buffs.Clear();

                if (Verbose)
                {
                    Log.Debug($"[Death] frame={frame} {entity.Id} 死亡判定成立 " +
                              $"HP={attributes.Hp}/{attributes.MaxHp}");
                }
            }
        }
    }
}
