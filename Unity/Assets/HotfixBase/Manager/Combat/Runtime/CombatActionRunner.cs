using System;
using System.Collections.Generic;

namespace Ux
{
    [Serializable]
    public struct CombatActionSnapshot
    {
        public long InstanceId;
        public long RequestId;
        public int ActionId;
        public long StartSimulationFrame;
        public int ActionFrame;
        public bool IsPredicted;
        public bool IsConfirmed;
        public bool HasHitConfirmed;
    }

    [Serializable]
    public struct CombatAcceptedHitSnapshot
    {
        public long ActionInstanceId;
        public string WindowId;
        public long TargetId;
    }

    [Serializable]
    public sealed class UnitCombatSnapshot
    {
        public const int CurrentVersion = 1;

        public int Version;
        public UnitStateMachineSnapshot StateMachine;
        public CombatActionSnapshot Action;
        public bool HasAction;
        public CombatAcceptedHitSnapshot[] AcceptedHits;
        public long LocalActionSequence;
        public bool IsGrounded;
    }

    public readonly struct CombatActionChangedEvent
    {
        public readonly CombatActionAsset Previous;
        public readonly CombatActionAsset Current;
        public readonly CombatActionEndReason EndReason;
        public readonly long SimulationFrame;

        public CombatActionChangedEvent(CombatActionAsset previous, CombatActionAsset current, CombatActionEndReason endReason, long simulationFrame)
        {
            Previous = previous;
            Current = current;
            EndReason = endReason;
            SimulationFrame = simulationFrame;
        }
    }

    /// <summary>
    /// 当前逻辑帧内处于激活状态的一条命中窗口快照。它只携带可复制的逻辑形状参数，
    /// 不持有 CombatActionAsset 引用，也不包含目标属性或伤害。
    /// </summary>
    public readonly struct CombatActiveHitWindow
    {
        public CombatActiveHitWindow(long actionInstanceId, int actionId, int actionFrame, int windowIndex, ActionHitWindow window)
        {
            ActionInstanceId = actionInstanceId;
            ActionId = actionId;
            ActionFrame = actionFrame;
            WindowIndex = windowIndex;
            WindowId = window?.StableId ?? string.Empty;
            StartFrame = window?.StartFrame ?? 0;
            EndFrame = window?.EndFrame ?? 0;
            Shape = window?.Shape ?? ActionHitShape.Circle;
            RadiusMillimeters = window?.RadiusMillimeters ?? 0;
        }

        public long ActionInstanceId { get; }
        public int ActionId { get; }
        public int ActionFrame { get; }
        public int WindowIndex { get; }
        public string WindowId { get; }
        public int StartFrame { get; }
        public int EndFrame { get; }
        public ActionHitShape Shape { get; }
        public int RadiusMillimeters { get; }
    }

    /// <summary>
    /// 每 Unit 独立的动作生命周期。它直接消费逻辑帧命令，管理动作帧、取消、完成、预测确认和快照；
    /// 不再依赖“每个动作一个状态节点”的状态图。
    /// </summary>
    public sealed class CombatActionRunner
    {
        private readonly Dictionary<int, CombatActionAsset> _actions = new();
        private readonly HashSet<CombatHitKey> _acceptedHits = new();
        private long _localSequence;
        private long _simulationFrame;
        private bool _initialized;

        public event Action<CombatActionChangedEvent> ActionChanged;

        public CombatActionSnapshot Current { get; private set; }
        public CombatActionAsset CurrentAsset { get; private set; }
        public bool HasAction => CurrentAsset != null;
        public long LocalSequence => _localSequence;
        public bool BlocksMovement =>
            CurrentAsset?.MovementPolicy == ActionMovementPolicy.Block;

        public void Initialize(CharacterCombatProfile profile, long simulationFrame)
        {
            Clear();
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            _simulationFrame = Math.Max(0, simulationFrame);
            var stableIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var action in profile.Actions)
            {
                if (action == null)
                {
                    continue;
                }
                if (action.ActionId <= 0 || string.IsNullOrEmpty(action.StableId))
                {
                    throw new InvalidOperationException($"动作缺少 ActionId 或 StableId: {action.name}");
                }
                if (action.DurationFrames <= 0)
                {
                    throw new InvalidOperationException(
                        $"动作逻辑持续帧必须大于 0: action={action.name}, duration={action.DurationFrames}");
                }
                if (!_actions.TryAdd(action.ActionId, action))
                {
                    throw new InvalidOperationException($"动作 ID 重复: {action.ActionId}");
                }
                if (!stableIds.Add(action.StableId))
                {
                    throw new InvalidOperationException($"动作 StableId 重复: {action.StableId}");
                }
            }

            foreach (var action in _actions.Values)
            {
                if (action.CancelWindows == null)
                {
                    throw new InvalidOperationException($"取消窗口列表为空引用: action={action.name}");
                }
                if (action.HitWindows == null)
                {
                    throw new InvalidOperationException($"命中窗口列表为空引用: action={action.name}");
                }

                var logicItemIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (var window in action.CancelWindows)
                {
                    if (window == null)
                    {
                        throw new InvalidOperationException($"取消窗口为空引用: action={action.name}");
                    }
                    if (string.IsNullOrEmpty(window.StableId) ||
                        !logicItemIds.Add(window.StableId))
                    {
                        throw new InvalidOperationException(
                            $"逻辑子项 StableId 缺失或重复: action={action.name}, item={window.StableId}");
                    }
                    if (window.StartFrame < 0 ||
                        window.EndFrame <= window.StartFrame ||
                        window.EndFrame > action.DurationFrames)
                    {
                        throw new InvalidOperationException(
                            $"取消窗口区间无效: action={action.name}, range=[{window.StartFrame}, {window.EndFrame}), duration={action.DurationFrames}");
                    }
                    if (!_actions.ContainsKey(window.TargetActionId))
                    {
                        throw new InvalidOperationException(
                            $"取消窗口目标动作不存在: action={action.name}, target={window.TargetActionId}");
                    }
                }
                foreach (var window in action.HitWindows)
                {
                    if (window == null)
                    {
                        throw new InvalidOperationException($"命中窗口为空引用: action={action.name}");
                    }
                    if (string.IsNullOrEmpty(window.StableId) ||
                        !logicItemIds.Add(window.StableId))
                    {
                        throw new InvalidOperationException(
                            $"逻辑子项 StableId 缺失或重复: action={action.name}, item={window.StableId}");
                    }
                    if (window.StartFrame < 0 ||
                        window.EndFrame <= window.StartFrame ||
                        window.EndFrame > action.DurationFrames)
                    {
                        throw new InvalidOperationException(
                            $"命中窗口区间无效: action={action.name}, range=[{window.StartFrame}, {window.EndFrame}), duration={action.DurationFrames}");
                    }
                    if (!Enum.IsDefined(typeof(ActionHitShape), window.Shape) ||
                        window.RadiusMillimeters <= 0 ||
                        window.RadiusMillimeters > 10000000)
                    {
                        throw new InvalidOperationException(
                            $"命中窗口形状参数无效: action={action.name}, shape={window.Shape}, radius={window.RadiusMillimeters}");
                    }
                }
            }
            _initialized = true;
        }

        public void Tick(long simulationFrame, in CombatFrameCommands commands, bool canStartActions)
        {
            if (!_initialized)
            {
                return;
            }

            AdvanceTo(simulationFrame);
            if (!canStartActions)
            {
                Interrupt(CombatActionEndReason.Interrupted);
                return;
            }

            if (HasAction)
            {
                TryCancel(commands);
            }
            else
            {
                TryStart(commands);
            }
        }

        public bool StartAction(int actionId, long requestId, long simulationFrame, bool predicted)
        {
            if (!_initialized || !_actions.TryGetValue(actionId, out var action))
            {
                return false;
            }

            AdvanceTo(simulationFrame);
            if (HasAction)
            {
                EndCurrent(CombatActionEndReason.Interrupted);
            }
            Start(action, requestId, predicted);
            return true;
        }

        public bool Confirm(long requestId, long authoritativeInstanceId, long authoritativeStartFrame)
        {
            if (!HasAction || Current.RequestId != requestId || authoritativeInstanceId <= 0)
            {
                return false;
            }

            var previousInstanceId = Current.InstanceId;
            var current = Current;
            current.InstanceId = authoritativeInstanceId;
            current.StartSimulationFrame = Math.Max(0, authoritativeStartFrame);
            current.ActionFrame = (int)Math.Min(
                int.MaxValue,
                Math.Max(0, _simulationFrame - current.StartSimulationFrame));
            current.IsPredicted = false;
            current.IsConfirmed = true;
            MigrateAcceptedHitInstanceId(previousInstanceId, authoritativeInstanceId);
            Current = current;
            _localSequence = Math.Max(_localSequence, authoritativeInstanceId);
            if (Current.ActionFrame >= CurrentAsset.DurationFrames)
            {
                EndCurrent(CombatActionEndReason.Completed);
            }
            return true;
        }

        public bool Reject(long requestId)
        {
            if (!HasAction || Current.RequestId != requestId)
            {
                return false;
            }
            EndCurrent(CombatActionEndReason.Rejected);
            return true;
        }

        /// <summary>
        /// 按 CombatActionAsset 中的序列化顺序追加当前帧激活的命中窗口。
        /// 该查询无副作用、不会分配内部集合，也不会执行空间查询或伤害结算。
        /// </summary>
        public int AppendActiveHitWindows(List<CombatActiveHitWindow> output)
        {
            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }
            if (!HasAction || CurrentAsset.HitWindows == null)
            {
                return 0;
            }

            var added = 0;
            for (var i = 0; i < CurrentAsset.HitWindows.Count; i++)
            {
                var window = CurrentAsset.HitWindows[i];
                if (window == null || !window.IsActive(Current.ActionFrame))
                {
                    continue;
                }
                output.Add(new CombatActiveHitWindow(
                    Current.InstanceId,
                    Current.ActionId,
                    Current.ActionFrame,
                    i,
                    window));
                added++;
            }
            return added;
        }

        private void MigrateAcceptedHitInstanceId(long previousInstanceId, long currentInstanceId)
        {
            if (previousInstanceId == currentInstanceId || _acceptedHits.Count == 0)
            {
                return;
            }

            var migrated = new List<CombatHitKey>();
            foreach (var key in _acceptedHits)
            {
                if (key.ActionInstanceId == previousInstanceId)
                {
                    migrated.Add(key);
                }
            }
            for (var i = 0; i < migrated.Count; i++)
            {
                var key = migrated[i];
                _acceptedHits.Remove(key);
                _acceptedHits.Add(new CombatHitKey(
                    currentInstanceId,
                    key.WindowId,
                    key.TargetId));
            }
        }

        public bool TryAcceptHit(long actionInstanceId, string windowId, long targetId)
        {
            if (!HasAction || actionInstanceId != Current.InstanceId ||
                string.IsNullOrEmpty(windowId) || targetId <= 0)
            {
                return false;
            }
            return _acceptedHits.Add(new CombatHitKey(actionInstanceId, windowId, targetId));
        }

        public CombatAcceptedHitSnapshot[] CaptureAcceptedHits()
        {
            if (_acceptedHits.Count == 0)
            {
                return Array.Empty<CombatAcceptedHitSnapshot>();
            }

            var result = new List<CombatAcceptedHitSnapshot>(_acceptedHits.Count);
            foreach (var key in _acceptedHits)
            {
                if (key.ActionInstanceId == Current.InstanceId)
                {
                    result.Add(new CombatAcceptedHitSnapshot
                    {
                        ActionInstanceId = key.ActionInstanceId,
                        WindowId = key.WindowId,
                        TargetId = key.TargetId,
                    });
                }
            }
            result.Sort((left, right) =>
            {
                var windowCompare = string.Compare(
                    left.WindowId,
                    right.WindowId,
                    StringComparison.Ordinal);
                return windowCompare != 0
                    ? windowCompare
                    : left.TargetId.CompareTo(right.TargetId);
            });
            return result.ToArray();
        }

        public bool MarkHitConfirmed(long actionInstanceId)
        {
            if (!HasAction || Current.InstanceId != actionInstanceId)
            {
                return false;
            }
            var current = Current;
            current.HasHitConfirmed = true;
            Current = current;
            return true;
        }

        public bool Interrupt(CombatActionEndReason reason = CombatActionEndReason.Interrupted)
        {
            if (!HasAction)
            {
                return false;
            }
            EndCurrent(reason);
            return true;
        }

        public void Restore(in CombatActionSnapshot snapshot, bool hasAction, long simulationFrame, CombatAcceptedHitSnapshot[] acceptedHits = null, long localActionSequence = -1)
        {
            _simulationFrame = Math.Max(0, simulationFrame);
            if (localActionSequence >= 0)
            {
                if (hasAction && (snapshot.InstanceId <= 0 || localActionSequence < snapshot.InstanceId))
                {
                    throw new InvalidOperationException(
                        $"快照动作序列无效: instance={snapshot.InstanceId}, sequence={localActionSequence}");
                }
                _localSequence = localActionSequence;
            }
            if (!hasAction)
            {
                if (HasAction)
                {
                    EndCurrent(CombatActionEndReason.SnapshotRestore);
                }
                return;
            }
            if (!_actions.TryGetValue(snapshot.ActionId, out var action))
            {
                throw new InvalidOperationException($"快照动作不存在: {snapshot.ActionId}");
            }
            if (snapshot.ActionFrame < 0 || snapshot.ActionFrame >= action.DurationFrames)
            {
                throw new InvalidOperationException(
                    $"快照动作帧无效: action={snapshot.ActionId}, frame={snapshot.ActionFrame}, duration={action.DurationFrames}");
            }

            var previous = CurrentAsset;
            CurrentAsset = action;
            _acceptedHits.Clear();
            if (acceptedHits != null)
            {
                for (var i = 0; i < acceptedHits.Length; i++)
                {
                    var accepted = acceptedHits[i];
                    if (accepted.ActionInstanceId == snapshot.InstanceId &&
                        !string.IsNullOrEmpty(accepted.WindowId) &&
                        accepted.TargetId > 0)
                    {
                        _acceptedHits.Add(new CombatHitKey(
                            accepted.ActionInstanceId,
                            accepted.WindowId,
                            accepted.TargetId));
                    }
                }
            }
            var current = snapshot;
            current.ActionFrame = Math.Max(0, current.ActionFrame);
            Current = current;
            if (localActionSequence < 0)
            {
                _localSequence = Math.Max(_localSequence, snapshot.InstanceId);
            }
            ActionChanged?.Invoke(new CombatActionChangedEvent(
                previous,
                action,
                CombatActionEndReason.SnapshotRestore,
                _simulationFrame));
        }

        public CombatActionAsset FindAction(int actionId)
        {
            return _actions.TryGetValue(actionId, out var action) ? action : null;
        }

        public void Clear()
        {
            if (HasAction)
            {
                EndCurrent(CombatActionEndReason.Release);
            }
            Current = default;
            CurrentAsset = null;
            _actions.Clear();
            _acceptedHits.Clear();
            _localSequence = 0;
            _simulationFrame = 0;
            _initialized = false;
            ActionChanged = null;
        }

        private void AdvanceTo(long simulationFrame)
        {
            var target = Math.Max(0, simulationFrame);
            if (target <= _simulationFrame)
            {
                return;
            }

            var delta = target - _simulationFrame;
            _simulationFrame = target;
            if (!HasAction)
            {
                return;
            }

            var current = Current;
            current.ActionFrame = delta >= int.MaxValue - (long)current.ActionFrame
                ? int.MaxValue
                : current.ActionFrame + (int)delta;
            Current = current;
            if (Current.ActionFrame >= CurrentAsset.DurationFrames)
            {
                EndCurrent(CombatActionEndReason.Completed);
            }
        }

        private bool TryStart(in CombatFrameCommands commands)
        {
            for (var i = 0; i < commands.Count; i++)
            {
                var command = commands[i];
                if (!_actions.TryGetValue(command.ActionId, out var action))
                {
                    continue;
                }

                Start(action, command.RequestId, command.RequestId != 0);
                return true;
            }
            return false;
        }

        private bool TryCancel(in CombatFrameCommands commands)
        {
            for (var commandIndex = 0; commandIndex < commands.Count; commandIndex++)
            {
                var command = commands[commandIndex];
                if (!_actions.TryGetValue(command.ActionId, out var target))
                {
                    continue;
                }

                foreach (var window in CurrentAsset.CancelWindows)
                {
                    if (window.TargetActionId != command.ActionId ||
                        !window.IsOpen(Current.ActionFrame, Current.HasHitConfirmed))
                    {
                        continue;
                    }

                    EndCurrent(CombatActionEndReason.Cancelled);
                    Start(target, command.RequestId, command.RequestId != 0);
                    return true;
                }
            }
            return false;
        }

        private void Start(CombatActionAsset action, long requestId, bool predicted)
        {
            var previous = CurrentAsset;
            CurrentAsset = action;
            _acceptedHits.Clear();
            Current = new CombatActionSnapshot
            {
                InstanceId = ++_localSequence,
                RequestId = requestId,
                ActionId = action.ActionId,
                StartSimulationFrame = _simulationFrame,
                ActionFrame = 0,
                IsPredicted = predicted,
                IsConfirmed = !predicted,
                HasHitConfirmed = false,
            };
            ActionChanged?.Invoke(new CombatActionChangedEvent(
                previous,
                action,
                CombatActionEndReason.Started,
                _simulationFrame));
        }

        private void EndCurrent(CombatActionEndReason reason)
        {
            var previous = CurrentAsset;
            _acceptedHits.Clear();
            Current = default;
            CurrentAsset = null;
            ActionChanged?.Invoke(new CombatActionChangedEvent(
                previous,
                null,
                reason,
                _simulationFrame));
        }

    }
}
