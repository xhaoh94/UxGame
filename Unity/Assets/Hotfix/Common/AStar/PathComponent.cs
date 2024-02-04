using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    public class PathComponent : Entity, IAwakeSystem
    {
        public List<Vector3> Points { get; set; }
        public int PathIndex { get; set; }
        public bool IsRun { get; private set; }
        Unit Unit => Parent as Unit;

        void IAwakeSystem.OnAwake()
        {
            Points = new List<Vector3>();
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            Points.Clear();
            PathIndex = 0;
            IsRun = false;
        }
        public void SetPoints(List<Pb.Vector3> points, int moveIndex)
        {
            PathIndex = moveIndex;
            Points.Clear();
            foreach (var point in points)
            {
                Points.Add(new Vector3(point.X, point.Y, point.Z));
            }
            IsRun = true;
            StateMgr.Ins.Update(Unit.ID, StateConditionBase.Type.Custom);
        }
        public void Stop(bool isUpdate)
        {
            IsRun = false;
            PathIndex = 0;
            Points.Clear();
            if (isUpdate)
                StateMgr.Ins.Update(Unit.ID);
        }
    }
}