using System;
using UnityEngine;

namespace Ux
{
    /// <summary>
    /// Unit 的战斗入口：输入命令、代码状态规则、动作生命周期、移动和 Timeline 表现协调。
    /// </summary>
    public sealed class CombatComponent : Entity, IAwakeSystem, ICombatEntity
    {
        private readonly CombatCommandBuffer _commands = new();
        private long _nextRequestId;
        private CombatTimelinePlayer _timelinePlayer;
        private CombatTimelinePlan _framePlan;
        private bool _framePlanInitialized;
        private bool _registered;
        private string _presentationVariant = CombatStatePresentation.DefaultVariantId;

        public CombatController Controller { get; private set; }
        public UnitStateMachine States => Controller?.States;
        public CombatActionRunner Actions => Controller?.Actions;
        public CharacterCombatProfile Profile { get; private set; }
        public string ProfileName { get; private set; }
        /// <summary>当前外部指定的状态表现变体；不存在时由 Profile 回退到 default/优先级规则。</summary>
        public string PresentationVariant => _presentationVariant;

        /// <summary>本组件已推进到的逻辑帧。Unit.SimulationFrame 直接透传这个值。</summary>
        public long SimulationFrame { get; private set; }

        private Unit Unit => ParentAs<Unit>();

        #region ICombatEntity

        public long Id => Unit?.ID ?? 0;
        public bool IsCombatActive => Controller?.IsInitialized == true;
        public Vector2 MoveInput => Unit?.Path?.MoveVector2 ?? Vector2.zero;

        public Vector3 Position
        {
            get => Unit?.Position ?? Vector3.zero;
            set
            {
                var unit = Unit;
                if (unit != null)
                {
                    unit.Position = value;
                }
            }
        }

        public Quaternion Rotation
        {
            get => Unit?.Rotation ?? Quaternion.identity;
            set
            {
                var unit = Unit;
                if (unit != null)
                {
                    unit.Rotation = value;
                }
            }
        }

        #endregion

        public void OnAwake()
        {
            Controller = new CombatController();
            ProfileName = Unit.CombatProfileName;
            Profile = CombatProfileMgr.Ins.LoadAsset(ProfileName);
            if (Profile == null)
            {
                Log.Error($"找不到角色战斗配置: unit={Unit.ID}, profile={ProfileName}");
                AbortInitialization();
                return;
            }

            var clock = SimulationClock.Ins;
            if (clock.IsRunning && Profile.FrameRate != clock.FrameRate)
            {
                Log.Error($"战斗配置帧率必须与逻辑帧率一致: profile={Profile.name}, combat={Profile.FrameRate}, logic={clock.FrameRate}");
                AbortInitialization();
                return;
            }

            try
            {
                Controller.Initialize(Profile, Unit.ID, clock.CurrentFrame);
                RefreshTimeline(true);
                RegisterWorld();
            }
            catch (Exception exception)
            {
                Log.Error($"初始化角色战斗配置失败: unit={Unit.ID}, profile={Profile.name}\n{exception}");
                AbortInitialization();
            }
        }

        /// <summary>把本单位交给主战斗世界统一调度。逻辑帧不再由 Scene 逐个驱动。</summary>
        private void RegisterWorld()
        {
            if (_registered || CombatMgr.Ins.Main.Register(this))
            {
                _registered = true;
            }
        }

        /// <summary>
        /// 按 ActionId 请求技能。输入、网络和录像命令都只携带权威 ActionId，
        /// 不再通过命令类型或优先级推断技能。
        /// </summary>
        public long RequestAction(int actionId, uint targetId = 0, Vector3 aimDirection = default)
        {
            var requestId = ++_nextRequestId;
            var frame = SimulationClock.Ins.CurrentFrame + 1;
            _commands.Enqueue(new CombatCommand(
                requestId,
                frame,
                actionId,
                targetId,
                aimDirection));
            return requestId;
        }

        public void EnqueueCommand(in CombatCommand command)
        {
            _commands.Enqueue(command);
            _nextRequestId = Math.Max(_nextRequestId, command.RequestId);
        }

        // 阶段 Commands
        public CombatFrameCommands ConsumeCommands(long frame)
        {
            return _commands.Consume(frame);
        }

        // 阶段 Actions
        public void TickLogic(long frame, in CombatFrameCommands commands)
        {
            SimulationFrame = frame;
            if (Controller?.IsInitialized != true)
            {
                return;
            }

            Controller.Tick(frame, MoveInput, commands);

            // 保持原有先后：先按新状态同步基础表现与动作覆盖层，再结算本帧位移。
            _framePlan = CombatTimelineResolver.Resolve(Profile, States, Actions, _presentationVariant);
            EnsureTimelinePlayer();
            _timelinePlayer?.Synchronize(_framePlan, Unit?.Viewer?.GetComponentInChildren<Animator>());
            _framePlanInitialized = true;
            TickMovement();
        }

        // 阶段 Presentation
        public void TickPresentation(long frame)
        {
            var timeline = Unit?.Timeline;
            if (timeline == null)
            {
                return;
            }

            if (Controller?.IsInitialized != true)
            {
                return;
            }

            _timelinePlayer?.Evaluate(in _framePlan, true);
        }

        /// <summary>
        /// 单单位推进入口。世界正常调度时不走这里，
        /// 保留它方便在不建世界的情况下单独驱动一个单位（单元测试、编辑器预览）。
        /// </summary>
        public void TickSimulationFrame(long frame)
        {
            var commands = ConsumeCommands(frame);
            TickLogic(frame, commands);
            TickPresentation(frame);
        }

        public bool SetControlState(ControlState state)
        {
            if (Controller?.SetControl(state) != true)
            {
                return false;
            }
            RefreshTimeline(false);
            return true;
        }

        public bool SetLifeState(LifeState state)
        {
            if (Controller?.SetLife(state) != true)
            {
                return false;
            }
            RefreshTimeline(false);
            return true;
        }

        /// <summary>
        /// 设置状态表现变体。变体是表现上下文而非战斗逻辑状态，通常由皮肤、武器或角色表现层调用。
        /// 变体只影响基础状态表现；技能覆盖层保持使用当前动作资源与动作帧。
        /// </summary>
        public bool SetPresentationVariant(string variantId)
        {
            var normalized = CombatStatePresentation.NormalizeVariantId(variantId);
            if (string.Equals(normalized, _presentationVariant, StringComparison.Ordinal))
            {
                return false;
            }

            _presentationVariant = normalized;
            if (Controller?.IsInitialized == true)
            {
                // 变体只影响基础状态表现；动作层保持当前动作与当前帧。
                RefreshTimeline(false);
            }
            return true;
        }

        public bool ClearPresentationVariant()
        {
            return SetPresentationVariant(CombatStatePresentation.DefaultVariantId);
        }

        public bool ConfirmAction(long requestId, long authoritativeInstanceId, long authoritativeStartFrame)
        {
            if (Actions?.Confirm(
                    requestId,
                    authoritativeInstanceId,
                    authoritativeStartFrame) != true)
            {
                return false;
            }

            if (!Actions.HasAction)
            {
                States.SetAction(ActionState.Free, StateChangeReason.ActionEnded);
            }
            RefreshTimeline(true);
            return true;
        }

        public bool RejectAction(long requestId)
        {
            if (Actions?.Reject(requestId) != true || States?.IsInitialized != true)
            {
                return false;
            }

            States.SetAction(ActionState.Free, StateChangeReason.ActionEnded);
            RefreshTimeline(false);
            return true;
        }

        public UnitCombatSnapshot CaptureSnapshot()
        {
            return Controller?.CaptureSnapshot();
        }

        public void RestoreSnapshot(UnitCombatSnapshot snapshot)
        {
            if (Controller?.IsInitialized != true || snapshot?.StateMachine == null)
            {
                return;
            }

            Controller.RestoreSnapshot(snapshot);
            RefreshTimeline(true);
            _commands.DiscardBefore(snapshot.StateMachine.SimulationFrame + 1);
        }

        /// <summary>模型异步加载完成后恢复 Animator 绑定并重新采样当前状态/动作帧。</summary>
        public void RefreshTimelineBinding()
        {
            if (Controller?.IsInitialized != true)
            {
                return;
            }
            Unit.Timeline?.ClearBindings();
            RefreshTimeline(true);
        }

        protected override void OnDestroy()
        {
            if (_registered)
            {
                // 用 GetWorld 而不是 Main：场景销毁时会先收口所有世界，
                // 这里若走 Main 会把主世界重新创建出来，留下一个帧号停留在旧场景的空世界。
                CombatMgr.Ins.GetWorld(CombatMgr.MainWorldKey)?.Unregister(Id);
                _registered = false;
            }
            _commands.Clear();
            Controller?.Release();
            Controller = null;
            Profile = null;
            _presentationVariant = CombatStatePresentation.DefaultVariantId;
            _framePlan = default;
            _framePlanInitialized = false;
            _timelinePlayer?.Release();
            _timelinePlayer = null;
            base.OnDestroy();
        }

        private bool RefreshTimeline(bool force)
        {
            if (Controller?.IsInitialized != true || Unit.Timeline == null)
            {
                return false;
            }

            EnsureTimelinePlayer();
            var nextPlan = CombatTimelineResolver.Resolve(Profile, States, Actions, _presentationVariant);
            var changed = force || !_framePlanInitialized ||
                !string.Equals(nextPlan.Base.OwnerKey, _framePlan.Base.OwnerKey, StringComparison.Ordinal) ||
                !string.Equals(nextPlan.Action.OwnerKey, _framePlan.Action.OwnerKey, StringComparison.Ordinal) ||
                nextPlan.ExclusiveBase != _framePlan.ExclusiveBase;
            _framePlan = nextPlan;
            _timelinePlayer.Synchronize(_framePlan, Unit.Viewer?.GetComponentInChildren<Animator>(), force);
            _timelinePlayer.Evaluate(_framePlan, false);
            _framePlanInitialized = true;
            return changed;
        }

        private void EnsureTimelinePlayer()
        {
            if (_timelinePlayer == null && Unit?.Timeline != null)
            {
                _timelinePlayer = new CombatTimelinePlayer(Unit.Timeline);
            }
        }

        private void AbortInitialization()
        {
            Unit?.Timeline?.Stop();
            _timelinePlayer?.Release();
            _timelinePlayer = null;
            Controller?.Release();
            Controller = null;
            Profile = null;
        }

        private void TickMovement()
        {
            if (Controller.IsMovementBlocked || States.Locomotion != LocomotionState.Move)
            {
                return;
            }

            var input = Unit.Path?.MoveVector2 ?? Vector2.zero;
            if (input.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            var direction = new Vector3(input.x, 0, input.y).normalized;
            var frameRate = Math.Max(1, Profile.FrameRate);
            Unit.Position += direction * (Profile.MoveSpeedPerSecond / frameRate);

            var targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            Unit.Rotation = Quaternion.RotateTowards(
                Unit.Rotation,
                targetRotation,
                Profile.TurnDegreesPerSecond / frameRate);
        }

    }
}
