﻿using Ux;
using System.Collections;
using Pathfinding;
using UnityEngine;
using UnityEngine.InputSystem;

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
            if (context.performed)
            {
                var pos = Mouse.current.position.ReadValue();
                var mapCamera = Player.Map.Camera.MapCamera;
                var ray = mapCamera.ScreenPointToRay(pos);
                if (Physics.Raycast(ray, out var hitInfo))
                {
                    if (hitInfo.transform.gameObject.CompareTag("Ground"))
                    {
                        Log.Debug("点击地板");
                        Player.Seeker.StartPath(hitInfo.point);
                    }
                }
            }
        }
    }
}