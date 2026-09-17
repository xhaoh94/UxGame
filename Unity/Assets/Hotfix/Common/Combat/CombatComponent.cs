using System;
using UnityEngine;

namespace Ux
{
    /// <summary>
    /// Unit 的战斗入口：输入命令、状态规则、动作生命周期、移动与 Timeline 表现协调。
    ///
    /// 它是 Unit（Unity 侧）与 BattleWorld（纯逻辑侧）之间的唯一桥接 —— 后者只认识 ICombatEntity，
    /// 这个组件就是那个实现。两条链路：移动是"输入 → 状态 → 位移"，攻击是"命令 → 动作 → 表现"，
    /// 两者在 TickLogic 里同帧汇合，所以能互相打断。分步说明见 OperateComponent.cs 类头。
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

        /// <summary>
        /// 移动链路 · 输入交接点：把 PathComponent 的方向向量转交给逻辑层。
        ///
        /// 它不是命令、不走 CombatCommandBuffer：移动是每帧都要的连续输入，新值直接覆盖旧值，
        /// 没有历史也没有帧号。位移结果靠 UNIT_UPDATE_POSITION 广播，不靠重放命令算出来。
        /// </summary>
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
        /// 请求执行指定动作
        /// </summary>
        public long RequestAction(int actionId, uint targetId = 0, Vector3 aimDirection = default)
        {
            var requestId = ++_nextRequestId;

            //下一帧触发
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

        // ── BattleWorld 一个逻辑帧的第 1 个阶段：Commands ──

        /// <summary>
        /// 攻击链路 · 取出：取走"帧号 == frame"那一桶命令，由 BattleWorld 的阶段 1 调用。
        /// frame 是传入的世界帧号（不是命令产生的时间点）；取走即删除，所以同一条命令不会被执行两次。
        /// </summary>
        public CombatFrameCommands ConsumeCommands(long frame)
        {
            return _commands.Consume(frame);
        }

        // ── BattleWorld 一个逻辑帧的第 2 个阶段：Actions ──
        // 一个单位一帧里全部的实质逻辑都在这一个方法里。

        /// <summary>
        /// 两条链路在这一帧汇合的地方 —— 移动判定、攻击执行、动画铺排都在这里，由 BattleWorld 的阶段 2 调用。
        ///
        /// 四步顺序不能换：Controller.Tick 推进状态与动作 → Resolve 决定这帧播什么 →
        /// Synchronize 切轨道 → TickMovement 结算位移。顺序错的表现：先算位移再推进动作，
        /// "这一帧刚起手的普攻"锁不住移动，会滑步（IsMovementBlocked 读的就是 Actions 的状态）。
        /// </summary>
        public void TickLogic(long frame, in CombatFrameCommands commands)
        {
            SimulationFrame = frame;
            if (Controller?.IsInitialized != true)
            {
                return;
            }

            // 两个入参对应两条链路的输入：MoveInput 是"当前值"（每帧现读），
            // commands 是阶段 1 刚从队列里取出的本帧命令。两者在这里汇合，所以移动和攻击能互相打断。
            Controller.Tick(frame, MoveInput, commands);

            // 攻击链路 · 表现落地：Base = 当前 Locomotion 的 Idle/Move 时间线，Action = 当前动作的攻击时间线。
            _framePlan = CombatTimelineResolver.Resolve(Profile, States, Actions, _presentationVariant);
            EnsureTimelinePlayer();
            _timelinePlayer?.Synchronize(_framePlan, Unit?.Viewer?.GetComponentInChildren<Animator>());
            _framePlanInitialized = true;

            // 移动链路收尾：只有 Locomotion 判定为 Move 且没被阻挡，这一帧才真的产生位移。
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

        /// <summary>
        /// 移动链路终点：位移结算。两重门禁都通过才动（没被阻挡 + Locomotion 是 Move）。
        ///
        /// 读的是"判定结果"而不是输入，所以想让技能期间不能跑只改 MovementPolicy 就行，这里不需要技能判断。
        /// 除以 FrameRate 是为了把位移按逻辑帧均匀摊开，保证帧率一致时结果完全可复现。
        /// </summary>
        private void TickMovement()
        {
            // 门禁 ①：死亡 / 被控制 / 当前动作锁移动 → 本帧不产生位移
            if (Controller.IsMovementBlocked || States.Locomotion != LocomotionState.Move)
            {
                return;
            }

            // 门禁 ②：Locomotion 已经是 Move，这里再确认输入确实有效（防御性检查）
            var input = Unit.Path?.MoveVector2 ?? Vector2.zero;
            if (input.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            // 二维输入 (x, y) 映射到世界水平面 (x, 0, z)
            var direction = new Vector3(input.x, 0, input.y).normalized;
            var frameRate = Math.Max(1, Profile.FrameRate);

            // 位移：方向 × (每秒移速 ÷ 帧率)，逐帧累加到 Position。
            // Position 的 setter 内部会把结果同步到 Viewer 的 transform.position。
            Unit.Position += direction * (Profile.MoveSpeedPerSecond / frameRate);

            // 转向：朝移动方向转，但每帧最多转 TurnDegreesPerSecond / 帧率 度，
            // 所以急转弯会有一个转身过程，不会瞬间贴面。
            var targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            Unit.Rotation = Quaternion.RotateTowards(
                Unit.Rotation,
                targetRotation,
                Profile.TurnDegreesPerSecond / frameRate);
        }

    }
}
