using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace Ux
{
    public partial class HeroZSIdle
    {        
        public Unit Unit => (StateMachine.Owner as StateComponent).ParentAs<Unit>();
        protected override void OnEnter()
        {
            base.OnEnter();
            var asset = TimelineMgr.Ins.LoadAsset("HeroZSIdle");
            Unit.Timeline.Play(asset);
        }
    }
    public partial class HeroZSRun
    {        
        public Unit Unit => (StateMachine.Owner as StateComponent).ParentAs<Unit>();

        public bool UseCharacterForward { get; } = false;
        public bool LockToCameraForward { get; } = false;
        public float TurnSpeed { get; } = 10;
        protected override void OnEnter()
        {
            base.OnEnter();
            var asset = TimelineMgr.Ins.LoadAsset("HeroZSRun");
            Unit.Timeline.SetBindObj("Animator", Unit.Viewer.GetComponentInChildren<Animator>());
            Unit.Timeline.Play(asset);
            GameMethod.Update += OnUpdate;
        }
        protected override void OnExit()
        {
            base.OnExit();
            GameMethod.Update -= OnUpdate;
            //Unit.Path.Stop(false);                 
        }
        void OnUpdate()
        {
            //if (Unit.Path.PathIndex < Unit.Path.Points.Count)
            //{
            //    var target = Unit.Path.Points[Unit.Path.PathIndex];
            //    var dir = target - Unit.Position;
            //    var rotation = Quaternion.LookRotation(dir);
            //    Unit.Rotation = Quaternion.Slerp(Unit.Rotation, rotation, Time.fixedDeltaTime * 10f);
            //    Unit.Position += dir.normalized * (Time.fixedDeltaTime * 5);
            //    if (Vector3.SqrMagnitude(dir) <= 0.1f)
            //    {
            //        Unit.Path.PathIndex++;
            //    }
            //}
            //else
            //{
            //    Unit.Path.Stop(true);
            //}

            UpdateTargetDirection();

            if (Unit.Path.MoveVector2 != Vector2.zero && targetDirection.magnitude > 0.1f)
            {
                Vector3 lookDirection = targetDirection.normalized;
                var freeRotation = Quaternion.LookRotation(lookDirection, Unit.Viewer.transform.up);
                var diferenceRotation = freeRotation.eulerAngles.y - Unit.Viewer.transform.eulerAngles.y;
                var eulerY = Unit.Viewer.transform.eulerAngles.y;

                if (diferenceRotation < 0 || diferenceRotation > 0) eulerY = freeRotation.eulerAngles.y;
                var euler = new Vector3(0, eulerY, 0);

                Unit.Rotation = Quaternion.Slerp(Unit.Rotation, Quaternion.Euler(euler), TurnSpeed * turnSpeedMultiplier * Time.deltaTime);
                Unit.Position += lookDirection * (Time.fixedDeltaTime * 5);
            }
        }
        private float turnSpeedMultiplier;
        public Vector3 targetDirection { get; private set; }
        public virtual void UpdateTargetDirection()
        {
            if (!UseCharacterForward)
            {
                turnSpeedMultiplier = 1f;
                var forward = Unit.Map.Camera.MapCamera.transform.TransformDirection(Vector3.forward);
                forward.y = 0;

                //get the right-facing direction of the referenceTransform
                var right = Unit.Map.Camera.MapCamera.transform.TransformDirection(Vector3.right);

                // determine the direction the player will face based on input and the referenceTransform's right and forward directions
                targetDirection = Unit.Path.MoveVector2.x * right + Unit.Path.MoveVector2.y * forward;
            }
            else
            {
                turnSpeedMultiplier = 0.2f;
                var forward = Unit.Viewer.transform.TransformDirection(Vector3.forward);
                forward.y = 0;

                //get the right-facing direction of the referenceTransform
                var right = Unit.Viewer.transform.TransformDirection(Vector3.right);
                targetDirection = Unit.Path.MoveVector2.x * right + Mathf.Abs(Unit.Path.MoveVector2.y) * forward;
            }
        }
    }

}
