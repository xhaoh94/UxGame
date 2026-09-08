using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    [Serializable]
    public readonly struct CombatCommand
    {
        public readonly long RequestId;
        public readonly long SimulationFrame;
        public readonly CombatCommandType Type;
        public readonly int Parameter;
        public readonly uint TargetId;
        public readonly Vector3 AimDirection;

        public CombatCommand(
            long requestId,
            long simulationFrame,
            CombatCommandType type,
            int parameter = 0,
            uint targetId = 0,
            Vector3 aimDirection = default)
        {
            RequestId = requestId;
            SimulationFrame = Math.Max(0, simulationFrame);
            Type = type;
            Parameter = parameter;
            TargetId = targetId;
            AimDirection = aimDirection;
        }
    }

    public readonly struct CombatFrameCommands
    {
        private readonly IReadOnlyList<CombatCommand> _items;
        public static CombatFrameCommands Empty => new(Array.Empty<CombatCommand>());
        public int Count => _items?.Count ?? 0;
        public CombatCommand this[int index] => _items[index];

        public CombatFrameCommands(IReadOnlyList<CombatCommand> items)
        {
            _items = items ?? Array.Empty<CombatCommand>();
        }

        public bool Contains(CombatCommandType type)
        {
            if (_items == null)
            {
                return false;
            }
            for (var i = 0; i < _items.Count; i++)
            {
                if (_items[i].Type == type)
                {
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>按逻辑帧保存输入命令，供本地模拟、网络同步和录像共用。</summary>
    public sealed class CombatCommandBuffer
    {
        private readonly SortedDictionary<long, List<CombatCommand>> _frames = new();

        public void Enqueue(in CombatCommand command)
        {
            if (!_frames.TryGetValue(command.SimulationFrame, out var commands))
            {
                commands = new List<CombatCommand>();
                _frames.Add(command.SimulationFrame, commands);
            }
            commands.Add(command);
            commands.Sort(CompareCommand);
        }

        public CombatFrameCommands Consume(long simulationFrame)
        {
            if (!_frames.TryGetValue(simulationFrame, out var commands))
            {
                return CombatFrameCommands.Empty;
            }
            _frames.Remove(simulationFrame);
            return new CombatFrameCommands(commands);
        }

        public void DiscardBefore(long simulationFrame)
        {
            if (_frames.Count == 0)
            {
                return;
            }
            var remove = new List<long>();
            foreach (var pair in _frames)
            {
                if (pair.Key >= simulationFrame)
                {
                    break;
                }
                remove.Add(pair.Key);
            }
            foreach (var frame in remove)
            {
                _frames.Remove(frame);
            }
        }

        public void Clear()
        {
            _frames.Clear();
        }

        private static int CompareCommand(CombatCommand a, CombatCommand b)
        {
            var requestCompare = a.RequestId.CompareTo(b.RequestId);
            return requestCompare != 0 ? requestCompare : a.Type.CompareTo(b.Type);
        }
    }
}
