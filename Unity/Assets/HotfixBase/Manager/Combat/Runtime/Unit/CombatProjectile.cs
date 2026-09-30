using System;
using UnityEngine;

namespace Ux
{
    /// <summary>
    /// 投射物：一个不出招、只位移的战斗单位。
    ///
    /// 复用 CombatController，所以攻击判定（HitboxSystem 的圆心就是实体自己的 Position）、
    /// 伤害、增益全部免费可用 —— 它的"招"就是一段覆盖全程的飞行攻击判定。
    /// 位移直接写 Position，不经过 Locomotion：投射物没有移动输入，朝向由生成那一刻定格。
    /// </summary>
    public sealed class CombatProjectile : ICombatEntity, ICombatProjectile
    {
        /// <summary>与 HitboxSystem.MillimetersPerUnit 同一单位域：1 世界单位 = 1000 毫米。</summary>
        private const float MillimetersPerUnit = 1000f;

        private readonly CombatSpawnProfile spawn;
        private readonly CombatCommand[] commandCache = new CombatCommand[1];
        private readonly long spawnFrame;
        private bool started;

        public CombatProjectile(long id, CombatSpawnProfile spawn, Vector3 position, Quaternion rotation, long spawnFrame, long spawnerId = 0)
        {
            Id = id;
            this.spawn = spawn ?? throw new ArgumentNullException(nameof(spawn));
            this.spawnFrame = spawnFrame;
            SpawnerId = spawnerId;
            Position = position;
            Rotation = rotation;

            Controller = new CombatController();
            Controller.Initialize(spawn.FlightProfile, id, spawnFrame);
        }

        public long Id { get; }
        public long SpawnerId { get; }
        public CombatController Controller { get; }
        public bool IsCombatActive => Controller.IsInitialized;

        /// <summary>投射物不能被其它攻击选中：它没有阵营，也不该被刀光砍下来。</summary>
        public bool CanBeTargeted => false;

        public Vector2 MoveInput => Vector2.zero;
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }

        /// <summary>生成物自己的配置，表现层据此找视觉资源。</summary>
        public CombatSpawnProfile Spawn => spawn;

        public CombatFrameCommands ConsumeCommands(long frame)
        {
            if (started)
            {
                return CombatFrameCommands.Empty;
            }
            started = true;

            // 起手走命令队列而不是直接调 Runner：命令是唯一入口，
            // "投射物起手"和"玩家出手"因此在 Runner 里是同一条路径。
            commandCache[0] = new CombatCommand(
                spawnFrame,
                frame,
                spawn.FlightActionId,
                0,
                Rotation * Vector3.forward);
            return new CombatFrameCommands(commandCache);
        }

        public void TickLogic(long frame, in CombatFrameCommands commands)
        {
            Controller.Tick(frame, MoveInput, commands);

            if (!Controller.IsInitialized || Controller.StateMachine.Life != LifeState.Alive)
            {
                return;
            }

            // HitboxSystem 的命中圆心就是这个 Position，所以位移只能由本类独占写入。
            Position += Rotation * Vector3.forward * (spawn.SpeedMillimetersPerFrame / MillimetersPerUnit);

            if (frame - spawnFrame >= spawn.MaxLifetimeFrames)
            {
                // 只标记死亡，不从世界摘除：本帧正在遍历实体，摘除留给下一帧的回收。
                Controller.SetLife(LifeState.Dead);
            }
        }

        public void TickPresentation(long frame)
        {
            // 表现由表现层的订阅者自行同步，逻辑侧不感知 GameObject。
        }
    }
}
