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
        private const int PhaseCount = 8;
        private const ulong FnvOffsetBasis = 14695981039346656037UL;
        private const ulong FnvPrime = 1099511628211UL;

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
        }

        public string Key { get; }

        public int FrameRate { get; }

        public long Frame { get; private set; }

        public int EntityCount => _entities.Count;

        /// <summary>允许的单帧追赶上限，超出只告警不截断，避免逻辑帧断裂。</summary>
        public int MaxCatchUpFrames { get; set; } = 8;

        public IReadOnlyCollection<ICombatEntity> Entities => _entities.Values;

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

        /// <summary>推进到目标逻辑帧。只允许正向推进，回退帧会被忽略并告警。</summary>
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

                hash = AppendHash(hash, (ulong)controller.States.GetCurrentStateId(StateLayer.Locomotion));
                hash = AppendHash(hash, (ulong)controller.States.GetCurrentStateId(StateLayer.Action));
                hash = AppendHash(hash, (ulong)controller.States.GetCurrentStateId(StateLayer.Control));
                hash = AppendHash(hash, (ulong)controller.States.GetCurrentStateId(StateLayer.Life));
                hash = AppendHash(hash, (ulong)controller.States.SimulationFrame);
                hash = AppendHash(hash, controller.IsGrounded ? 1UL : 0UL);
                hash = AppendHash(hash, controller.Actions.HasAction ? 1UL : 0UL);
                hash = AppendHash(hash, (ulong)controller.Actions.Current.ActionId);
                hash = AppendHash(hash, (ulong)controller.Actions.Current.ActionFrame);
                hash = AppendHash(hash, (ulong)controller.Actions.Current.InstanceId);
                hash = AppendHash(hash, (ulong)controller.Actions.Current.StartSimulationFrame);
                hash = AppendHash(hash, controller.Actions.Current.HasHitConfirmed ? 1UL : 0UL);
                hash = AppendHash(hash, (ulong)controller.Actions.Current.RequestId);
                hash = AppendHash(hash, controller.Actions.Current.IsPredicted ? 1UL : 0UL);
                hash = AppendHash(hash, controller.Actions.Current.IsConfirmed ? 1UL : 0UL);
                hash = AppendHash(hash, (ulong)controller.Actions.LocalSequence);
                var acceptedHits = controller.Actions.CaptureAcceptedHits();
                hash = AppendHash(hash, (ulong)acceptedHits.Length);
                for (var hitIndex = 0; hitIndex < acceptedHits.Length; hitIndex++)
                {
                    var accepted = acceptedHits[hitIndex];
                    hash = AppendHash(hash, (ulong)accepted.ActionInstanceId);
                    hash = AppendHash(hash, accepted.WindowId);
                    hash = AppendHash(hash, (ulong)accepted.TargetId);
                }
            }
            return hash;
        }

        private void TickFrame(long frame)
        {
            var count = _ordered.Length;

            // 1 命令采样
            RunSystems(BattlePhase.Commands, frame);
            for (var i = 0; i < count; i++)
            {
                var entity = _ordered[i];
                _frameCommands[i] = entity.IsCombatActive
                    ? entity.ConsumeCommands(frame)
                    : CombatFrameCommands.Empty;
            }

            // 2 状态与动作推进（含位移）
            RunSystems(BattlePhase.Actions, frame);
            for (var i = 0; i < count; i++)
            {
                var entity = _ordered[i];
                if (entity.IsCombatActive)
                {
                    entity.TickLogic(frame, _frameCommands[i]);
                }
            }

            // 3 Timeline 求值（P2 的 Gameplay Track 接入后才有内置实现）
            RunSystems(BattlePhase.Timeline, frame);

            // 4-7 命中查询 / 伤害结算 / Buff / 死亡判定，全部由插件提供
            RunSystems(BattlePhase.Hitbox, frame);
            RunSystems(BattlePhase.Damage, frame);
            RunSystems(BattlePhase.Buff, frame);
            RunSystems(BattlePhase.Death, frame);

            // 8 表现同步
            RunSystems(BattlePhase.Presentation, frame);
            for (var i = 0; i < count; i++)
            {
                var entity = _ordered[i];
                if (entity.IsCombatActive)
                {
                    entity.TickPresentation(frame);
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
