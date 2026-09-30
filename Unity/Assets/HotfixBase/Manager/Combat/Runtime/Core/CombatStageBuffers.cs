using System.Collections.Generic;

namespace Ux
{
    /// <summary>本帧开放的一条动作窗口快照（可取消窗口或连招衔接）。只复制可序列化参数，不持有 CombatActionAsset 引用。</summary>
    public readonly struct CombatActiveActionWindow
    {
        public CombatActiveActionWindow(
            long actionInstanceId,
            int actionId,
            int actionFrame,
            int windowIndex,
            ActionTargetWindow window)
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

        /// <summary>允许切换到哪个动作。0 表示没有指向任何动作。</summary>
        public int TargetActionId { get; }

        public bool RequiresHitConfirm { get; }
    }

    /// <summary>本帧成立的一条离散生成事件快照。只复制事件位置、身份与生成物配置。</summary>
    public readonly struct CombatActiveSpawnEvent
    {
        public CombatActiveSpawnEvent(
            long actionInstanceId,
            int actionId,
            int actionFrame,
            int eventIndex,
            ActionSpawnEvent frameEvent)
        {
            ActionInstanceId = actionInstanceId;
            ActionId = actionId;
            ActionFrame = actionFrame;
            EventIndex = eventIndex;
            EventId = frameEvent?.StableId ?? string.Empty;
            Frame = frameEvent?.Frame ?? 0;
            SpawnProfile = frameEvent?.SpawnProfile;
        }

        public long ActionInstanceId { get; }
        public int ActionId { get; }
        public int ActionFrame { get; }
        public int EventIndex { get; }
        public string EventId { get; }
        public int Frame { get; }

        /// <summary>生成什么。为空表示事件配置不完整，消费方跳过。</summary>
        public CombatSpawnProfile SpawnProfile { get; }
    }

    /// <summary>
    /// 单个单位在一个逻辑帧内的帧事件集合，Timeline 阶段的产物、后续阶段的输入。
    ///
    /// 可变对象 + 池化复用（表项内含 List，做成 struct 会陷进引用共享），每帧 Reset 后填充、之后只读。
    /// 窗口记录"当前帧成立的区间事实"，事件记录"当前帧触发的离散事实"；单位自身状态（位置、HP）不在这里。
    /// </summary>
    public sealed class CombatFrameEventSet
    {
        public long EntityId { get; private set; }
        public long ActionInstanceId { get; private set; }
        public int ActionId { get; private set; }
        public int ActionFrame { get; private set; }

        /// <summary>本帧该动作是否已确认命中，动作窗口的 RequiresHitConfirm 靠它判定。</summary>
        public bool HasHitConfirmed { get; private set; }

        /// <summary>本帧处于激活区间的攻击判定，顺序与资产配置顺序一致。</summary>
        public List<CombatActiveHitboxWindow> HitboxWindows { get; } = new();

        /// <summary>本帧处于开放区间的动作窗口，顺序与资产配置顺序一致。</summary>
        public List<CombatActiveActionWindow> ActionWindows { get; } = new();

        /// <summary>本帧成立的离散生成事件，顺序与资产事件列表顺序一致。</summary>
        public List<CombatActiveSpawnEvent> SpawnEvents { get; } = new();

        public bool HasHitboxWindows => HitboxWindows.Count > 0;

        public bool HasActionWindows => ActionWindows.Count > 0;

        public bool HasSpawnEvents => SpawnEvents.Count > 0;

        internal void Reset(long entityId, in CombatActionSnapshot action, bool hasHitConfirmed)
        {
            EntityId = entityId;
            ActionInstanceId = action.InstanceId;
            ActionId = action.ActionId;
            ActionFrame = action.ActionFrame;
            HasHitConfirmed = hasHitConfirmed;
            HitboxWindows.Clear();
            ActionWindows.Clear();
            SpawnEvents.Clear();
        }
    }

    /// <summary>
    /// 一个世界的「本帧帧事件表」：阶段之间唯一的交接方式，插件互不持有引用。
    /// Timeline 阶段写，Hitbox / Damage / Buff / Spawn 读。
    ///
    /// 每逻辑帧开头由 BattleWorld 调 BeginFrame 清空，所以读到的永远是本帧结果、不会残留上一帧 ——
    /// 即使 Timeline 插件没注册，读到的也只是空表而不是脏数据。
    /// </summary>
    public sealed class CombatFrameEventTable
    {
        private readonly List<CombatFrameEventSet> _pool = new();

        public long Frame { get; private set; } = -1;

        public int Count { get; private set; }

        public CombatFrameEventSet this[int index] => _pool[index];

        /// <summary>本帧开始：标记表为空，池里的表项留作复用。</summary>
        public void BeginFrame(long frame)
        {
            Frame = frame;
            Count = 0;
        }

        /// <summary>Timeline 阶段为某个单位追加一组帧事件，返回可写的表项。</summary>
        public CombatFrameEventSet Append(long entityId, in CombatActionSnapshot action, bool hasHitConfirmed)
        {
            if (Count == _pool.Count)
            {
                _pool.Add(new CombatFrameEventSet());
            }

            var set = _pool[Count++];
            set.Reset(entityId, action, hasHitConfirmed);
            return set;
        }

        /// <summary>按单位 ID 查找本帧帧事件。表项数等于"本帧出招的单位数"，比全单位少，线性扫足够。</summary>
        public bool TryFind(long entityId, out CombatFrameEventSet set)
        {
            for (var i = 0; i < Count; i++)
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

        public long Frame { get; private set; } = -1;

        public int Count => _hits.Count;

        public CombatHitCandidate this[int index] => _hits[index];

        public void BeginFrame(long frame)
        {
            Frame = frame;
            _hits.Clear();
        }

        public void Add(in CombatHitCandidate hit)
        {
            _hits.Add(hit);
        }

        public void Clear()
        {
            Frame = -1;
            _hits.Clear();
        }
    }
}
