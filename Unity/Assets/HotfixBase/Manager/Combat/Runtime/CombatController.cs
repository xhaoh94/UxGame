using System;
using UnityEngine;

namespace Ux
{
    /// <summary>
    /// 战斗逻辑的集中式代码入口。宏观状态规则在这里固定执行顺序，资源只提供数值、动作和 Timeline。
    /// </summary>
    // 宏观战斗状态只在固定逻辑帧中按稳定顺序推进。
    public sealed class CombatController
    {
        private bool _grounded = true;

        public CharacterCombatProfile Profile { get; private set; }
        public UnitStateMachine States { get; } = new();
        public CombatActionRunner Actions { get; } = new();
        public bool IsInitialized => Profile != null && States.IsInitialized;
        public bool IsGrounded => _grounded;

        public bool CanStartActions =>
            States.Life == LifeState.Alive &&
            States.Control == ControlState.Normal;

        public bool IsMovementBlocked =>
            States.Life != LifeState.Alive ||
            States.Control != ControlState.Normal ||
            Actions.BlocksMovement;

        public void Initialize(
            CharacterCombatProfile profile,
            long ownerId,
            long simulationFrame)
        {
            Release();
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
            profile.ValidateRuntime();
            States.Initialize(ownerId, simulationFrame);
            Actions.Initialize(profile, simulationFrame);
        }

        public void Tick(
            long simulationFrame,
            Vector2 moveInput,
            in CombatFrameCommands commands)
        {
            if (!IsInitialized)
            {
                return;
            }

            States.AdvanceTo(simulationFrame);
            Actions.Tick(simulationFrame, commands, CanStartActions);
            States.SetAction(
                Actions.HasAction ? ActionState.Executing : ActionState.Free,
                Actions.HasAction
                    ? StateChangeReason.ActionStarted
                    : StateChangeReason.ActionEnded);

            if (!_grounded)
            {
                States.SetLocomotion(LocomotionState.Airborne);
            }
            else if (IsMovementBlocked || moveInput.sqrMagnitude <= 0.0001f)
            {
                States.SetLocomotion(LocomotionState.Idle);
            }
            else
            {
                States.SetLocomotion(LocomotionState.Move);
            }
        }

        public void SetGrounded(bool grounded)
        {
            _grounded = grounded;
        }

        public bool SetControl(ControlState state)
        {
            if (!IsInitialized)
            {
                return false;
            }
            var changed = States.SetControl(state, StateChangeReason.ExternalRequest);
            if (state != ControlState.Normal)
            {
                Actions.Interrupt();
                States.SetAction(ActionState.Free, StateChangeReason.ActionEnded);
                States.SetLocomotion(LocomotionState.Idle);
            }
            return changed;
        }

        public bool SetLife(LifeState state)
        {
            if (!IsInitialized)
            {
                return false;
            }
            var changed = States.SetLife(state, StateChangeReason.ExternalRequest);
            if (state == LifeState.Dead)
            {
                Actions.Interrupt();
                States.SetAction(ActionState.Free, StateChangeReason.ActionEnded);
                States.SetLocomotion(LocomotionState.Idle);
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
                StateMachine = States.CaptureSnapshot(),
                Action = Actions.Current,
                HasAction = Actions.HasAction,
                AcceptedHits = Actions.CaptureAcceptedHits(),
                LocalActionSequence = Actions.LocalSequence,
                IsGrounded = _grounded,
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
            _grounded = snapshot.IsGrounded;
            States.RestoreSnapshot(snapshot.StateMachine);
            Actions.Restore(
                snapshot.Action,
                snapshot.HasAction,
                snapshot.StateMachine.SimulationFrame,
                snapshot.AcceptedHits,
                snapshot.LocalActionSequence);
        }

        public void Release()
        {
            Actions.Clear();
            States.Release();
            Profile = null;
            _grounded = true;
        }
    }
}
