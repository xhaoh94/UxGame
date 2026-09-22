using System;
using System.Collections.Generic;

namespace Ux
{
    /// <summary>战斗宏观状态层。具体攻击和技能不作为状态节点，而由 CombatActionRunner 管理。</summary>
    public enum StateLayer : byte
    {
        Locomotion = 0,// 行走层，用于控制战斗行走状态层
        Control = 1, // 控制层，用于控制战斗宏观状态层
        Life = 2,// 生命层，用于控制战斗生命状态层
    }

    public enum LocomotionState : byte
    {
        Idle = 1,
        Move = 2,
        Airborne = 3,
    }

    public enum ControlState : byte
    {
        Normal = 1,
        Stunned = 2, //眩晕
        Knockback = 3,//击退
        Frozen = 4,//冰冻
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

    /// <summary>动作结束的原因。用于判定是否要触发动作结束事件。</summary>
    public enum CombatActionEndReason : byte
    {
        Started,//动作刚开始就被打断了
        Completed,//动作正常完成
        Cancelled,//动作被取消了
        Interrupted,//动作被中断了（比如被击退、眩晕等）
        Rejected,//动作被拒绝了（比如被控制、死亡等）
        SnapshotRestore,//动作快照恢复时，原本的动作被替换了
        Release,//动作被释放了（比如被释放了技能、死亡等）
    }

    public static class CombatStateId
    {
        public static int ToId(this LocomotionState state) => (int)state;
        public static int ToId(this ControlState state) => (int)state;
        public static int ToId(this LifeState state) => (int)state;

        public static string GetDisplayName(StateLayer layer, int stateId)
        {
            return layer switch
            {
                StateLayer.Locomotion => ((LocomotionState)stateId).ToString(),
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
        /// - Control.Normal、Life.Alive 若配置会因 CombatTimelineResolver.Resolve 的层级优先级被优先命中，
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
                StateLayer.Control => typeof(ControlState),
                StateLayer.Life => typeof(LifeState),
                _ => null,
            };
        }
    }
}
