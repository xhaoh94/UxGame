namespace Ux
{
    /// <summary>
    /// 一个逻辑帧内固定的阶段顺序。顺序本身就是规则：
    /// 先采样命令，再推进动作，然后才允许命中查询和伤害结算，最后由死亡判定收口。
    /// 顺序写反会直接产出"已经死了还能还手"这类结果。
    /// </summary>
    public enum BattlePhase : byte
    {
        /// <summary>取出本帧命令。命令取走后不可重复消费。</summary>
        Commands = 0,

        /// <summary>推进宏观状态机与动作生命周期，并结算本帧位移。</summary>
        Actions = 1,

        /// <summary>Timeline 解析出当前帧的所有帧事件。供后面阶段使用。</summary>
        Timeline = 2,

        /// <summary>命中查询。由 BattleWorld 统一调度，不在 Clip 内各自结算。</summary>
        Hitbox = 3,

        /// <summary>伤害结算。</summary>
        Damage = 4,

        /// <summary>Buff tick。</summary>
        Buff = 5,

        /// <summary>死亡判定。放在伤害之后，避免同帧死亡单位继续行动。</summary>
        Death = 6,

        /// <summary>表现同步。逻辑全部结束后才推进动画与 Timeline。</summary>
        Presentation = 7,
    }

    /// <summary>
    /// 战斗子系统插件。P0 只在 BattleWorld 内置 Commands / Actions / Presentation 三个核心阶段，
    /// 其余阶段由后续需求以插件形式注册，避免把还没实现的内容写死进核心循环。
    /// 每个阶段内插件先执行，内置核心后执行。
    /// </summary>
    public interface ICombatSystem
    {
        BattlePhase Phase { get; }

        /// <summary>同阶段内的执行次序，小的先执行。相同 Order 按注册先后决定，保证全序。</summary>
        int Order { get; }

        void Tick(BattleWorld world, long frame);
    }
}
