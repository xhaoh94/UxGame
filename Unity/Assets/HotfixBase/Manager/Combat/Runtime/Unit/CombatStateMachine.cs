using System;

namespace Ux
{
    /// <summary>
    /// 代码驱动的确定性宏观状态容器。它不求值资源条件，也不决定攻击内容；
    /// CombatController 在固定逻辑帧中集中提交 Life、Control、Locomotion 状态。
    /// </summary>
    public sealed class CombatStateMachine
    {
        private static readonly StateLayer[] Layers =
        {
            StateLayer.Locomotion,
            StateLayer.Control,
            StateLayer.Life,
        };

        private readonly StateLayerRuntime[] _layers =
        {
            new(), new(), new(),
        };

        public event Action<StateChangedEvent> StateChanged;

        public long OwnerId { get; private set; }
        public long SimulationFrame { get; private set; }
        public bool IsInitialized { get; private set; }

        public LocomotionState Locomotion =>
            (LocomotionState)GetCurrentStateId(StateLayer.Locomotion);
        public ControlState Control =>
            (ControlState)GetCurrentStateId(StateLayer.Control);
        public LifeState Life =>
            (LifeState)GetCurrentStateId(StateLayer.Life);

        public void Initialize(long ownerId, long simulationFrame = 0)
        {
            Release();
            OwnerId = ownerId;
            SimulationFrame = Math.Max(0, simulationFrame);
            IsInitialized = true;

            ChangeState(StateLayer.Locomotion, LocomotionState.Idle.ToId(), StateChangeReason.Initialize);
            ChangeState(StateLayer.Control, ControlState.Normal.ToId(), StateChangeReason.Initialize);
            ChangeState(StateLayer.Life, LifeState.Alive.ToId(), StateChangeReason.Initialize);
        }

        /// <summary>在本逻辑帧执行规则前推进所有已存在状态的状态帧。</summary>
        public void AdvanceTo(long simulationFrame)
        {
            if (!IsInitialized)
            {
                return;
            }

            var targetFrame = Math.Max(0, simulationFrame);
            if (targetFrame <= SimulationFrame)
            {
                return;
            }

            var delta = targetFrame - SimulationFrame;
            SimulationFrame = targetFrame;
            foreach (var runtime in _layers)
            {
                runtime.Advance(delta);
            }
        }

        public bool SetLocomotion(LocomotionState state, StateChangeReason reason = StateChangeReason.CodeRule)
        {
            return ChangeState(StateLayer.Locomotion, state.ToId(), reason);
        }

        public bool SetControl(ControlState state, StateChangeReason reason = StateChangeReason.ExternalRequest)
        {
            return ChangeState(StateLayer.Control, state.ToId(), reason);
        }

        public bool SetLife(LifeState state, StateChangeReason reason = StateChangeReason.ExternalRequest)
        {
            return ChangeState(StateLayer.Life, state.ToId(), reason);
        }

        public int GetCurrentStateId(StateLayer layer)
        {
            return _layers[LayerIndex(layer)].StateId;
        }

        public int GetStateFrame(StateLayer layer)
        {
            return _layers[LayerIndex(layer)].Frame;
        }

        public bool IsPlaying(StateLayer layer, int stateId)
        {
            return GetCurrentStateId(layer) == stateId;
        }

        public CombatStateMachineSnapshot CaptureSnapshot()
        {
            return new CombatStateMachineSnapshot
            {
                SimulationFrame = SimulationFrame,
                Locomotion = CaptureLayer(StateLayer.Locomotion),
                Control = CaptureLayer(StateLayer.Control),
                Life = CaptureLayer(StateLayer.Life),
            };
        }

        public void RestoreSnapshot(CombatStateMachineSnapshot snapshot)
        {
            if (!IsInitialized || snapshot == null)
            {
                return;
            }

            SimulationFrame = Math.Max(0, snapshot.SimulationFrame);
            foreach (var layer in Layers)
            {
                var value = snapshot.GetLayer(layer);
                if (value.Layer != layer || !CombatStateId.IsDefined(layer, value.StateId))
                {
                    throw new InvalidOperationException(
                        $"状态快照无效: layer={layer}, state={value.StateId}");
                }

                var runtime = _layers[LayerIndex(layer)];
                var previous = runtime.StateId;
                runtime.Set(value.StateId, value.StateFrame);
                if (previous != value.StateId)
                {
                    StateChanged?.Invoke(new StateChangedEvent(
                        layer,
                        previous,
                        value.StateId,
                        StateChangeReason.SnapshotRestore,
                        SimulationFrame));
                }
            }
        }

        public void Release()
        {
            OwnerId = 0;
            SimulationFrame = 0;
            IsInitialized = false;
            foreach (var runtime in _layers)
            {
                runtime.Clear();
            }
            StateChanged = null;
        }

        private StateLayerSnapshot CaptureLayer(StateLayer layer)
        {
            var runtime = _layers[LayerIndex(layer)];
            return new StateLayerSnapshot
            {
                Layer = layer,
                StateId = runtime.StateId,
                StateFrame = runtime.Frame,
            };
        }

        private bool ChangeState(StateLayer layer, int stateId, StateChangeReason reason)
        {
            if (!IsInitialized || !CombatStateId.IsDefined(layer, stateId))
            {
                return false;
            }

            var runtime = _layers[LayerIndex(layer)];
            if (runtime.StateId == stateId)
            {
                return false;
            }

            var previous = runtime.StateId;
            runtime.Set(stateId, 0);
            StateChanged?.Invoke(new StateChangedEvent(
                layer,
                previous,
                stateId,
                reason,
                SimulationFrame));
            return true;
        }

        private static int LayerIndex(StateLayer layer)
        {
            return layer switch
            {
                StateLayer.Locomotion => 0,
                StateLayer.Control => 1,
                StateLayer.Life => 2,
                _ => throw new ArgumentOutOfRangeException(nameof(layer), layer, null),
            };
        }
    }
}
