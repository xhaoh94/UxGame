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
        private string _timelineOwnerKey = string.Empty;
        private TimelineSelection _frameSelection;
        private bool _frameTimelineSwitched;
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
                return;
            }

            var clock = SimulationClock.Ins;
            if (clock.IsRunning && Profile.FrameRate != clock.FrameRate)
            {
                Log.Error($"战斗配置帧率必须与逻辑帧率一致: profile={Profile.name}, combat={Profile.FrameRate}, logic={clock.FrameRate}");
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
        public long RequestAction(
            int actionId,
            uint targetId = 0,
            Vector3 aimDirection = default)
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
            _frameTimelineSwitched = false;
            if (Controller?.IsInitialized != true)
            {
                return;
            }

            Controller.Tick(frame, MoveInput, commands);

            // 保持原有先后：先按新状态切好表现选段，再结算本帧位移。
            _frameSelection = ResolveTimelineOwner();
            _frameTimelineSwitched = RefreshTimeline(false, _frameSelection);
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
                timeline.Tick();
                return;
            }

            if (_frameTimelineSwitched)
            {
                // 新动作在进入逻辑帧只执行第 0 帧；状态表现只做绝对帧采样，不触发 Gameplay Event。
                if (_frameSelection.IsAction && _frameSelection.Frame == 0)
                {
                    timeline.TickCurrentFrame();
                }
                return;
            }
            timeline.Tick();
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
        /// 技能 Timeline 仍优先于状态表现；动作结束后会使用新的变体解析宏观状态 Timeline。
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
                // 变体只影响状态表现；技能播放期间保持原有 Action Timeline 和当前帧，
                // 只有实际选段发生变化时才重新绑定/采样。
                var selection = ResolveTimelineOwner();
                if (RefreshTimeline(false, selection))
                {
                    Unit?.Timeline?.Set(selection.Frame, false);
                }
            }
            return true;
        }

        public bool ClearPresentationVariant()
        {
            return SetPresentationVariant(CombatStatePresentation.DefaultVariantId);
        }

        public bool ConfirmAction(
            long requestId,
            long authoritativeInstanceId,
            long authoritativeStartFrame)
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
            var selection = ResolveTimelineOwner();
            RefreshTimeline(true, selection);
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
            var selection = ResolveTimelineOwner();
            RefreshTimeline(true, selection);
            Unit.Timeline?.Set(selection.Frame, false);
            _commands.DiscardBefore(snapshot.StateMachine.SimulationFrame + 1);
        }

        /// <summary>模型异步加载完成后恢复 Animator 绑定并重新采样当前状态/动作帧。</summary>
        public void RefreshTimelineBinding()
        {
            if (Controller?.IsInitialized != true)
            {
                return;
            }
            var selection = ResolveTimelineOwner();
            RefreshTimeline(true, selection);
            Unit.Timeline?.Set(selection.Frame, false);
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
            _timelineOwnerKey = string.Empty;
            base.OnDestroy();
        }

        private bool RefreshTimeline(bool force)
        {
            return RefreshTimeline(force, ResolveTimelineOwner());
        }

        private bool RefreshTimeline(bool force, in TimelineSelection selection)
        {
            if (Controller?.IsInitialized != true || Unit.Timeline == null)
            {
                return false;
            }
            if (!force && string.Equals(
                    selection.OwnerKey,
                    _timelineOwnerKey,
                    StringComparison.Ordinal))
            {
                return false;
            }

            _timelineOwnerKey = selection.OwnerKey;
            if (selection.Asset == null)
            {
                Unit.Timeline.Stop();
                return true;
            }

            var animationTrack = selection.Asset.FindTrack<AnimationTrackAsset>();
            var animator = Unit.Viewer?.GetComponentInChildren<Animator>();
            if (animationTrack != null && animator != null)
            {
                Unit.Timeline.SetBinding(animationTrack, animator);
            }
            Unit.Timeline.Play(selection.Asset);
            Unit.Timeline.Set(selection.Frame, false);
            return true;
        }

        private TimelineSelection ResolveTimelineOwner()
        {
            if (Controller?.IsInitialized != true)
            {
                return default;
            }

            var lifeId = States.GetCurrentStateId(StateLayer.Life);
            var presentation = Profile.GetStatePresentation(
                StateLayer.Life,
                lifeId,
                _presentationVariant);
            if (presentation?.Timeline != null)
            {
                return TimelineSelection.ForState(
                    StateLayer.Life,
                    lifeId,
                    States.GetStateFrame(StateLayer.Life),
                    presentation);
            }

            var controlId = States.GetCurrentStateId(StateLayer.Control);
            presentation = Profile.GetStatePresentation(
                StateLayer.Control,
                controlId,
                _presentationVariant);
            if (presentation?.Timeline != null)
            {
                return TimelineSelection.ForState(
                    StateLayer.Control,
                    controlId,
                    States.GetStateFrame(StateLayer.Control),
                    presentation);
            }

            if (Actions.HasAction)
            {
                var actionTimeline = Profile.GetActionTimeline(Actions.Current.ActionId);
                if (actionTimeline != null)
                {
                    return TimelineSelection.ForAction(
                        Actions.Current.InstanceId,
                        Actions.Current.ActionFrame,
                        actionTimeline);
                }
            }

            var locomotionId = States.GetCurrentStateId(StateLayer.Locomotion);
            presentation = Profile.GetStatePresentation(
                StateLayer.Locomotion,
                locomotionId,
                _presentationVariant);
            return TimelineSelection.ForState(
                StateLayer.Locomotion,
                locomotionId,
                States.GetStateFrame(StateLayer.Locomotion),
                presentation);
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

        private readonly struct TimelineSelection
        {
            public readonly string OwnerKey;
            public readonly TimelineAsset Asset;
            public readonly int Frame;
            public readonly bool IsAction;

            private TimelineSelection(
                string ownerKey,
                TimelineAsset asset,
                int frame,
                bool isAction)
            {
                OwnerKey = ownerKey ?? string.Empty;
                Asset = asset;
                Frame = Math.Max(0, frame);
                IsAction = isAction;
            }

            public static TimelineSelection ForState(
                StateLayer layer,
                int stateId,
                int frame,
                CombatStatePresentation presentation)
            {
                var asset = presentation?.Timeline;
                var presentationKey = presentation == null
                    ? "none"
                    : $"{presentation.StableId}:{presentation.VariantId}";
                return new TimelineSelection(
                    $"state:{(int)layer}:{stateId}:{presentationKey}",
                    asset,
                    frame,
                    false);
            }

            public static TimelineSelection ForAction(
                long instanceId,
                int frame,
                TimelineAsset asset)
            {
                return new TimelineSelection(
                    $"action:{instanceId}",
                    asset,
                    frame,
                    true);
            }
        }
    }
}
