using System;
using System.Collections.Generic;

namespace Ux
{
    /// <summary>
    /// 战斗模块入口。负责 BattleWorld 的生命周期，并把 SimulationClock 的逻辑帧分发给所有世界。
    /// 世界本身不是单例：主世界（场景战斗）和回放世界（战报校验）需要能同时存在，
    /// 所以这里管理的是一组实例，Main 只是其中固定键的那一个。
    /// </summary>
    public sealed class CombatMgr : Singleton<CombatMgr>
    {
        public const string MainWorldKey = "main";

        private readonly SortedDictionary<string, BattleWorld> _worlds = new();
        private BattleWorld[] _tickOrder = Array.Empty<BattleWorld>();
        private bool _orderDirty = true;

        /// <summary>
        /// 是否由 SimulationClock 自动驱动所有世界。关闭后需由外部手动 Tick，
        /// 供单元测试和服务器重放使用。
        /// </summary>
        public bool AutoDriveByClock { get; set; } = true;

        /// <summary>主战斗世界，首次访问时按当前时钟状态创建。</summary>
        public BattleWorld Main => GetOrCreateWorld(MainWorldKey);

        public int WorldCount => _worlds.Count;

        protected override void OnCreated()
        {
            SimulationClock.Ins.FrameAdvanced += OnFrameAdvanced;
        }

        public BattleWorld CreateWorld(string key, int frameRate, long startFrame = 0)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("战斗世界 key 不能为空", nameof(key));
            }

            if (_worlds.TryGetValue(key, out var existing))
            {
                Log.Warning($"战斗世界已存在，直接返回: key={key}, frame={existing.Frame}");
                return existing;
            }

            var world = new BattleWorld(key, frameRate, startFrame);
            _worlds.Add(key, world);
            _orderDirty = true;
            return world;
        }

        public BattleWorld GetWorld(string key)
        {
            return key != null && _worlds.TryGetValue(key, out var world) ? world : null;
        }

        public bool DestroyWorld(string key)
        {
            if (key == null || !_worlds.TryGetValue(key, out var world))
            {
                return false;
            }

            _worlds.Remove(key);
            _orderDirty = true;
            world.Clear();
            world.ClearSystems();
            return true;
        }

        /// <summary>关闭所有世界。切场景时调用，避免残留单位被继续推进。</summary>
        public void DestroyAllWorlds()
        {
            EnsureOrder();
            for (var i = 0; i < _tickOrder.Length; i++)
            {
                var world = _tickOrder[i];
                world.Clear();
                world.ClearSystems();
            }
            _worlds.Clear();
            _orderDirty = true;
        }


        private BattleWorld GetOrCreateWorld(string key)
        {
            if (_worlds.TryGetValue(key, out var world))
            {
                return world;
            }

            var clock = SimulationClock.Ins;
            return CreateWorld(key, clock.FrameRate, clock.CurrentFrame);
        }

        private void OnFrameAdvanced(long frame)
        {
            if (!AutoDriveByClock)
            {
                return;
            }

            EnsureOrder();
            for (var i = 0; i < _tickOrder.Length; i++)
            {
                _tickOrder[i].Tick(frame);
            }
        }

        private void EnsureOrder()
        {
            if (!_orderDirty)
            {
                return;
            }

            if (_tickOrder.Length != _worlds.Count)
            {
                _tickOrder = new BattleWorld[_worlds.Count];
            }

            var index = 0;
            foreach (var pair in _worlds)
            {
                _tickOrder[index++] = pair.Value;
            }
            _orderDirty = false;
        }
    }
}
