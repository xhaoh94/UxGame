using System;
using System.Collections.Generic;

namespace Ux
{
    /// <summary>
    /// 一场战斗的容器：持有参战单位、按固定阶段顺序推进一个逻辑帧、提供世界级快照与状态哈希。
    /// 它不引用 Unit / GameObject，因此可以在无渲染环境（战报校验、服务器重放）里独立运行；
    /// 也正因如此它不是单例——主世界与回放世界需要能同时存在，由 CombatMgr 管理多个实例。
    /// </summary>
    public sealed class BattleWorld
    {
        // 阶段种类数。必须与 BattlePhase 成员数、_systemBuckets 的 new() 个数同步，
        // 漏改不会编译报错，只会静默失效（越界错误日志 / 直接数组越界）。
        private const int PhaseCount = 8;
        private const ulong FnvOffsetBasis = 14695981039346656037UL;
        private const ulong FnvPrime = 1099511628211UL;

        // 逻辑帧内各阶段的执行顺序，是阶段顺序的唯一事实来源。
        // 不用 Enum.GetValues：BattlePhase 是 : byte，它返回的是 byte[]（强转会抛 InvalidCastException）；
        // 而且它每次调用都新建数组，写死在这里才能被下面的静态构造校验抓住漏补。
        private static readonly BattlePhase[] PhaseOrder = BuildPhaseOrder();

        /// <summary>启动校验：PhaseOrder 与 PhaseCount / 枚举值不一致时记错误日志。不抛异常，避免 TypeInitializationException 让整个类型不可用。</summary>
        static BattleWorld()
        {
            if (PhaseOrder.Length != PhaseCount)
            {
                Log.Error(
                    $"BattlePhase 成员数与 PhaseCount 不一致: enum={PhaseOrder.Length}, PhaseCount={PhaseCount}");
            }

            for (var i = 0; i < PhaseOrder.Length; i++)
            {
                if ((int)PhaseOrder[i] != i)
                {
                    Log.Error($"PhaseOrder 第 {i} 项应为 {(BattlePhase)i}，实际为 {PhaseOrder[i]}");
                }
            }
        }

        private static BattlePhase[] BuildPhaseOrder()
        {
            // 顺序 = 枚举值升序，和 BattlePhase 里的定义顺序保持一致。
            return new[]
            {
                BattlePhase.Commands,
                BattlePhase.Actions,
                BattlePhase.Timeline,
                BattlePhase.Hitbox,
                BattlePhase.Damage,
                BattlePhase.Buff,
                BattlePhase.Death,
                BattlePhase.Presentation,
            };
        }

        // SortedDictionary 保证按 Id 升序遍历，这是遍历顺序确定性的来源。
        private readonly SortedDictionary<long, ICombatEntity> _entities = new();
        private readonly List<SystemEntry> _systems = new();
        private readonly List<SystemEntry>[] _systemBuckets =
        {
            new(), new(), new(), new(), new(), new(), new(), new(),
        };

        // 遍历快照，避免每帧从 SortedDictionary 取枚举器。
        private ICombatEntity[] _ordered = Array.Empty<ICombatEntity>();
        private CombatFrameCommands[] _frameCommands = Array.Empty<CombatFrameCommands>();

        // 本帧出招中的单位：Actions 阶段末尾收集，Timeline 阶段直接消费，省掉插件再扫一遍全场。
        private readonly List<ICombatEntity> _actionActive = new();

        private bool _entityOrderDirty = true;
        private bool _systemOrderDirty = true;
        private long _systemSequence;

        public event Action<ICombatEntity> EntityRegistered;
        public event Action<ICombatEntity> EntityUnregistered;


        public BattleWorld(string key, int frameRate, long startFrame = 0)
        {
            Key = key ?? string.Empty;
            FrameRate = Math.Max(1, frameRate);
            Frame = Math.Max(0, startFrame);

            // 框架随世界一起创建的内置插件。它们填的阶段槽位在设计上允许被替换或再叠加，
            // 只是当前这套实现是所有玩法都要用的默认组合。
            AddSystem(new CombatTimelineSystem());
            AddSystem(new HitboxSystem());
            AddSystem(new CombatDamageSystem());
            AddSystem(new CombatBuffSystem());
            AddSystem(new CombatDeathSystem());
        }

        public string Key { get; }

        public int FrameRate { get; }

        public long Frame { get; private set; }

        public int EntityCount => _entities.Count;

        /// <summary>允许的单帧追赶上限，超出只告警不截断，避免逻辑帧断裂。</summary>
        public int MaxCatchUpFrames { get; set; } = 8;

        public IReadOnlyCollection<ICombatEntity> Entities => _entities.Values;

        /// <summary>
        /// 按 Id 升序的单位数组快照，给逐帧遍历用（foreach Entities 会装箱接口枚举器，每帧一个堆对象）。
        /// 内容在 Tick 开头刷新：中途注册/注销的单位下一次 Tick 才可见，与内置阶段是同一个视图。
        /// </summary>
        public IReadOnlyList<ICombatEntity> OrderedEntities => _ordered;

        /// <summary>
        /// 本帧出招中的单位，由 Actions 阶段在 TickLogic 之后收集，Timeline 阶段直接消费。
        /// "谁在出招"因此成了阶段产出的显式事实，插件不必自己扫全场推断。
        /// 顺序 = _ordered 的 Id 升序，与帧事件表的写入顺序一致。
        ///
        /// 它是 Actions 阶段末尾的快照：之后的阶段再改 IsCombatActive 不会反映到这里。
        /// </summary>
        public IReadOnlyList<ICombatEntity> ActionActiveEntities => _actionActive;

        /// <summary>本帧帧事件表：Timeline 阶段的产物，Hitbox / Damage / Buff 的输入。每帧开头复位，插件之间唯一的交接方式。</summary>
        public CombatFrameEventTable FrameEvents { get; } = new();

        /// <summary>本帧待结算命中：Hitbox 阶段的产物，Damage 阶段的输入。</summary>
        public CombatHitBuffer PendingHits { get; } = new();

        #region 单位注册

        public bool Register(ICombatEntity entity)
        {
            if (entity == null)
            {
                return false;
            }

            if (_entities.TryGetValue(entity.Id, out var existing))
            {
                if (ReferenceEquals(existing, entity))
                {
                    return false;
                }
                Log.Warning($"战斗世界已存在同 ID 单位，将被替换: world={Key}, id={entity.Id}");
            }

            _entities[entity.Id] = entity;
            _entityOrderDirty = true;
            EntityRegistered?.Invoke(entity);
            return true;
        }

        public bool Unregister(long id)
        {
            if (!_entities.TryGetValue(id, out var entity))
            {
                return false;
            }

            _entities.Remove(id);
            _entityOrderDirty = true;
            EntityUnregistered?.Invoke(entity);
            return true;
        }

        public bool Unregister(ICombatEntity entity)
        {
            return entity != null && Unregister(entity.Id);
        }

        public bool TryGetEntity(long id, out ICombatEntity entity)
        {
            return _entities.TryGetValue(id, out entity);
        }

        public bool Contains(long id)
        {
            return _entities.ContainsKey(id);
        }

        public void Clear()
        {
            _entities.Clear();
            _entityOrderDirty = true;
        }

        #endregion

        #region 子系统

        public void AddSystem(ICombatSystem system)
        {
            if (system == null || IndexOfSystem(system) >= 0)
            {
                return;
            }

            _systems.Add(new SystemEntry(++_systemSequence, system));
            _systemOrderDirty = true;
        }

        public bool RemoveSystem(ICombatSystem system)
        {
            if (system == null)
            {
                return false;
            }

            var index = IndexOfSystem(system);
            if (index < 0)
            {
                return false;
            }

            _systems.RemoveAt(index);
            _systemOrderDirty = true;
            return true;
        }

        public void ClearSystems()
        {
            _systems.Clear();
            _systemOrderDirty = true;
        }

        #endregion

        /// <summary>
        /// 推进到目标逻辑帧，只允许正向推进，回退会被忽略并告警。
        /// 这是个追赶循环而不是"推一帧"：卡顿时一帧连补多次 TickFrame，保证逻辑帧号连续、绝不跳号
        /// （跳号会让帧同步直接失效）；补得太多只告警不截断，因为截断同样是跳号。
        /// </summary>
        public void Tick(long frame)
        {
            if (frame <= Frame)
            {
                if (frame < Frame)
                {
                    Log.Warning($"逻辑帧回退被忽略: world={Key}, current={Frame}, target={frame}");
                }
                return;
            }

            var pending = frame - Frame;
            if (pending > MaxCatchUpFrames)
            {
                Log.Warning(
                    $"单帧追赶过多，逻辑可能已卡顿: world={Key}, pending={pending}, max={MaxCatchUpFrames}");
            }

            EnsureOrder();
            EnsureSystems();
            while (Frame < frame)
            {
                Frame++;
                TickFrame(Frame);
            }
        }

        public BattleWorldSnapshot CaptureSnapshot()
        {
            EnsureOrder();
            var count = 0;
            var buffer = new EntityCombatSnapshot[_ordered.Length];
            for (var i = 0; i < _ordered.Length; i++)
            {
                var entity = _ordered[i];
                if (!entity.IsCombatActive)
                {
                    continue;
                }

                var combat = entity.Controller.CaptureSnapshot();
                if (combat == null)
                {
                    continue;
                }

                buffer[count++] = new EntityCombatSnapshot
                {
                    Id = entity.Id,
                    Combat = combat,
                    Position = entity.Position,
                    Rotation = entity.Rotation,
                };
            }

            if (count != buffer.Length)
            {
                Array.Resize(ref buffer, count);
            }
            return new BattleWorldSnapshot(Frame, buffer);
        }

        public void RestoreSnapshot(BattleWorldSnapshot snapshot)
        {
            if (snapshot?.Entities == null)
            {
                return;
            }

            EnsureOrder();
            var snapshotIndex = 0;
            for (var i = 0; i < _ordered.Length; i++)
            {
                var entity = _ordered[i];
                if (!entity.IsCombatActive)
                {
                    continue;
                }
                if (snapshotIndex >= snapshot.Entities.Length ||
                    snapshot.Entities[snapshotIndex].Id != entity.Id)
                {
                    throw new InvalidOperationException(
                        "无法恢复战斗快照：当前可恢复实体集合与快照不一致。");
                }
                var combat = snapshot.Entities[snapshotIndex].Combat;
                if (combat?.StateMachine == null ||
                    combat.Version != UnitCombatSnapshot.CurrentVersion)
                {
                    throw new InvalidOperationException(
                        $"无法恢复战斗快照：实体 {entity.Id} 的战斗快照无效。");
                }
                snapshotIndex++;
            }
            if (snapshotIndex != snapshot.Entities.Length)
            {
                throw new InvalidOperationException(
                    "无法恢复战斗快照：当前可恢复实体集合与快照不一致。");
            }

            for (var i = 0; i < snapshot.Entities.Length; i++)
            {
                var entry = snapshot.Entities[i];
                var entity = _entities[entry.Id];
                entity.Controller.RestoreSnapshot(entry.Combat);
                entity.Position = entry.Position;
                entity.Rotation = entry.Rotation;
            }

            Frame = Math.Max(0, snapshot.Frame);
        }

        /// <summary>
        /// 世界状态的确定性哈希。相同输入序列跑两次必须得到相同的值，
        /// 这是战报校验和帧同步发散排查的基础。位置与朝向不参与计算：P0 尚未引入定点数，
        /// 浮点跨机器不一致会把哈希变成噪声。
        /// 覆盖范围：状态机各层状态、动作实例与动作帧、已确认命中、属性（血量）、增益列表。
        /// </summary>
        public ulong ComputeStateHash()
        {
            EnsureOrder();
            var hash = AppendHash(FnvOffsetBasis, (ulong)Frame);
            for (var i = 0; i < _ordered.Length; i++)
            {
                var entity = _ordered[i];
                hash = AppendHash(hash, (ulong)entity.Id);

                var controller = entity.Controller;
                if (controller?.IsInitialized != true)
                {
                    continue;
                }

                hash = AppendHash(hash, (ulong)controller.StateMachine.GetCurrentStateId(StateLayer.Locomotion));
                hash = AppendHash(hash, (ulong)controller.StateMachine.GetCurrentStateId(StateLayer.Control));
                hash = AppendHash(hash, (ulong)controller.StateMachine.GetCurrentStateId(StateLayer.Life));
                hash = AppendHash(hash, (ulong)controller.StateMachine.SimulationFrame);
                hash = AppendHash(hash, controller.IsGrounded ? 1UL : 0UL);
                hash = AppendHash(hash, controller.ActionRunner.HasAction ? 1UL : 0UL);
                hash = AppendHash(hash, (ulong)controller.ActionRunner.Current.ActionId);
                hash = AppendHash(hash, (ulong)controller.ActionRunner.Current.ActionFrame);
                hash = AppendHash(hash, (ulong)controller.ActionRunner.Current.InstanceId);
                hash = AppendHash(hash, (ulong)controller.ActionRunner.Current.StartSimulationFrame);
                hash = AppendHash(hash, controller.ActionRunner.Current.HasHitConfirmed ? 1UL : 0UL);
                hash = AppendHash(hash, (ulong)controller.ActionRunner.Current.RequestId);
                hash = AppendHash(hash, controller.ActionRunner.Current.IsPredicted ? 1UL : 0UL);
                hash = AppendHash(hash, controller.ActionRunner.Current.IsConfirmed ? 1UL : 0UL);
                hash = AppendHash(hash, (ulong)controller.ActionRunner.LocalSequence);
                var acceptedHits = controller.ActionRunner.CaptureAcceptedHits();
                hash = AppendHash(hash, (ulong)acceptedHits.Length);
                for (var hitIndex = 0; hitIndex < acceptedHits.Length; hitIndex++)
                {
                    var accepted = acceptedHits[hitIndex];
                    hash = AppendHash(hash, (ulong)accepted.ActionInstanceId);
                    hash = AppendHash(hash, accepted.WindowId);
                    hash = AppendHash(hash, (ulong)accepted.TargetId);
                }

                // 血量与增益是玩法上可见的状态，必须进哈希：少了它们，"血量在发散但状态机一致"会被判为校验通过。
                var attributes = controller.Attributes;
                hash = AppendHash(hash, (ulong)attributes.MaxHp);
                hash = AppendHash(hash, (ulong)attributes.Hp);

                var buffs = controller.Buffs;
                hash = AppendHash(hash, (ulong)buffs.Count);
                for (var buffIndex = 0; buffIndex < buffs.Count; buffIndex++)
                {
                    var buff = buffs[buffIndex];
                    hash = AppendHash(hash, (ulong)buff.BuffId);
                    hash = AppendHash(hash, (ulong)buff.RemainingFrames);
                    hash = AppendHash(hash, (ulong)buff.DamagePerTick);
                }
            }
            return hash;
        }

        /// <summary>
        /// Commands 阶段的内置核心：把每个单位本帧要执行的命令取出来，放进 _frameCommands
        /// </summary>
        private void PhaseCommands(int count, long frame)
        {
            for (var i = 0; i < count; i++)
            {
                var entity = _ordered[i];
                _frameCommands[i] = entity.IsCombatActive
                    ? entity.ConsumeCommands(frame)
                    : CombatFrameCommands.Empty;
            }
        }

        /// <summary>
        /// Actions 阶段的内置核心：推进每个战斗单位的逻辑（状态机、动作生命周期、位移），
        /// 并顺手收集"本帧出招中"的单位给 Timeline 阶段——这一层本来就在遍历全场，
        /// 顺手记一笔就能免掉下游再扫一遍。
        /// </summary>
        private void PhaseActions(int count, long frame)
        {
            // 与生产者同处复位：Actions 阶段末尾的"谁在出招"是本帧的权威事实。
            _actionActive.Clear();
            for (var i = 0; i < count; i++)
            {
                var entity = _ordered[i];
                if (!entity.IsCombatActive)
                {
                    continue;
                }

                entity.TickLogic(frame, _frameCommands[i]);

                // 必须在 TickLogic 之后收集：起手、取消、结束都发生在这一步里。
                if (entity.Controller.ActionRunner.HasAction)
                {
                    _actionActive.Add(entity);
                }
            }
        }

        /// <summary>Presentation 阶段的内置核心：逻辑帧全部结束后，才把本帧结果同步给表现层。</summary>
        private void PhasePresentation(int count, long frame)
        {
            for (var i = 0; i < count; i++)
            {
                var entity = _ordered[i];
                if (entity.IsCombatActive)
                {
                    entity.TickPresentation(frame);
                }
            }
        }

        private void TickFrame(long frame)
        {
            FrameEvents.BeginFrame(frame);
            PendingHits.BeginFrame(frame);

            var count = _ordered.Length;
            for (var i = 0; i < PhaseOrder.Length; i++)
            {
                var phase = PhaseOrder[i];
                RunSystems(phase, frame);
                switch (phase)
                {
                    case BattlePhase.Commands:
                        PhaseCommands(count, frame);
                        break;
                    case BattlePhase.Actions:
                        PhaseActions(count, frame);
                        break;
                    case BattlePhase.Presentation:
                        PhasePresentation(count, frame);
                        break;
                }
            }
        }

        private void RunSystems(BattlePhase phase, long frame)
        {
            var bucket = _systemBuckets[(int)phase];
            for (var i = 0; i < bucket.Count; i++)
            {
                bucket[i].System.Tick(this, frame);
            }
        }

        private void EnsureOrder()
        {
            if (!_entityOrderDirty)
            {
                return;
            }

            var count = _entities.Count;
            if (_ordered.Length != count)
            {
                _ordered = new ICombatEntity[count];
                _frameCommands = new CombatFrameCommands[count];
            }

            // 注意这里**没有任何排序**：_entities 是 SortedDictionary，本身就已经按 Id 升序，
            // 这一句只是把它的值序列"摊平"进数组，顺序原样带过来。
            // 名字叫 ordered 指的是"顺序已经是确定且有序的"，不是"这里做了排序"。
            var index = 0;
            foreach (var pair in _entities)
            {
                _ordered[index++] = pair.Value;
            }
            _entityOrderDirty = false;
        }

        private void EnsureSystems()
        {
            if (!_systemOrderDirty)
            {
                return;
            }

            for (var i = 0; i < PhaseCount; i++)
            {
                _systemBuckets[i].Clear();
            }

            for (var i = 0; i < _systems.Count; i++)
            {
                var entry = _systems[i];
                var phase = (int)entry.System.Phase;
                if (phase < 0 || phase >= PhaseCount)
                {
                    Log.Error($"战斗子系统阶段越界: system={entry.System.GetType().Name}, phase={entry.System.Phase}");
                    continue;
                }
                _systemBuckets[phase].Add(entry);
            }

            for (var i = 0; i < PhaseCount; i++)
            {
                _systemBuckets[i].Sort(CompareSystem);
            }
            _systemOrderDirty = false;
        }

        private int IndexOfSystem(ICombatSystem system)
        {
            for (var i = 0; i < _systems.Count; i++)
            {
                if (ReferenceEquals(_systems[i].System, system))
                {
                    return i;
                }
            }
            return -1;
        }

        private static int CompareSystem(SystemEntry a, SystemEntry b)
        {
            var order = a.System.Order.CompareTo(b.System.Order);
            return order != 0 ? order : a.Sequence.CompareTo(b.Sequence);
        }

        private static ulong AppendHash(ulong hash, ulong value)
        {
            hash ^= value;
            return hash * FnvPrime;
        }

        private static ulong AppendHash(ulong hash, string value)
        {
            if (value == null)
            {
                return AppendHash(hash, 0UL);
            }
            hash = AppendHash(hash, (ulong)value.Length);
            for (var i = 0; i < value.Length; i++)
            {
                hash = AppendHash(hash, value[i]);
            }
            return hash;
        }

        private readonly struct SystemEntry
        {
            public readonly long Sequence;
            public readonly ICombatSystem System;

            public SystemEntry(long sequence, ICombatSystem system)
            {
                Sequence = sequence;
                System = system;
            }
        }
    }
}
