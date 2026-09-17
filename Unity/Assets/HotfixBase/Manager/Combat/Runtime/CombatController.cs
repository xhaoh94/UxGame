using System;
using UnityEngine;

namespace Ux
{
    /// <summary>
    /// 战斗逻辑的集中式代码入口。宏观状态规则在这里固定执行顺序，资源只提供数值、动作和 Timeline。
    ///
    /// 一个单位的逻辑总装：UnitStateMachine 管宏观状态，CombatActionRunner 管招式生命周期，
    /// 每帧调用顺序由 Tick 固定。不引用任何 Unity 组件，服务器和战报校验里能直接跑。
    /// </summary>
    // 宏观战斗状态只在固定逻辑帧中按稳定顺序推进。
    public sealed class CombatController
    {
        private bool _grounded = true;

        public CharacterCombatProfile Profile { get; private set; }
        public UnitStateMachine States { get; } = new();
        public CombatActionRunner Actions { get; } = new();

        /// <summary>单位属性 —— ⚠ 最小版本，只有生命值。不参与 Tick：由伤害阶段改写、死亡阶段读取。</summary>
        public AttributeSet Attributes { get; } = new();

        /// <summary>增益容器。同上，由增益阶段结算、死亡阶段清空。</summary>
        public CombatBuffContainer Buffs { get; } = new();

        public bool IsInitialized => Profile != null && States.IsInitialized;
        public bool IsGrounded => _grounded;

        /// <summary>能不能开始新动作 = 活着 + 控制正常。注意它**不**看当前有没有动作。</summary>
        public bool CanStartActions =>
            States.Life == LifeState.Alive &&
            States.Control == ControlState.Normal;

        /// <summary>
        /// 能不能移动 = 活着 + 控制正常 + 当前动作没有锁移动。
        /// 最后一个条件来自 CombatActionAsset.movementPolicy：
        ///   Allow(0) → 不锁，攻击期间照样能跑
        ///   Block(1) → 锁，一旦进入该动作，Locomotion 会被强行压成 Idle 并停止位移
        /// </summary>
        public bool IsMovementBlocked =>
            States.Life != LifeState.Alive ||
            States.Control != ControlState.Normal ||
            Actions.BlocksMovement;

        public void Initialize(CharacterCombatProfile profile, long ownerId, long simulationFrame)
        {
            Release();
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
            profile.ValidateRuntime();
            States.Initialize(ownerId, simulationFrame);
            Actions.Initialize(profile, simulationFrame);

            // 属性与增益都从配置重新起算。Initialize 可以被重复调用（换皮肤/热重载场景），
            // 所以这里必须是把旧状态丢掉重建，而不是"在旧值上继续"。
            Attributes.Initialize(profile.MaxHp);
            Buffs.Clear();
        }

        /// <summary>
        /// 单位每逻辑帧的唯一入口，由 <see cref="CombatComponent.TickLogic"/> 调用
        /// （即 BattleWorld 一个逻辑帧的第 2 个阶段）。
        ///
        /// 三个入参全都来自逻辑帧，没有一个是现从 Unity 组件身上取的：
        ///   simulationFrame — 当前逻辑帧号
        ///   moveInput       — 本帧移动输入（来自 PathComponent.MoveVector2）
        ///   commands        — 本帧命令（来自 CombatCommandBuffer）
        ///
        /// moveInput 和 commands 虽然都是"本帧的输入"，但性质不同，别混为一谈：
        ///   moveInput 是**当前值**：一个可覆盖的方向字段，没有历史，逻辑层每帧主动来读。
        ///   commands  是**事件流**：每条带帧号和 RequestId，按键时入队、到帧才取出、取走即删。
        /// 所以只有 commands 需要队列和帧号 —— 队列存在的意义就是让离散事件能对齐到某一帧。
        ///
        /// 执行顺序固定为"先推进动作、后判定移动"，顺序不能换，原因见第 4 步。
        /// </summary>
        public void Tick(long simulationFrame, Vector2 moveInput, in CombatFrameCommands commands)
        {
            if (!IsInitialized)
            {
                return;
            }

            // ── 1. 把所有状态层的帧计数推到当前帧 ──
            // 表现层稍后要用这个帧号去求值 Idle/Move 的 Timeline，所以必须赶在判定之前推进。
            States.AdvanceTo(simulationFrame);

            // ── 2. 推进动作生命周期（攻击链路的执行段）──
            // 有动作时：拿本帧命令去比对取消窗口（能不能被下一招打断）；
            // 没动作时：拿本帧命令去尝试起手。
            // canStartActions 为 false（死亡/被控）时，内部会把当前动作直接打断。
            Actions.Tick(simulationFrame, commands, CanStartActions);

            // ── 3. 把"有没有动作"同步到 Action 状态层（Free / Executing）──
            // 这一层只是给表现层和外部查询看的标签，真正的动作数据在 CombatActionRunner 里。
            States.SetAction(Actions.HasAction ? ActionState.Executing : ActionState.Free, Actions.HasAction ? StateChangeReason.ActionStarted : StateChangeReason.ActionEnded);

            // ── 4. 判定 Locomotion，三选一，优先级从上到下（移动链路的判定段）──
            if (!_grounded)
            {
                // 离地优先：空中不区分 Idle/Move，交给专门的跳落表现去演
                States.SetLocomotion(LocomotionState.Airborne);
            }
            else if (IsMovementBlocked || moveInput.sqrMagnitude <= 0.0001f)
            {
                // 被阻挡（死亡 / 被控 / 当前动作锁移动）或输入接近零 → Idle。
                // 0.0001f 是平方阈值，对应向量长度 0.01，用来滤掉摇杆回中时的微小抖动。
                //
                // 注意 IsMovementBlocked 里含 Actions.BlocksMovement，而动作刚在第 2 步推进过，
                // 所以"这一帧刚起手的锁移动技能"能在同一帧立刻把单位压成 Idle —— 不需要
                // 任何一行额外的"打断位移"代码。这就是第 2 步必须排在前面的原因。
                States.SetLocomotion(LocomotionState.Idle);
            }
            else
            {
                // 有有效输入且没被阻挡 → Move。
                // 这里就是"站定"和"走路"的唯一分界点：它只看**有没有输入**，不看速度，
                // 项目里也不存在 Run 状态。跑步只体现为表现层切到了哪条 Timeline。
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
            _grounded = snapshot.IsGrounded;
            States.RestoreSnapshot(snapshot.StateMachine);

            // 属性与增益必须和状态机、动作一起回滚。少还原任何一项，
            // 回滚后的世界就会出现"状态回到了过去、血量还停在未来"这种裂开的状态。
            Attributes.RestoreSnapshot(in snapshot.Attributes);
            Buffs.RestoreSnapshot(snapshot.Buffs);

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
            Attributes.Release();
            Buffs.Clear();
            Profile = null;
            _grounded = true;
        }
    }
}
