using System;

namespace Ux
{
    /// <summary>战斗宏观状态层。具体攻击和技能不作为状态节点，而由 CombatActionRunner 管理。</summary>
    public enum StateLayer : byte
    {
        Locomotion = 0,// 行走层，用于控制战斗行走状态层
        Action = 1,// 动作层，用于控制战斗动作状态层
        Control = 2,// 控制层，用于控制战斗宏观状态层
        Life = 3,// 生命层，用于控制战斗生命状态层
    }

    public enum LocomotionState : byte
    {
        Idle = 1,
        Move = 2,
        Airborne = 3,
    }

    public enum ActionState : byte
    {
        Free = 1,
        Executing = 2,
    }

    public enum ControlState : byte
    {
        Normal = 1,
        Stunned = 2,
        Knockback = 3,
        Frozen = 4,
    }

    public enum LifeState : byte
    {
        Alive = 1,
        Dead = 2,
    }

    public enum CombatCommandType : byte
    {
        None,
        Attack,
        Skill01,
        Skill02,
        Skill03,
        Dodge,
        Jump,
    }

    public enum ActionMovementPolicy : byte
    {
        Allow,
        Block,
    }

    public enum CombatActionEndReason : byte
    {
        Started,
        Completed,
        Cancelled,
        Interrupted,
        Rejected,
        SnapshotRestore,
        Release,
    }

    public static class CombatStateId
    {
        public static int ToId(this LocomotionState state) => (int)state;
        public static int ToId(this ActionState state) => (int)state;
        public static int ToId(this ControlState state) => (int)state;
        public static int ToId(this LifeState state) => (int)state;

        public static string GetDisplayName(StateLayer layer, int stateId)
        {
            return layer switch
            {
                StateLayer.Locomotion => ((LocomotionState)stateId).ToString(),
                StateLayer.Action => ((ActionState)stateId).ToString(),
                StateLayer.Control => ((ControlState)stateId).ToString(),
                StateLayer.Life => ((LifeState)stateId).ToString(),
                _ => stateId.ToString(),
            };
        }

        public static bool IsDefined(StateLayer layer, int stateId)
        {
            if (stateId < byte.MinValue || stateId > byte.MaxValue)
            {
                return false;
            }
            return layer switch
            {
                StateLayer.Locomotion => Enum.IsDefined(
                    typeof(LocomotionState),
                    (LocomotionState)stateId),
                StateLayer.Action => Enum.IsDefined(
                    typeof(ActionState),
                    (ActionState)stateId),
                StateLayer.Control => Enum.IsDefined(
                    typeof(ControlState),
                    (ControlState)stateId),
                StateLayer.Life => Enum.IsDefined(
                    typeof(LifeState),
                    (LifeState)stateId),
                _ => false,
            };
        }
    }
}
