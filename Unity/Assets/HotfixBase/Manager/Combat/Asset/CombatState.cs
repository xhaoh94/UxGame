using System;
using System.Collections.Generic;

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

        /// <summary>
        /// 可作为“状态表现”映射候选的状态集合，从对应枚举反射生成（保持枚举定义顺序）。
        /// 新增状态只需在枚举里加值，编辑器下拉与“添加默认映射”会自动包含，无需再维护手写列表。
        /// 只排除结构性错误项：
        /// - Action 层整层禁止（由技能列表管理，ValidateRuntime 会拒绝）；
        /// - Control.Normal、Life.Alive 若配置会因 ResolveTimelineOwner 的层级优先级被优先命中，
        ///   从而永远盖掉下层动作/移动表现，故不允许映射。
        /// </summary>
        public static int[] GetMappableStateIds(StateLayer layer)
        {
            var type = GetStateEnumType(layer);
            if (type == null)
            {
                return Array.Empty<int>();
            }

            var result = new List<int>();
            foreach (var value in Enum.GetValues(type))
            {
                var stateId = Convert.ToInt32(value);
                if (IsStatePresentationMappable(layer, stateId))
                {
                    result.Add(stateId);
                }
            }
            return result.ToArray();
        }

        /// <summary>判断一个状态值是否能作为“状态表现”映射目标（结构允许且枚举已定义）。</summary>
        public static bool IsStatePresentationMappable(StateLayer layer, int stateId)
        {
            if (layer == StateLayer.Action)
            {
                return false;
            }
            if (layer == StateLayer.Life && stateId == (int)LifeState.Alive)
            {
                return false;
            }
            if (layer == StateLayer.Control && stateId == (int)ControlState.Normal)
            {
                return false;
            }
            return IsDefined(layer, stateId);
        }

        private static Type GetStateEnumType(StateLayer layer)
        {
            return layer switch
            {
                StateLayer.Locomotion => typeof(LocomotionState),
                StateLayer.Action => typeof(ActionState),
                StateLayer.Control => typeof(ControlState),
                StateLayer.Life => typeof(LifeState),
                _ => null,
            };
        }
    }
}
