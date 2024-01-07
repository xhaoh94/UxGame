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
        Player Player => Parent as Player;
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
                var mapCamera = Player.Map.Camera.MapCamera;
                var ray = mapCamera.ScreenPointToRay(pos);
                if (Physics.Raycast(ray, out var hitInfo))
                {
                    if (hitInfo.transform.gameObject.CompareTag("Ground") ||
                        hitInfo.transform.gameObject.CompareTag("FogOfWar"))
                    {
                        Log.Debug("点击地板");
                        Player.Seeker.StartPath(hitInfo.point);
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
                    var asset = await SkillMgr.Ins.GetSkillAssetAsync("Skill01");
                    Player.Director.SetPlayableAsset(asset);
                    Player.Director.Play();
                }
                else if (context.control == Keyboard.current.eKey)
                {
                    var asset = await SkillMgr.Ins.GetSkillAssetAsync("Skill02");
                    Player.Director.SetPlayableAsset(asset);
                    Player.Director.Play();
                }
            }
        }
    }
}