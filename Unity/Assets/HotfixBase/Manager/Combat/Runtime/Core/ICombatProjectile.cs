namespace Ux
{
    /// <summary>
    /// 投射物标记。命中判定需要区分"能出招的单位"与"被打出来的东西"：
    /// 后者没有阵营、也不该被刀光选中，但它自己仍要出招（飞行）并判定命中。
    /// </summary>
    public interface ICombatProjectile
    {
        /// <summary>生成物自己的逻辑配置，表现层据此解析视觉映射。</summary>
        CombatSpawnProfile Spawn { get; }

        /// <summary>是否可以被其它攻击选中。默认不参与索敌。</summary>
        bool CanBeTargeted { get; }

        /// <summary>发射者实体 Id。命中判定用它豁免发射者自己，否则弹体在脚下生成会当场打中发射者。</summary>
        long SpawnerId { get; }
    }
}
