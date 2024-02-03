using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    public partial class HeroZSIdle
    {
        //const UnitAnimNode AnimNode = new UnitAnimNode(this);
        public override long OwnerID => Unit.ID;
        public Unit Unit => (Machine.Owner as StateComponent).ParentAs<Unit>();
        public override AnimComponent Anim => Unit.Anim;
        protected override void OnEnter()
        {
            base.OnEnter();
        }

    }
    public partial class HeroZSRun
    {
        public override long OwnerID => Unit.ID;
        public Unit Unit => (Machine.Owner as StateComponent).ParentAs<Unit>();
        public override AnimComponent Anim => Unit.Anim;

        protected override void OnEnter()
        {
            base.OnEnter();
        }
        protected override void OnUpdate()
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
            //        _pathIndex++;
            //    }
            //}
            //else
            //{                
            //    _points.Clear();
            //    _pathIndex = 0;
            //}
        }
        protected override StateConditionBase CreateCondition(string condition, params object[] args)
        {
            switch (condition)
            {
                case nameof(ActionMoveCondition):
                    return new HeroMoveCondition();
            }
            return base.CreateCondition(condition, args);
        }
    }

    public class HeroMoveCondition : ActionMoveCondition
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
