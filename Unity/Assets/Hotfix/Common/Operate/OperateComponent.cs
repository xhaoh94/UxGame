using UnityEngine;
using UnityEngine.InputSystem;

namespace Ux
{
    /// <summary>只负责把设备输入转换成世界方向和逻辑帧命令。</summary>
    /// <remarks>
    /// 玩家操作进入战斗系统的唯一入口，移动和攻击两条链路都从这里出发。
    /// 本类不改任何战斗状态，只往外"扔输入"：移动扔方向向量，攻击扔一条带帧号的命令。
    ///
    /// ═══════════════════════════════════════════════════════════════
    /// 链路 A：按移动键 → 单位进入 Move 状态并产生位移
    /// ═══════════════════════════════════════════════════════════════
    ///   OnMove（本文件）
    ///     → SceneModule.SendMove            包成 Pb.BcstUnitMove，抛 UNIT_MOVE 事件
    ///     → Scene._OnUnitMove               按 roleId 找到对应 Unit
    ///     → PathComponent.SetPoints         把方向写进 MoveVector2（存的是"方向"，不是路径点）
    ///     → CombatComponent.MoveInput       把 MoveVector2 暴露给逻辑层
    ///     → CombatController.Tick           读 MoveInput，判定 Airborne / Idle / Move
    ///     → CombatComponent.TickMovement    只有判定为 Move 且没被阻挡，才真的改 Position
    ///
    /// 这条链全程**没有队列**：方向输入是"当前值"，逻辑层每帧主动来读一次，
    /// 所以松手时发零向量就等于告诉逻辑层"我不动了"。
    ///
    /// ═══════════════════════════════════════════════════════════════
    /// 链路 B：按 Q / 开火键 → 普攻动作跑起来
    /// ═══════════════════════════════════════════════════════════════
    ///   OnFire / OnKey（本文件）
    ///     → CombatComponent.RequestAction   包成 CombatCommand，帧号 = 当前帧 + 1，入队
    ///   ┈┈ 以上发生在按键的瞬间，不在任何逻辑帧内部 ┈┈
    ///     → BattleWorld 阶段 1              取出"帧号 == 当前帧"的命令（取走即删）
    ///     → BattleWorld 阶段 2              把命令交给 CombatComponent.TickLogic
    ///     → CombatController.Tick           转交动作系统
    ///     → CombatActionRunner.Tick         没动作走 TryStart，有动作走 TryCancel
    ///     → CombatActionRunner.Start        建立动作实例，ActionFrame = 0 ← "出手"真正发生
    ///     → CombatComponent.TickLogic 尾段  Resolve + Synchronize，攻击动画铺到 Action 轨道
    ///     → CombatActionRunner.AdvanceTo    此后每帧 ActionFrame + 1，到 DurationFrames 自动结束
    ///
    /// ═══════════════════════════════════════════════════════════════
    /// 两条链路最关键的区别
    /// ═══════════════════════════════════════════════════════════════
    /// 移动是"下一个逻辑帧立刻生效"，攻击是"下一个逻辑帧才开始起手"。
    /// 差别只在于：攻击多了一层**按帧号排队的命令缓冲**。
    /// 这层缓冲不是为了攻击本身，而是为了让网络命令和录像命令能走同一条路 ——
    /// 服务器发来的命令只要带上帧号塞进同一个队列，重放结果就和本地跑出来的一模一样。
    /// </remarks>
    public sealed class OperateComponent : Entity, IAwakeSystem, InputActions.IPlayerActions
    {
        /// <summary>普攻动作的 ActionId，对应 HeroZSAttack01.asset 里的 actionId 字段。</summary>
        private const int AttackActionId = 1001;

        private InputActions _input;
        private Unit Unit => ParentAs<Unit>();

        public void OnAwake()
        {
            _input = new InputActions();
            _input.Player.SetCallbacks(this);
            _input.Enable();
        }

        protected override void OnDestroy()
        {
            _input?.Disable();
            _input?.Dispose();
            _input = null;
            base.OnDestroy();
        }

        /// <summary>方向输入入口（移动链路的起点）。松手时发零向量，相当于"告诉逻辑层我不动了"。</summary>
        public void OnMove(InputAction.CallbackContext context)
        {
            // performed == false 表示这一次是"松手/取消"回调。
            // 发零向量而不是直接改状态 —— Idle 是逻辑层自己根据输入算出来的，输入层无权决定。
            if (!context.performed)
            {
                SceneModule.Ins.SendMove(Vector2.zero);
                return;
            }

            // 摇杆/按键给出的是屏幕或手柄空间的二维输入，战斗逻辑只认世界方向，所以要转换。
            var moveInput = context.ReadValue<Vector2>();
            var camera = Unit.Map?.Camera?.MapCamera;
            if (camera == null)
            {
                // 没有相机（无渲染环境/测试）时直接把原始输入当世界方向用。
                SceneModule.Ins.SendMove(moveInput);
                return;
            }

            // 相机空间 → 世界空间：把相机的 forward/right 压到水平面当基向量，
            // 这样"往上推摇杆"永远是朝屏幕里走，而不是朝世界 Z 轴。
            var forward = camera.transform.forward;
            forward.y = 0;
            forward.Normalize();
            var right = camera.transform.right;
            right.y = 0;
            right.Normalize();
            var worldDirection = right * moveInput.x + forward * moveInput.y;
            // 只取水平分量 (x, z)，Y 交给垂直逻辑（跳跃/重力）处理。
            SceneModule.Ins.SendMove(new Vector2(worldDirection.x, worldDirection.z));
        }

        /// <summary>开火键（鼠标左键/手柄 RT）入口，攻击链路起点。</summary>
        public void OnFire(InputAction.CallbackContext context)
        {
            // 只认按下，不认持续按住 —— 连发逻辑不在这里，而在动作的取消窗口里
            // （见 CombatActionRunner.TryCancel）。
            if (context.performed && Unit.Combat != null)
            {
                // 只传 ActionId，不传"这是什么技能"。
                // 具体动作的时长、能不能取消、打多远，全部由资源决定，输入层不参与。
                Unit.Combat.RequestAction(AttackActionId);
            }
        }

        /// <summary>Q 键入口。和 OnFire 走的是完全相同的一条路（两条入口共用一个 ActionId）。</summary>
        public void OnKey(InputAction.CallbackContext context)
        {
            if (!context.performed || Unit.Combat == null)
            {
                return;
            }

            var control = context.control;
            if (control == Keyboard.current.qKey)
            {
                Unit.Combat.RequestAction(AttackActionId);
            }
        }
    }
}
