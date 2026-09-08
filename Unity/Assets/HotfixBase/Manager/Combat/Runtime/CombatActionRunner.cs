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
    public sealed class UnitCombatSnapshot
    {
        public UnitStateMachineSnapshot StateMachine;
        public CombatActionSnapshot Action;
        public bool HasAction;
    }

    public readonly struct CombatActionChangedEvent
    {
        public readonly CombatActionAsset Previous;
        public readonly CombatActionAsset Current;
        public readonly CombatActionEndReason EndReason;
        public readonly long SimulationFrame;

        public CombatActionChangedEvent(
            CombatActionAsset previous,
            CombatActionAsset current,
            CombatActionEndReason endReason,
            long simulationFrame)
        {
            Previous = previous;
            Current = current;
            EndReason = endReason;
            SimulationFrame = simulationFrame;
        }
    }

    /// <summary>
    /// 每 Unit 独立的动作生命周期。它直接消费逻辑帧命令，管理动作帧、取消、完成、预测确认和快照；
    /// 不再依赖“每个动作一个状态节点”的状态图。
    /// </summary>
    public sealed class CombatActionRunner
    {
        private readonly Dictionary<int, CombatActionAsset> _actions = new();
        private readonly Dictionary<CombatCommandType, List<CombatActionAsset>> _startActions = new();
        private long _localSequence;
        private long _simulationFrame;
        private bool _initialized;

        public event Action<CombatActionChangedEvent> ActionChanged;

        public CombatActionSnapshot Current { get; private set; }
        public CombatActionAsset CurrentAsset { get; private set; }
        public bool HasAction => CurrentAsset != null;
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
                if (!_actions.TryAdd(action.ActionId, action))
                {
                    throw new InvalidOperationException($"动作 ID 重复: {action.ActionId}");
                }
                if (!stableIds.Add(action.StableId))
                {
                    throw new InvalidOperationException($"动作 StableId 重复: {action.StableId}");
                }
                if (action.Timeline != null && action.Timeline.FrameRate != profile.FrameRate)
                {
                    throw new InvalidOperationException(
                        $"动作 Timeline 帧率不一致: action={action.name}, actionRate={profile.FrameRate}, timelineRate={action.Timeline.FrameRate}");
                }

                if (!_startActions.TryGetValue(action.TriggerCommand, out var actions))
                {
                    actions = new List<CombatActionAsset>();
                    _startActions.Add(action.TriggerCommand, actions);
                }
                actions.Add(action);
            }

            foreach (var pair in _startActions)
            {
                pair.Value.Sort(CompareAction);
            }

            foreach (var action in _actions.Values)
            {
                foreach (var window in action.CancelWindows)
                {
                    if (window != null && !_actions.ContainsKey(window.TargetActionId))
                    {
                        throw new InvalidOperationException(
                            $"取消窗口目标动作不存在: action={action.name}, target={window.TargetActionId}");
                    }
                }
            }
            _initialized = true;
        }

        public void Tick(
            long simulationFrame,
            in CombatFrameCommands commands,
            bool canStartActions)
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

        public bool StartAction(
            int actionId,
            long requestId,
            long simulationFrame,
            bool predicted)
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

        public bool Confirm(
            long requestId,
            long authoritativeInstanceId,
            long authoritativeStartFrame)
        {
            if (!HasAction || Current.RequestId != requestId)
            {
                return false;
            }

            var current = Current;
            current.InstanceId = authoritativeInstanceId;
            current.StartSimulationFrame = Math.Max(0, authoritativeStartFrame);
            current.ActionFrame = (int)Math.Min(
                int.MaxValue,
                Math.Max(0, _simulationFrame - current.StartSimulationFrame));
            current.IsPredicted = false;
            current.IsConfirmed = true;
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

        public void MarkHitConfirmed()
        {
            if (!HasAction)
            {
                return;
            }
            var current = Current;
            current.HasHitConfirmed = true;
            Current = current;
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

        public void Restore(in CombatActionSnapshot snapshot, bool hasAction, long simulationFrame)
        {
            _simulationFrame = Math.Max(0, simulationFrame);
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
            var current = snapshot;
            current.ActionFrame = Math.Max(0, current.ActionFrame);
            Current = current;
            _localSequence = Math.Max(_localSequence, snapshot.InstanceId);
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
            _startActions.Clear();
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
            current.ActionFrame = (int)Math.Min(int.MaxValue, current.ActionFrame + delta);
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
                if (!_startActions.TryGetValue(command.Type, out var actions) || actions.Count == 0)
                {
                    continue;
                }

                var selected = actions[0];
                // Parameter > 0 表示调用方明确指定 ActionId；否则按该命令下的优先级排序选择。
                // 这样既兼容旧的 EnqueueCommand(Attack)，也支持 RequestAction(actionId)。
                if (command.Parameter > 0)
                {
                    selected = null;
                    for (var actionIndex = 0; actionIndex < actions.Count; actionIndex++)
                    {
                        if (actions[actionIndex].ActionId == command.Parameter)
                        {
                            selected = actions[actionIndex];
                            break;
                        }
                    }
                    if (selected == null)
                    {
                        continue;
                    }
                }

                Start(selected, command.RequestId, command.RequestId != 0);
                return true;
            }
            return false;
        }

        private bool TryCancel(in CombatFrameCommands commands)
        {
            for (var commandIndex = 0; commandIndex < commands.Count; commandIndex++)
            {
                var command = commands[commandIndex];
                ActionCancelWindow selected = null;
                foreach (var window in CurrentAsset.CancelWindows)
                {
                    if (window == null ||
                        !window.IsOpen(Current.ActionFrame, command.Type, Current.HasHitConfirmed) ||
                        (command.Parameter > 0 && window.TargetActionId != command.Parameter))
                    {
                        continue;
                    }
                    if (selected == null || CompareWindow(window, selected) < 0)
                    {
                        selected = window;
                    }
                }

                if (selected == null || !_actions.TryGetValue(selected.TargetActionId, out var target))
                {
                    continue;
                }

                EndCurrent(CombatActionEndReason.Cancelled);
                Start(target, command.RequestId, command.RequestId != 0);
                return true;
            }
            return false;
        }

        private void Start(CombatActionAsset action, long requestId, bool predicted)
        {
            var previous = CurrentAsset;
            CurrentAsset = action;
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
            Current = default;
            CurrentAsset = null;
            ActionChanged?.Invoke(new CombatActionChangedEvent(
                previous,
                null,
                reason,
                _simulationFrame));
        }

        private static int CompareAction(CombatActionAsset a, CombatActionAsset b)
        {
            var priority = b.Priority.CompareTo(a.Priority);
            return priority != 0 ? priority : a.ActionId.CompareTo(b.ActionId);
        }

        private static int CompareWindow(ActionCancelWindow a, ActionCancelWindow b)
        {
            var priority = b.Priority.CompareTo(a.Priority);
            return priority != 0 ? priority : a.TargetActionId.CompareTo(b.TargetActionId);
        }
    }
}
