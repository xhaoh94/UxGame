using System;
using UnityEngine;

namespace Ux
{
    /// <summary>
    /// 客户端技能表现映射。权威技能 ID 和逻辑帧数据只保存在 CombatActionAsset；
    /// 本类型仅把角色 Profile 中的逻辑技能关联到客户端 Timeline。
    /// </summary>
    [Serializable]
    public sealed class CombatActionPresentation
    {
        [SerializeField] private CombatActionAsset action;
        [SerializeField] private TimelineAsset timeline;

        public CombatActionPresentation()
        {
        }

        public CombatActionPresentation(CombatActionAsset action, TimelineAsset timeline)
        {
            this.action = action;
            this.timeline = timeline;
        }

        public CombatActionAsset Action => action;
        public TimelineAsset Timeline => timeline;

        public bool Matches(CombatActionAsset target)
        {
            return action == target;
        }
    }
}
