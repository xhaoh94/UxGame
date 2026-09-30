using System;
using UnityEngine;

namespace Ux
{
    /// <summary>
    /// 动作中的离散帧事件。与 CombatLogicWindow 的区间语义不同，事件只在 Frame 这一帧成立一次。
    /// 事件集合使用 SerializeReference，后续可以添加音效、特效、换招等事件类型，而不必为每种事件造一条伪窗口轨。
    /// </summary>
    [Serializable]
    public abstract class CombatFrameEvent
    {
        [SerializeField, HideInInspector] private string stableId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [Min(0)] public int Frame;

        public string StableId => stableId;
        public string DisplayName => displayName;
        public virtual string DefaultDisplayName => "帧事件";

        public virtual void ValidateData()
        {
            if (string.IsNullOrEmpty(stableId))
            {
                RegenerateStableId();
            }
            displayName ??= string.Empty;
            Frame = Mathf.Max(0, Frame);
        }

        /// <summary>运行时校验事件的业务字段。默认事件没有额外约束。</summary>
        public virtual void ValidateRuntime(CombatActionAsset action)
        {
        }

        internal void RegenerateStableId()
        {
            stableId = Guid.NewGuid().ToString("N");
        }
    }

    /// <summary>生成物事件：在动作的单个逻辑帧生成一个投射物。</summary>
    [Serializable]
    public sealed class ActionSpawnEvent : CombatFrameEvent
    {
        [SerializeField] private CombatSpawnProfile spawnProfile;

        public CombatSpawnProfile SpawnProfile => spawnProfile;
        public override string DefaultDisplayName => "生成投射物";

        public override void ValidateRuntime(CombatActionAsset action)
        {
            if (spawnProfile == null)
            {
                throw new InvalidOperationException(
                    $"生成事件未配置生成物: action={action?.name}, event={StableId}");
            }
        }
    }
}
