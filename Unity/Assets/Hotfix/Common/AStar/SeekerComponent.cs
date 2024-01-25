using System;
using System.Collections.Generic;
using Ux;
using Pathfinding;
using UnityEngine;

namespace Ux
{
    public class SeekerComponent : Entity, IAwakeSystem<Seeker>
    {
        public Seeker Seeker { get; private set; }
        Unit Unit => Parent as Unit;

        public void OnAwake(Seeker a)
        {
            Seeker = a;
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            Seeker = null;
        }
       
        public void StartPath(Vector3 target)
        {
            Seeker.StartPath(Unit.Model.transform.position, target, OnPathComplete);
            //
            // if (AstarPath.active == null) return;
            // var p = ABPath.Construct(Player.Go.transform.position, target, OnPathComplete);
            // AstarPath.StartPath (p);

            // var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            // go.transform.position = target;
            // go.transform.localScale = Vector3.one * 0.1f;
        }

        void OnPathComplete(Path p)
        {
            if (p.error)
            {
                return;
            }
            SceneModule.Ins.SendMove(p.vectorPath);
        }

    }
}