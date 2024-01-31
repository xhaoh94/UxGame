using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    public class StateIdle : UnitTimeLineNode
    {
        //public override string ResName => "Hero_ZS@Stand";        
        public override string ResName => "ZS_Idle";
        protected override void OnEnter(object args = null)
        {
            base.OnEnter(args);
        }

    }
    public class StateRun : UnitAnimNode
    {
        public override string ResName => "Hero_ZS@Run";
        private List<Vector3> _points = new List<Vector3>();
        private int _pathIndex;
        protected override void OnEnter(object args = null)
        {
            base.OnEnter(args);
            if (args is Pb.BcstUnitMove move)
            {
                _pathIndex = move.pointIndex;
                _points.Clear();
                foreach (var point in move.Points)
                {
                    _points.Add(new Vector3(point.X, point.Y, point.Z));
                }
            }
        }
        protected override void OnUpdate()
        {
            if (_pathIndex < _points.Count)
            {
                var target = _points[_pathIndex];
                var dir = target - Unit.Position;
                var rotation = Quaternion.LookRotation(dir);
                Unit.Rotation = Quaternion.Slerp(Unit.Rotation, rotation, Time.fixedDeltaTime * 10f);
                Unit.Position += dir.normalized * (Time.fixedDeltaTime * 5);
                if (Vector3.SqrMagnitude(dir) <= 0.1f)
                {
                    _pathIndex++;
                }
            }
            else
            {
                Machine.Enter<StateIdle>();
                _points.Clear();
                _pathIndex = 0;
            }
        }
    }
}
