using System;
using System.Collections.Generic;

namespace Ux
{
    /// <summary>单个增益的快照。只包含可复制的纯数据，不含任何容器引用。</summary>
    [Serializable]
    public struct CombatBuffSnapshot
    {
        public int BuffId;
        public int RemainingFrames;
        public int DamagePerTick;
        public long AppliedFrame;
    }

    /// <summary>
    /// 一条正在生效的增益 —— ⚠ 最小版本（设计文档 4.5 的修改器栈/叠层/驱散属于 P4）。
    /// 做成一类可变对象 + 池化复用，避免每帧分配。DamagePerTick 为 0 表示纯标记型增益。
    /// </summary>
    public sealed class CombatBuff
    {
        public int BuffId { get; private set; }
        public int RemainingFrames { get; private set; }
        public int DamagePerTick { get; private set; }
        public long AppliedFrame { get; private set; }

        /// <summary>是否已到期。由容器每帧递减，增益阶段负责移除。</summary>
        public bool IsExpired => RemainingFrames <= 0;

        internal void Reset(int buffId, int durationFrames, int damagePerTick, long frame)
        {
            BuffId = buffId;
            RemainingFrames = Math.Max(1, durationFrames);
            DamagePerTick = Math.Max(0, damagePerTick);
            AppliedFrame = frame;
        }

        internal int TickDown()
        {
            RemainingFrames--;
            return RemainingFrames;
        }
    }

    /// <summary>
    /// 增益容器，挂在 CombatController 上，每个单位一份。只存不管结算——周期扣血与到期移除在增益阶段。
    ///
    /// 顺序即规则：遍历顺序 = 施加顺序，移除用有序删除（不是把队尾换过来），
    /// 保证同一状态下重放两遍的周期伤害先后与数值完全一致。
    /// </summary>
    public sealed class CombatBuffContainer
    {
        private readonly List<CombatBuff> _active = new();
        private readonly List<CombatBuff> _pool = new();

        public int Count => _active.Count;

        public CombatBuff this[int index] => _active[index];

        /// <summary>施加一条增益。当前不做叠层与去重，同一 BuffId 可挂多条独立倒计时。</summary>
        public CombatBuff Apply(int buffId, int durationFrames, int damagePerTick, long frame)
        {
            var buff = _pool.Count > 0 ? _pool[_pool.Count - 1] : null;
            if (buff == null)
            {
                buff = new CombatBuff();
            }
            else
            {
                _pool.RemoveAt(_pool.Count - 1);
            }

            buff.Reset(buffId, durationFrames, damagePerTick, frame);
            _active.Add(buff);
            return buff;
        }

        /// <summary>按索引移除并回收实例。调用方负责保证索引有效。</summary>
        public void RemoveAt(int index)
        {
            var buff = _active[index];
            _active.RemoveAt(index);
            _pool.Add(buff);
        }

        public void Clear()
        {
            for (var i = 0; i < _active.Count; i++)
            {
                _pool.Add(_active[i]);
            }
            _active.Clear();
        }

        public CombatBuffSnapshot[] CaptureSnapshot()
        {
            if (_active.Count == 0)
            {
                return Array.Empty<CombatBuffSnapshot>();
            }

            var result = new CombatBuffSnapshot[_active.Count];
            for (var i = 0; i < _active.Count; i++)
            {
                var buff = _active[i];
                result[i] = new CombatBuffSnapshot
                {
                    BuffId = buff.BuffId,
                    RemainingFrames = buff.RemainingFrames,
                    DamagePerTick = buff.DamagePerTick,
                    AppliedFrame = buff.AppliedFrame,
                };
            }
            return result;
        }

        public void RestoreSnapshot(CombatBuffSnapshot[] snapshot)
        {
            Clear();
            if (snapshot == null)
            {
                return;
            }

            for (var i = 0; i < snapshot.Length; i++)
            {
                var entry = snapshot[i];
                if (entry.BuffId <= 0 || entry.RemainingFrames <= 0)
                {
                    continue;
                }
                Apply(entry.BuffId, entry.RemainingFrames, entry.DamagePerTick, entry.AppliedFrame);
            }
        }
    }
}
