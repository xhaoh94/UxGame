using System.Collections.Generic;

namespace Ux
{
    /// <summary>本帧开放的一条取消窗口快照。只复制可序列化参数，不持有 CombatActionAsset 引用。</summary>
    public readonly struct CombatActiveCancelWindow
    {
        public CombatActiveCancelWindow(
            long actionInstanceId,
            int actionId,
            int actionFrame,
            int windowIndex,
            ActionCancelWindow window)
        {
            ActionInstanceId = actionInstanceId;
            ActionId = actionId;
            ActionFrame = actionFrame;
            WindowIndex = windowIndex;
            WindowId = window?.StableId ?? string.Empty;
            StartFrame = window?.StartFrame ?? 0;
            EndFrame = window?.EndFrame ?? 0;
            TargetActionId = window?.TargetActionId ?? 0;
            RequiresHitConfirm = window?.RequiresHitConfirm ?? false;
        }

        public long ActionInstanceId { get; }
        public int ActionId { get; }
        public int ActionFrame { get; }
        public int WindowIndex { get; }
        public string WindowId { get; }
        public int StartFrame { get; }
        public int EndFrame { get; }

        /// <summary>允许取消成哪个动作。0 表示没有指向任何动作。</summary>
        public int TargetActionId { get; }

        public bool RequiresHitConfirm { get; }
    }

    /// <summary>
    /// 单个单位在一个逻辑帧内的帧事件集合，Timeline 阶段的产物、后续阶段的输入。
    ///
    /// 可变对象 + 池化复用（表项内含 List，做成 struct 会陷进引用共享），每帧 Reset 后填充、之后只读。
    /// 只放"按帧区间成立"的东西；单位自身状态（位置、HP）不在这里。
    /// </summary>
    public sealed class CombatFrameEventSet
    {
        public long EntityId { get; private set; }
        public long ActionInstanceId { get; private set; }
        public int ActionId { get; private set; }
        public int ActionFrame { get; private set; }

        /// <summary>本帧该动作是否已确认命中，取消窗口的 RequiresHitConfirm 靠它判定。</summary>
        public bool HasHitConfirmed { get; private set; }

        /// <summary>本帧处于激活区间的命中窗口，顺序与资产配置顺序一致。</summary>
        public List<CombatActiveHitWindow> HitWindows { get; } = new();

        /// <summary>本帧处于开放区间的取消窗口，顺序与资产配置顺序一致。</summary>
        public List<CombatActiveCancelWindow> CancelWindows { get; } = new();

        public bool HasHitWindows => HitWindows.Count > 0;

        public bool HasCancelWindows => CancelWindows.Count > 0;

        internal void Reset(long entityId, in CombatActionSnapshot action, bool hasHitConfirmed)
        {
            EntityId = entityId;
            ActionInstanceId = action.InstanceId;
            ActionId = action.ActionId;
            ActionFrame = action.ActionFrame;
            HasHitConfirmed = hasHitConfirmed;
            HitWindows.Clear();
            CancelWindows.Clear();
        }
    }

    /// <summary>
    /// 一个世界的「本帧帧事件表」：阶段之间唯一的交接方式，插件互不持有引用。
    /// Timeline 阶段写，Hitbox / Damage / Buff 读。
    ///
    /// 每逻辑帧开头由 BattleWorld 调 BeginFrame 清空，所以读到的永远是本帧结果、不会残留上一帧 ——
    /// 即使 Timeline 插件没注册，读到的也只是空表而不是脏数据。
    /// </summary>
    public sealed class CombatFrameEventTable
    {
        private readonly List<CombatFrameEventSet> _pool = new();
        private int _count;
        private long _frame = -1;

        public long Frame => _frame;

        public int Count => _count;

        public CombatFrameEventSet this[int index] => _pool[index];

        /// <summary>本帧开始：标记表为空，池里的表项留作复用。</summary>
        public void BeginFrame(long frame)
        {
            _frame = frame;
            _count = 0;
        }

        /// <summary>Timeline 阶段为某个单位追加一组帧事件，返回可写的表项。</summary>
        public CombatFrameEventSet Append(long entityId, in CombatActionSnapshot action, bool hasHitConfirmed)
        {
            if (_count == _pool.Count)
            {
                _pool.Add(new CombatFrameEventSet());
            }

            var set = _pool[_count++];
            set.Reset(entityId, action, hasHitConfirmed);
            return set;
        }

        /// <summary>按单位 ID 查找本帧帧事件。表项数等于"本帧出招的单位数"，比全单位少，线性扫足够。</summary>
        public bool TryFind(long entityId, out CombatFrameEventSet set)
        {
            for (var i = 0; i < _count; i++)
            {
                if (_pool[i].EntityId == entityId)
                {
                    set = _pool[i];
                    return true;
                }
            }

            set = null;
            return false;
        }
    }

    /// <summary>
    /// 待结算命中缓冲：阶段 Hitbox 写，阶段 Damage 消费。
    /// 与帧事件表同一套交接思路，区别只在形状——命中候选是扁平记录，所以是个可复用列表。
    /// 写入顺序即结算顺序，不依赖任何字典遍历序。
    /// </summary>
    public sealed class CombatHitBuffer
    {
        private readonly List<CombatHitCandidate> _hits = new();
        private long _frame = -1;

        public long Frame => _frame;

        public int Count => _hits.Count;

        public CombatHitCandidate this[int index] => _hits[index];

        public void BeginFrame(long frame)
        {
            _frame = frame;
            _hits.Clear();
        }

        public void Add(in CombatHitCandidate hit)
        {
            _hits.Add(hit);
        }

        public void Clear()
        {
            _frame = -1;
            _hits.Clear();
        }
    }
}
