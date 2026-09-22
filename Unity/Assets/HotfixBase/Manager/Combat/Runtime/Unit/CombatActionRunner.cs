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
        /// <summary>
        /// 1 → 2：加入 Attributes 与 Buffs；2 → 3：移除 CombatStateMachineSnapshot 的 Action 层快照。
        /// 版本号必须跟着字段一起涨（RestoreSnapshot 会拒绝版本不一致的快照）——旧快照还原新结构会让状态数据
        /// 静默错位，比直接拒绝危险。快照无落盘、无网络传输，升级不需兼容旧数据。
        /// </summary>
        public const int CurrentVersion = 3;

        public int Version;
        public CombatStateMachineSnapshot StateMachine;
        public CombatActionSnapshot Action;
        public bool HasAction;
        public CombatAcceptedHitSnapshot[] AcceptedHits;
        public long LocalActionSequence;
        public bool IsGrounded;
        public UnitAttributeSnapshot Attributes;
        public CombatBuffSnapshot[] Buffs;
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
    /// 每 Unit 独立的动作生命周期：消费逻辑帧命令，管理动作帧、取消、完成、预测确认和快照。
    ///
    /// 职责边界：CombatStateMachine 回答"现在处于什么状态"，本类回答"这一招播到第几帧了"。
    /// 所以加一个新技能不用动状态机，加一个 CombatActionAsset 资产就行。
    /// </summary>
    public sealed class CombatActionRunner
    {
        private readonly Dictionary<int, CombatActionAsset> _actions = new();
        private readonly HashSet<CombatHitKey> _acceptedHits = new();
        private long _simulationFrame;
        private bool _initialized;

        public event Action<CombatActionChangedEvent> ActionChanged;

        public CombatActionSnapshot Current { get; private set; }
        public CombatActionAsset CurrentAsset { get; private set; }
        public bool HasAction => CurrentAsset != null;
        public long LocalSequence { get; private set; }

        /// <summary>
        /// 当前动作是否锁移动。CombatController.IsMovementBlocked 会读它，
        /// 所以这一个属性就是"普攻能不能边跑边打"的开关（值来自资源的 movementPolicy）。
        /// </summary>
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

        /// <summary>
        /// 动作系统每帧的唯一入口，由 CombatController.Tick 调用。
        /// 走向按顺序判断：推进动作帧 → 不允许起手就打断 → 有动作走 TryCancel → 没动作走 TryStart。
        /// "有动作只能取消、没动作才能起手"这条互斥，保证同一单位同一时刻只有一条动作在跑。
        /// </summary>
        public void Tick(long simulationFrame, in CombatFrameCommands commands, bool canStartActions)
        {
            if (!_initialized)
            {
                return;
            }

            // 0. 动作帧 +1，到 DurationFrames 会自动 Completed 结束（见 AdvanceTo）
            AdvanceTo(simulationFrame);

            if (!canStartActions)
            {
                // 1. 死了或被控：无条件打断，命令也不看了
                Interrupt(CombatActionEndReason.Interrupted);
                return;
            }

            if (HasAction)
            {
                //当前有动作在执行，则判断是否能被新的命令取消，能打断则用新命令打断
                TryCancel(commands);
            }
            else
            {
                // 当前没有动作在执行，尝试执行新动作命令
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
            LocalSequence = Math.Max(LocalSequence, authoritativeInstanceId);
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
                LocalSequence = localActionSequence;
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
                LocalSequence = Math.Max(LocalSequence, snapshot.InstanceId);
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
            LocalSequence = 0;
            _simulationFrame = 0;
            _initialized = false;
            ActionChanged = null;
        }

        /// <summary>
        /// 逐帧推进：把动作帧推进到目标帧。
        ///
        /// 推的是差值而不是"帧号 = 帧号"：卡顿被补跑 3 帧时 ActionFrame 一次 +3，
        /// 动作总时长在帧率波动下保持一致，也不会因为丢帧永远播不完。
        /// 累加到 DurationFrames 就自动 Completed 结束，不需要任何人手动触发。
        /// </summary>
        private void AdvanceTo(long simulationFrame)
        {
            var target = Math.Max(0, simulationFrame);
            if (target <= _simulationFrame)
            {
                // 只允许正向推进；帧号回退会被静默忽略（BattleWorld 那边会告警）
                return;
            }

            var delta = target - _simulationFrame;
            _simulationFrame = target;
            if (!HasAction)
            {
                return;
            }

            var current = Current;
            // 溢出保护：ActionFrame 是 int，长时间不结束的动作也不会翻负
            current.ActionFrame = delta >= int.MaxValue - (long)current.ActionFrame
                ? int.MaxValue
                : current.ActionFrame + (int)delta;
            Current = current;

            if (Current.ActionFrame >= CurrentAsset.DurationFrames)
            {
                EndCurrent(CombatActionEndReason.Completed);
            }
        }

        /// <summary>
        /// 空闲时尝试起手：取第一条能在本单位动作表里找到的命令，找到就起手并返回 —— 一帧最多起手一次。
        /// 技能的一切（时长、窗口、锁不锁移动）都查表得到，所以命令本身只需要一个 ActionId。
        /// </summary>
        private bool TryStart(in CombatFrameCommands commands)
        {
            for (var i = 0; i < commands.Count; i++)
            {
                var command = commands[i];
                if (!_actions.TryGetValue(command.ActionId, out var action))
                {
                    // 本单位没有这个 ActionId（比如别的角色的技能）→ 跳过
                    continue;
                }

                // RequestId != 0 视为"预测执行"：本地先跑，等服务器确认再校正起始帧。
                Start(action, command.RequestId, command.RequestId != 0);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 有动作时尝试被新命令取消：窗口由当前动作提供，且必须指向命令里的那个动作；
        /// 条件成立就结束当前动作（Cancelled）并立刻起手新动作，ActionFrame 从 0 开始。
        ///
        /// ⚠ "狂点打不出伤害"就是这么来的：普攻 1001 的取消窗口是 [5,26) 且指向自己，
        /// 帧 5-26 之间每次按键都会把动作重置回帧 0，永远走不到帧 41 的命中窗口。
        /// </summary>
        private bool TryCancel(in CombatFrameCommands commands)
        {
            for (var i = 0; i < commands.Count; i++)
            {
                var command = commands[i];
                if (!_actions.TryGetValue(command.ActionId, out var action))
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

                    // 取消当前的动作
                    EndCurrent(CombatActionEndReason.Cancelled);
                    // 立即开始新动作
                    Start(action, command.RequestId, command.RequestId != 0);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 建立一次动作实例 —— "出手"真正发生的地方。
        /// InstanceId 每次起手唯一（表现层用它当 owner key，连招时能强制重播动画），
        /// StartSimulationFrame 用于服务器确认时重算 ActionFrame，IsPredicted 表示本地先跑等 Confirm 转正。
        ///
        /// 这里必须清 _acceptedHits：新动作不能复用上一招已命中的目标，否则连招第二下会打不出伤害。
        /// </summary>
        private void Start(CombatActionAsset actionAsset, long requestId, bool predicted)
        {
            var previous = CurrentAsset;
            CurrentAsset = actionAsset;
            _acceptedHits.Clear();
            Current = new CombatActionSnapshot
            {
                InstanceId = ++LocalSequence,
                RequestId = requestId,
                ActionId = actionAsset.ActionId,
                StartSimulationFrame = _simulationFrame,
                ActionFrame = 0,
                IsPredicted = predicted,
                IsConfirmed = !predicted,
                HasHitConfirmed = false,
            };
            // 广播"动作开始"，表现层据此把新的攻击 Timeline 铺到 Action 轨道上。
            // 事件只是通知不是命令：没人监听也不影响逻辑，无渲染环境照样跑。
            ActionChanged?.Invoke(new CombatActionChangedEvent(
                previous,
                actionAsset,
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
