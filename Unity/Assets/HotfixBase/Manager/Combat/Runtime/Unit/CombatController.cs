using System;
using UnityEngine;

namespace Ux
{
    /// <summary>
    /// 战斗逻辑的集中式代码入口。宏观状态规则在这里固定执行顺序，资源只提供数值、动作和 Timeline。
    ///
    /// 一个单位的逻辑总装：CombatStateMachine 管宏观状态，CombatActionRunner 管招式生命周期，
    /// 每帧调用顺序由 Tick 固定。不引用任何 Unity 组件，服务器和战报校验里能直接跑。
    /// </summary>
    // 宏观战斗状态只在固定逻辑帧中按稳定顺序推进。
    public sealed class CombatController
    {
        public CharacterCombatProfile Profile { get; private set; }
        public CombatStateMachine StateMachine { get; } = new();
        public CombatActionRunner ActionRunner { get; } = new();

        /// <summary>单位属性 —— ⚠ 最小版本，只有生命值。不参与 Tick：由伤害阶段改写、死亡阶段读取。</summary>
        public AttributeSet Attributes { get; } = new();

        /// <summary>增益容器。同上，由增益阶段结算、死亡阶段清空。</summary>
        public CombatBuffContainer Buffs { get; } = new();

        public bool IsInitialized => Profile != null && StateMachine.IsInitialized;
        public bool IsGrounded { get; private set; } = true;

        /// <summary>能不能开始新动作 = 活着 + 控制正常。注意它**不**看当前有没有动作。</summary>
        public bool CanStartActions =>
            StateMachine.Life == LifeState.Alive &&
            StateMachine.Control == ControlState.Normal;

        /// <summary>
        /// 能不能移动 = 活着 + 控制正常 + 当前动作没有锁移动。
        /// 最后一个条件来自配置 CombatActionAsset.movementPolicy：
        ///   Allow(0) → 不锁，攻击期间照样能跑
        ///   Block(1) → 锁，一旦进入该动作，Locomotion 会被强行压成 Idle 并停止位移
        /// </summary>
        public bool IsMovementBlocked =>
            StateMachine.Life != LifeState.Alive ||
            StateMachine.Control != ControlState.Normal ||
            ActionRunner.BlocksMovement;

        public void Initialize(CharacterCombatProfile profile, long ownerId, long simulationFrame)
        {
            Release();
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
            profile.ValidateRuntime();
            StateMachine.Initialize(ownerId, simulationFrame);
            ActionRunner.Initialize(profile, simulationFrame);

            // 属性与增益都从配置重新起算。Initialize 可以被重复调用（换皮肤/热重载场景），
            // 所以这里必须是把旧状态丢掉重建，而不是"在旧值上继续"。
            Attributes.Initialize(profile.MaxHp);
            Buffs.Clear();
        }

        public void Tick(long simulationFrame, Vector2 moveInput, in CombatFrameCommands commands)
        {
            if (!IsInitialized)
            {
                return;
            }

            //这里只是推进了状态帧号，状态本身的改变要在 CombatActionRunner 里做。
            StateMachine.AdvanceTo(simulationFrame);

            //推进动作命令（内部有可能打断当前的动作，也有可能直接起新动作）
            ActionRunner.Tick(simulationFrame, commands, CanStartActions);

            if (!IsGrounded)
            {
                StateMachine.SetLocomotion(LocomotionState.Airborne);
            }
            else if (IsMovementBlocked || moveInput.sqrMagnitude <= 0.0001f)
            {
                StateMachine.SetLocomotion(LocomotionState.Idle);
            }
            else
            {
                StateMachine.SetLocomotion(LocomotionState.Move);
            }
        }

        public void SetGrounded(bool grounded)
        {
            IsGrounded = grounded;
        }

        public bool SetControl(ControlState state)
        {
            if (!IsInitialized)
            {
                return false;
            }
            var changed = StateMachine.SetControl(state, StateChangeReason.ExternalRequest);
            if (state != ControlState.Normal)
            {
                ActionRunner.Interrupt();
                StateMachine.SetLocomotion(LocomotionState.Idle);
            }
            return changed;
        }

        public bool SetLife(LifeState state)
        {
            if (!IsInitialized)
            {
                return false;
            }
            var changed = StateMachine.SetLife(state, StateChangeReason.ExternalRequest);
            if (state == LifeState.Dead)
            {
                ActionRunner.Interrupt();
                StateMachine.SetLocomotion(LocomotionState.Idle);
            }
            return changed;
        }

        public UnitCombatSnapshot CaptureSnapshot()
        {
            if (!IsInitialized)
            {
                return null;
            }
            return new UnitCombatSnapshot
            {
                Version = UnitCombatSnapshot.CurrentVersion,
                StateMachine = StateMachine.CaptureSnapshot(),
                Action = ActionRunner.Current,
                HasAction = ActionRunner.HasAction,
                AcceptedHits = ActionRunner.CaptureAcceptedHits(),
                LocalActionSequence = ActionRunner.LocalSequence,
                IsGrounded = IsGrounded,
                Attributes = Attributes.CaptureSnapshot(),
                Buffs = Buffs.CaptureSnapshot(),
            };
        }

        public void RestoreSnapshot(UnitCombatSnapshot snapshot)
        {
            if (!IsInitialized || snapshot?.StateMachine == null)
            {
                return;
            }
            if (snapshot.Version != UnitCombatSnapshot.CurrentVersion)
            {
                throw new InvalidOperationException(
                    $"不支持的战斗快照版本: {snapshot.Version}");
            }
            IsGrounded = snapshot.IsGrounded;
            StateMachine.RestoreSnapshot(snapshot.StateMachine);

            // 属性与增益必须和状态机、动作一起回滚。少还原任何一项，
            // 回滚后的世界就会出现"状态回到了过去、血量还停在未来"这种裂开的状态。
            Attributes.RestoreSnapshot(in snapshot.Attributes);
            Buffs.RestoreSnapshot(snapshot.Buffs);

            ActionRunner.Restore(
                snapshot.Action,
                snapshot.HasAction,
                snapshot.StateMachine.SimulationFrame,
                snapshot.AcceptedHits,
                snapshot.LocalActionSequence);
        }

        public void Release()
        {
            ActionRunner.Clear();
            StateMachine.Release();
            Attributes.Release();
            Buffs.Clear();
            Profile = null;
            IsGrounded = true;
        }
    }
}
