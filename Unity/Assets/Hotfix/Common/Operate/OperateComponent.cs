using Ux;
using System.Collections;
using Pathfinding;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Timeline;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FairyGUI;

namespace Ux
{
    public class OperateComponent : Entity, IAwakeSystem, IUpdateSystem, InputActions.IPlayerActions
    {
        Unit Unit => Parent as Unit;
        private InputActions _input;

        public void OnAwake()
        {
            _input = new InputActions();
            _input.Player.SetCallbacks(this);
            _input.Enable();
        }

        public void OnUpdate()
        {
        }

        protected override void OnDestroy()
        {
            _input?.Disable();
            _input?.Dispose();
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                Vector2 move = context.ReadValue<Vector2>();
            }
        }

        public void OnFire(InputAction.CallbackContext context)
        {
            if (Stage.isTouchOnUI)
            {
                return;
            }
            if (context.performed)
            {
                var pos = Mouse.current.position.ReadValue();
                var mapCamera = Unit.Map.Camera.MapCamera;
                var ray = mapCamera.ScreenPointToRay(pos);
                if (Physics.Raycast(ray, out var hitInfo))
                {
                    if (hitInfo.transform.gameObject.CompareTag("Ground") ||
                        hitInfo.transform.gameObject.CompareTag("FogOfWar"))
                    {
                        Log.Debug("点击地板");
                        Unit.Seeker.StartPath(hitInfo.point);
                    }
                }
            }
        }

        public async void OnKey(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                if (context.control == Keyboard.current.qKey)
                {
                    Unit.State.Machine.Enter<StateAttack>();
                }
                else if (context.control == Keyboard.current.eKey)
                {
                    var asset = await SkillMgr.Ins.GetSkillAssetAsync("Skill02");
                    Unit.Director.SetPlayableAsset(asset);
                    Unit.Director.Play();
                }
            }
        }
    }
}