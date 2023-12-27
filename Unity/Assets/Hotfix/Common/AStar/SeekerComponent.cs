using System;
using System.Collections.Generic;
using Ux;
using Pathfinding;
using UnityEngine;

namespace Ux
{
    public class SeekerComponent : Entity, IAwakeSystem<Seeker>, IFixedUpdateSystem
    {
        public Seeker seeker;
        Player Player => Parent as Player;
        private Path path;
        private int pathIndex;
        public bool IsRun { get; private set; }

        public void OnAwake(Seeker a)
        {
            seeker = a;
        }

        public void OnFixedUpdate()
        {
            if (path != null && pathIndex < path.vectorPath.Count)
            {
                var target = path.vectorPath[pathIndex];
                var dir = target - Player.Postion;
                var rotation = Quaternion.LookRotation(dir);
                Player.Rotation = Quaternion.Slerp(Player.Rotation, rotation, Time.fixedDeltaTime * 10f);
                Player.Postion += dir.normalized * (Time.fixedDeltaTime * 5);
                if (Vector3.SqrMagnitude(dir) <= 0.1f)
                {
                    pathIndex++;
                }

                if (!IsRun)
                {
                    IsRun = true;
                    Player?.State.Machine.Enter<StateRun>();
                }
            }
            else
            {
                if (IsRun)
                {
                    IsRun = false;
                    Player?.State.Machine.Enter<StateIdle>();
                    path = null;
                    pathIndex = 0;
                }
            }
        }

        public void StartPath(Vector3 target)
        {
            seeker.StartPath(Player.Go.transform.position, target, OnPathComplete);
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

            pathIndex = 0;
            path = p;
        }
    }
}