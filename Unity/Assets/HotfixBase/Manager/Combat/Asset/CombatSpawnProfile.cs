using System;
using UnityEngine;

namespace Ux
{
    /// <summary>生成物飞行过程中的目标选择方式。</summary>
    public enum SpawnHomingMode : byte
    {
        /// <summary>朝生成瞬间的朝向直飞，之后不再转向。</summary>
        Straight = 0,

        /// <summary>生成那一帧选定目标，之后飞向目标当时的位置。目标消失后按最后位置直飞。</summary>
        LockOnSpawn = 1,

        /// <summary>每帧朝目标当前位置修正朝向。目标消失后转为直飞。</summary>
        TrackTarget = 2,
    }

    /// <summary>
    /// 生成物的逻辑配置：怎么飞、飞多久、用哪个动作描述自己的命中与伤害。
    ///
    /// 刻意不含任何渲染资源 —— 视觉由表现侧的 CombatSpawnPresentation 按本资产查找，
    /// 这样逻辑程序集不会被拉进表现资源树，战报重放仍可在无渲染环境下跑。
    /// </summary>
    [CreateAssetMenu(fileName = "CombatSpawn", menuName = "UxGame/战斗/生成物")]
    public sealed class CombatSpawnProfile : ScriptableObject
    {
        [SerializeField] private string stableId = string.Empty;
        [SerializeField, Min(1)] private int spawnId = 1;
        [SerializeField] private string displayName = "新生成物";
        [SerializeField] private CharacterCombatProfile flightProfile;
        [SerializeField, Min(1)] private int speedMillimetersPerFrame = 20;
        [SerializeField] private SpawnHomingMode homingMode = SpawnHomingMode.Straight;
        [SerializeField, Min(1)] private int maxLifetimeFrames = 120;

        public string StableId => stableId;
        public int SpawnId => spawnId;
        public string DisplayName => displayName;

        /// <summary>
        /// 生成物自己的战斗配置：只应包含一个"飞行"动作，攻击判定、伤害、持续时间都由它描述。
        /// 复用角色配置类型，是为了让投射物能直接走 CombatController 与整套命中／伤害链路。
        /// </summary>
        public CharacterCombatProfile FlightProfile => flightProfile;

        /// <summary>投射物起手的动作 ID —— 取飞行配置里的第一个动作。</summary>
        public int FlightActionId =>
            flightProfile != null && flightProfile.Actions.Count > 0
                ? flightProfile.Actions[0].ActionId
                : 0;

        /// <summary>飞行速度，单位毫米/逻辑帧（与命中半径同一单位域）。</summary>
        public int SpeedMillimetersPerFrame => Mathf.Max(1, speedMillimetersPerFrame);

        public SpawnHomingMode HomingMode => homingMode;

        /// <summary>没打中任何目标时的存活上限，防止生成物永久留在世界里。</summary>
        public int MaxLifetimeFrames => Mathf.Max(1, maxLifetimeFrames);

        public void ValidateData()
        {
            if (string.IsNullOrEmpty(stableId))
            {
                stableId = Guid.NewGuid().ToString("N");
            }
            spawnId = Mathf.Max(1, spawnId);
            displayName ??= string.Empty;
            speedMillimetersPerFrame = Mathf.Max(1, speedMillimetersPerFrame);
            maxLifetimeFrames = Mathf.Max(1, maxLifetimeFrames);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(stableId))
            {
                stableId = Guid.NewGuid().ToString("N");
            }
            displayName ??= string.Empty;
        }
#endif
    }
}
