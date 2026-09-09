using UnityEngine;
using UnityEngine.InputSystem;

namespace Ux
{
    /// <summary>只负责把设备输入转换成世界方向和逻辑帧命令。</summary>
    public sealed class OperateComponent : Entity, IAwakeSystem, InputActions.IPlayerActions
    {
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

        public void OnMove(InputAction.CallbackContext context)
        {
            if (!context.performed)
            {
                SceneModule.Ins.SendMove(Vector2.zero);
                return;
            }

            var moveInput = context.ReadValue<Vector2>();
            var camera = Unit.Map?.Camera?.MapCamera;
            if (camera == null)
            {
                SceneModule.Ins.SendMove(moveInput);
                return;
            }

            var forward = camera.transform.forward;
            forward.y = 0;
            forward.Normalize();
            var right = camera.transform.right;
            right.y = 0;
            right.Normalize();
            var worldDirection = right * moveInput.x + forward * moveInput.y;
            SceneModule.Ins.SendMove(new Vector2(worldDirection.x, worldDirection.z));
        }

        public void OnFire(InputAction.CallbackContext context)
        {
            if (context.performed && Unit.Combat != null)
            {
                Unit.Combat.RequestAction(AttackActionId);
            }
        }

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
