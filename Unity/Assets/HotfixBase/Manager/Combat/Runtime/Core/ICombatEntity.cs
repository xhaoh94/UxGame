using UnityEngine;

namespace Ux
{
    /// <summary>
    /// BattleWorld 能识别的最小战斗单位抽象。刻意不依赖 Unit / GameObject，
    /// 这样同一套世界循环可以跑在战报校验、服务器重放这类无渲染环境里。
    /// 实现方必须保证所有返回值只取决于逻辑帧输入，不得读取实时输入、时间与物理。
    /// </summary>
    public interface ICombatEntity
    {
        /// <summary>
        /// 稳定且唯一的排序 ID。世界按它升序遍历，这是确定性的前提，
        /// 因此不能用 GameObject 实例 ID 这类每次运行都会变的值。
        /// </summary>
        long Id { get; }

        /// <summary>是否参与本帧模拟。未初始化完成的单位会被跳过而不是报错。</summary>
        bool IsCombatActive { get; }

        CombatController Controller { get; }

        /// <summary>本帧移动输入，取值必须是逻辑帧内的固定采样。</summary>
        Vector2 MoveInput { get; }

        Vector3 Position { get; set; }

        Quaternion Rotation { get; set; }

        /// <summary>阶段 Commands：取出本帧命令。</summary>
        CombatFrameCommands ConsumeCommands(long frame);

        /// <summary>阶段 Actions：推进状态机、动作生命周期与位移。</summary>
        void TickLogic(long frame, in CombatFrameCommands commands);

        /// <summary>阶段 Presentation：推进表现。无渲染环境下由实现自行跳过。</summary>
        void TickPresentation(long frame);
    }
}
