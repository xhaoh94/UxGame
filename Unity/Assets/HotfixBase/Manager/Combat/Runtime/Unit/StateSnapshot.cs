using System;

namespace Ux
{
    public readonly struct StateChangedEvent
    {
        public readonly StateLayer Layer;
        public readonly int PreviousStateId;
        public readonly int CurrentStateId;
        public readonly StateChangeReason Reason;
        public readonly long SimulationFrame;

        public StateChangedEvent(StateLayer layer, int previousStateId, int currentStateId, StateChangeReason reason, long simulationFrame)
        {
            Layer = layer;
            PreviousStateId = previousStateId;
            CurrentStateId = currentStateId;
            Reason = reason;
            SimulationFrame = simulationFrame;
        }
    }

    [Serializable]
    public struct StateLayerSnapshot
    {
        public StateLayer Layer;
        public int StateId;
        public int StateFrame;
    }

    [Serializable]
    public sealed class CombatStateMachineSnapshot
    {
        public long SimulationFrame;
        public StateLayerSnapshot Locomotion;
        public StateLayerSnapshot Control;
        public StateLayerSnapshot Life;

        public StateLayerSnapshot GetLayer(StateLayer layer)
        {
            return layer switch
            {
                StateLayer.Locomotion => Locomotion,
                StateLayer.Control => Control,
                StateLayer.Life => Life,
                _ => throw new ArgumentOutOfRangeException(nameof(layer), layer, null),
            };
        }
    }

    internal sealed class StateLayerRuntime
    {
        public int StateId;
        public int Frame;

        public void Set(int stateId, int frame = 0)
        {
            StateId = stateId;
            Frame = Math.Max(0, frame);
        }

        public void Advance(long deltaFrames)
        {
            if (StateId == 0 || deltaFrames <= 0)
            {
                return;
            }
            Frame = (int)Math.Min(int.MaxValue, Frame + deltaFrames);
        }

        public void Clear()
        {
            StateId = 0;
            Frame = 0;
        }
    }
}
