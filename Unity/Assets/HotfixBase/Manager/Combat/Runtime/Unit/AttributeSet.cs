using System;

namespace Ux
{
    /// <summary>属性快照。跟其它快照一样必须是可复制的纯数据。</summary>
    [Serializable]
    public struct UnitAttributeSnapshot
    {
        public int MaxHp;
        public int Hp;
    }

    /// <summary>
    /// 单位属性集 —— ⚠ 最小版本，当前只有生命值（设计文档 4.3 的修改器栈属于 P1/P4）。
    /// 全整数运算，无随机无浮点无时间依赖，可以算进状态哈希。
    /// </summary>
    public sealed class AttributeSet
    {
        public bool IsInitialized { get; private set; }

        public int MaxHp { get; private set; }

        public int Hp { get; private set; }

        /// <summary>未初始化时返回 false —— 否则未初始化的单位会被死亡阶段一跑就全判死。</summary>
        public bool IsDepleted => IsInitialized && Hp <= 0;

        public void Initialize(int maxHp)
        {
            MaxHp = Math.Max(1, maxHp);
            Hp = MaxHp;
            IsInitialized = true;
        }

        /// <summary>扣血，返回实际扣掉的量（夹到 0，不会出现负数 HP）。逻辑判定只看 IsDepleted。</summary>
        public int ApplyDamage(int amount)
        {
            if (!IsInitialized || amount <= 0 || Hp <= 0)
            {
                return 0;
            }

            var applied = Math.Min(Hp, amount);
            Hp -= applied;
            return applied;
        }

        /// <summary>回血，返回实际回复的量（不会超过上限）。已经死了就不再接受治疗，复活走显式流程。</summary>
        public int ApplyHeal(int amount)
        {
            if (!IsInitialized || amount <= 0 || Hp <= 0)
            {
                return 0;
            }

            var applied = Math.Min(MaxHp - Hp, amount);
            Hp += applied;
            return applied;
        }

        public UnitAttributeSnapshot CaptureSnapshot()
        {
            return new UnitAttributeSnapshot
            {
                MaxHp = MaxHp,
                Hp = Hp,
            };
        }

        public void RestoreSnapshot(in UnitAttributeSnapshot snapshot)
        {
            MaxHp = Math.Max(1, snapshot.MaxHp);
            Hp = Math.Clamp(snapshot.Hp, 0, MaxHp);
            IsInitialized = true;
        }

        public void Release()
        {
            MaxHp = 0;
            Hp = 0;
            IsInitialized = false;
        }
    }
}
