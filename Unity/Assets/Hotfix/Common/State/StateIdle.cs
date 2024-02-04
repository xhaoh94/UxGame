using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace Ux
{
    public partial class HeroZSIdle
    {
        //public override DirectorWrapMode WarpMode => DirectorWrapMode.Loop;
        public Unit Unit => (Machine.Owner as StateComponent).ParentAs<Unit>();
        protected override void OnEnter()
        {
            base.OnEnter();
        }

    }
    public partial class HeroZSRun
    {
        //public override DirectorWrapMode WarpMode => DirectorWrapMode.Loop;
        public Unit Unit => (Machine.Owner as StateComponent).ParentAs<Unit>();


        protected override void OnEnter()
        {
            base.OnEnter();
        }
        protected override void OnExit()
        {
            base.OnExit();            
            Unit.Path.Stop(false);            
        }
        protected override void OnUpdate()
        {
            if (Unit.Path.PathIndex < Unit.Path.Points.Count)
            {
                var target = Unit.Path.Points[Unit.Path.PathIndex];
                var dir = target - Unit.Position;
                var rotation = Quaternion.LookRotation(dir);
                Unit.Rotation = Quaternion.Slerp(Unit.Rotation, rotation, Time.fixedDeltaTime * 10f);
                Unit.Position += dir.normalized * (Time.fixedDeltaTime * 5);
                if (Vector3.SqrMagnitude(dir) <= 0.1f)
                {
                    Unit.Path.PathIndex++;
                }
            }
            else
            {
                Unit.Path.Stop(true);
            }
        }
        protected override StateConditionBase CreateCondition(string condition, params object[] args)
        {
            switch (condition)
            {
                case nameof(HeroMoveCondition):
                    return new HeroMoveCondition();
            }
            return base.CreateCondition(condition, args);
        }
    }

    public class HeroMoveCondition : CustomCondition
    {
        public override bool IsValid
        {
            get
            {
                var unit = (UnitState.Machine.Owner as StateComponent).ParentAs<Unit>();
                return unit.Path.IsRun;
            }
        }
    }
}
