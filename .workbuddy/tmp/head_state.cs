using System;
using System.Collections.Generic;

namespace Ux
{
    /// <summary>鎴樻枟瀹忚鐘舵€佸眰銆傚叿浣撴敾鍑诲拰鎶€鑳戒笉浣滀负鐘舵€佽妭鐐癸紝鑰岀敱 CombatActionRunner 绠＄悊銆?/summary>
    public enum StateLayer : byte
    {
        Locomotion = 0,// 琛岃蛋灞傦紝鐢ㄤ簬鎺у埗鎴樻枟琛岃蛋鐘舵€佸眰
        Action = 1,// 鍔ㄤ綔灞傦紝鐢ㄤ簬鎺у埗鎴樻枟鍔ㄤ綔鐘舵€佸眰
        Control = 2,// 鎺у埗灞傦紝鐢ㄤ簬鎺у埗鎴樻枟瀹忚鐘舵€佸眰
        Life = 3,// 鐢熷懡灞傦紝鐢ㄤ簬鎺у埗鎴樻枟鐢熷懡鐘舵€佸眰
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
        /// 鍙綔涓衡€滅姸鎬佽〃鐜扳€濇槧灏勫€欓€夌殑鐘舵€侀泦鍚堬紝浠庡搴旀灇涓惧弽灏勭敓鎴愶紙淇濇寔鏋氫妇瀹氫箟椤哄簭锛夈€?        /// 鏂板鐘舵€佸彧闇€鍦ㄦ灇涓鹃噷鍔犲€硷紝缂栬緫鍣ㄤ笅鎷変笌鈥滄坊鍔犻粯璁ゆ槧灏勨€濅細鑷姩鍖呭惈锛屾棤闇€鍐嶇淮鎶ゆ墜鍐欏垪琛ㄣ€?        /// 鍙帓闄ょ粨鏋勬€ч敊璇」锛?        /// - Action 灞傛暣灞傜姝紙鐢辨妧鑳藉垪琛ㄧ鐞嗭紝ValidateRuntime 浼氭嫆缁濓級锛?        /// - Control.Normal銆丩ife.Alive 鑻ラ厤缃細鍥?ResolveTimelineOwner 鐨勫眰绾т紭鍏堢骇琚紭鍏堝懡涓紝
        ///   浠庤€屾案杩滅洊鎺変笅灞傚姩浣?绉诲姩琛ㄧ幇锛屾晠涓嶅厑璁告槧灏勩€?        /// </summary>
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

        /// <summary>鍒ゆ柇涓€涓姸鎬佸€兼槸鍚﹁兘浣滀负鈥滅姸鎬佽〃鐜扳€濇槧灏勭洰鏍囷紙缁撴瀯鍏佽涓旀灇涓惧凡瀹氫箟锛夈€?/summary>
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
