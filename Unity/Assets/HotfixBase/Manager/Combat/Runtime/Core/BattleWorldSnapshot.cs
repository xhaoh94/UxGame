using System;
using UnityEngine;

namespace Ux
{
    /// <summary>
    /// 单个单位的世界级快照。除了状态机与动作，还带上位置与朝向，
    /// 否则回滚后单位会站在错误的位置上继续打。
    /// </summary>
    [Serializable]
    public struct EntityCombatSnapshot
    {
        public long Id;
        public UnitCombatSnapshot Combat;
        public Vector3 Position;
        public Quaternion Rotation;
    }

    [Serializable]
    public sealed class BattleWorldSnapshot
    {
        public long Frame;
        public EntityCombatSnapshot[] Entities;

        public BattleWorldSnapshot(long frame, EntityCombatSnapshot[] entities)
        {
            Frame = Math.Max(0, frame);
            Entities = entities ?? Array.Empty<EntityCombatSnapshot>();
        }
    }
}
