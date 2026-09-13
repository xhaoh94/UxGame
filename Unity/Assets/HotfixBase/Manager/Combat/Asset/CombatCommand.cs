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
        public readonly int ActionId;
        public readonly uint TargetId;
        public readonly Vector3 AimDirection;

        public CombatCommand(long requestId, long simulationFrame, int actionId, uint targetId = 0, Vector3 aimDirection = default)
        {
            RequestId = requestId;
            SimulationFrame = Math.Max(0, simulationFrame);
            ActionId = actionId;
            TargetId = targetId;
            AimDirection = aimDirection;
        }

        internal static int Compare(CombatCommand a, CombatCommand b)
        {
            var result = a.SimulationFrame.CompareTo(b.SimulationFrame);
            if (result != 0)
            {
                return result;
            }
            result = a.RequestId.CompareTo(b.RequestId);
            if (result != 0)
            {
                return result;
            }
            result = a.ActionId.CompareTo(b.ActionId);
            if (result != 0)
            {
                return result;
            }
            result = a.TargetId.CompareTo(b.TargetId);
            if (result != 0)
            {
                return result;
            }
            result = CompareFloatBits(a.AimDirection.x, b.AimDirection.x);
            if (result != 0)
            {
                return result;
            }
            result = CompareFloatBits(a.AimDirection.y, b.AimDirection.y);
            return result != 0
                ? result
                : CompareFloatBits(a.AimDirection.z, b.AimDirection.z);
        }

        private static int CompareFloatBits(float a, float b)
        {
            var left = unchecked((uint)BitConverter.SingleToInt32Bits(a));
            var right = unchecked((uint)BitConverter.SingleToInt32Bits(b));
            return left.CompareTo(right);
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
            if (items == null || items.Count == 0)
            {
                _items = Array.Empty<CombatCommand>();
                return;
            }

            var sorted = new List<CombatCommand>(items);
            sorted.Sort(CombatCommand.Compare);
            _items = sorted;
        }

        public bool ContainsAction(int actionId)
        {
            if (_items == null)
            {
                return false;
            }
            for (var i = 0; i < _items.Count; i++)
            {
                if (_items[i].ActionId == actionId)
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

    }
}
