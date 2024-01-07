using System;
using System.Collections.Generic;
using Ux;
using Pathfinding;
using UnityEngine;

namespace Ux
{
    public class SeekerComponent : Entity, IAwakeSystem<Seeker>, IFixedUpdateSystem
    {
        public Seeker Seeker { get; private set; }
        Player Player => Parent as Player;

        private Path _path;
        private int _pathIndex;
        public bool IsRun { get; private set; }

        public void OnAwake(Seeker a)
        {
            Seeker = a;
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            Seeker = null;
            _path = null;
            _pathIndex = 0;
            IsRun = false;
        }

        public void OnFixedUpdate()
        {
            if (_path != null && _pathIndex < _path.vectorPath.Count)
            {
                var target = _path.vectorPath[_pathIndex];
                var dir = target - Player.Position;
                var rotation = Quaternion.LookRotation(dir);
                Player.Rotation = Quaternion.Slerp(Player.Rotation, rotation, Time.fixedDeltaTime * 10f);
                Player.Position += dir.normalized * (Time.fixedDeltaTime * 5);
                if (Vector3.SqrMagnitude(dir) <= 0.1f)
                {
                    _pathIndex++;
                }

                if (!IsRun)
                {
                    IsRun = true;
                    Player.State.Machine.Enter<StateRun>();
                }
            }
            else
            {
                if (IsRun)
                {
                    IsRun = false;
                    Player.State.Machine.Enter<StateIdle>();
                    _path = null;
                    _pathIndex = 0;
                }
            }
        }

        public void StartPath(Vector3 target)
        {
            Seeker.StartPath(Player.Go.transform.position, target, OnPathComplete);
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

            _pathIndex = 0;
            _path = p;
        }
    }
}