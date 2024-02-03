using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    public class PathComponent : Entity,IAwakeSystem, IFixedUpdateSystem
    {
        private List<Vector3> _points;
        private int _pathIndex;
        public bool IsRun { get; private set; }
        Unit Unit => Parent as Unit;

        void IAwakeSystem.OnAwake()
        {
            _points = new List<Vector3>();
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();            
            _points.Clear();
            _pathIndex = 0;
            IsRun = false;
        }
        public void OnFixedUpdate()
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

                if (!IsRun)
                {
                    IsRun = true;
                    StateMgr.Ins.AddMove(Unit.ID);
                    StateMgr.Ins.Update(Unit.ID);
                }
            }
            else
            {
                if (IsRun)
                {
                    IsRun = false;                                        
                    _points.Clear();
                    _pathIndex = 0;
                    StateMgr.Ins.RevemoMove(Unit.ID);
                    StateMgr.Ins.Update(Unit.ID);
                }
            }
        }

        public void SetPoints(List<Pb.Vector3> points, int moveIndex)
        {
            _pathIndex = moveIndex;
            _points.Clear();
            foreach (var point in points)
            {
                _points.Add(new Vector3(point.X, point.Y, point.Z));
            }
        }
    }
}