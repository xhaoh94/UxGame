using Cysharp.Threading.Tasks;
using FairyGUI;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ux
{
    [Serializable]
    public struct TriggerData
    {
        public Key Key;
        public string State;
    }
    public class OperateComponent : Entity, IAwakeSystem, IUpdateSystem, InputActions.IPlayerActions
    {
        Unit Unit => Parent as Unit;
        private InputActions _input;
        TriggerData? triggerData;
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
        public void AddTrigger(TriggerData triggerData)
        {
            this.triggerData = triggerData;
        }
        public void RemoveTrigger()
        {
            this.triggerData = null;
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

        public void OnKey(InputAction.CallbackContext context)
        {
            StateMgr.Ins.Update(Unit.ID, StateConditionBase.Type.Action_Keyboard);
            //if (context.performed)
            //{
            //    if (this.triggerData != null)
            //    {
            //        if (context.control == Keyboard.current[triggerData.Value.Key])
            //        {
            //            Unit.State.Machine.Enter(triggerData.Value.State);
            //            this.triggerData = null;
            //            return;
            //        }
            //    }

            //    if (context.control == Keyboard.current.qKey)
            //    {
            //        Unit.State.Machine.Enter<StateAttack>();
            //    }
            //    else if (context.control == Keyboard.current.eKey)
            //    {
            //        var asset = await StateMgr.Ins.GetSkillAssetAsync("Skill02");
            //        Unit.Director.SetPlayableAsset(asset);
            //        Unit.Director.Play();
            //    }
            //}
        }
    }
}