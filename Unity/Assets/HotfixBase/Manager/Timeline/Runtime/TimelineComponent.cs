using System;
using UnityEngine;
using UnityEngine.Playables;

namespace Ux
{
    public partial class TimelineComponent : Entity, IAwakeSystem, IFixedUpdateSystem
    {
        public Timeline Current { get; private set; }
        public Timeline Last { get; private set; }
        public PlayableGraph PlayableGraph { get; private set; }

        float _playSpeed = 1;
        public float PlaySpeed
        {
            get => (float)Math.Round(Math.Max(0.001f, _playSpeed), 2);
            set => _playSpeed = value;
        }

        void IAwakeSystem.OnAwake()
        {
            PlaySpeed = 1;
            PlayableGraph = PlayableGraph.Create(Parent.Viewer.name);
            PlayableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            PlayableGraph.Play();            
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            PlayableGraph.Destroy();
        }

        void IFixedUpdateSystem.OnFixedUpdate()
        {
            if (Current != null)
            {
                if (PlayableGraph.IsValid())
                {
                    var deltaTime = Time.fixedDeltaTime * PlaySpeed;
                    //PlayableGraph.Evaluate(deltaTime);
                    Current?.Evaluate(deltaTime);
                    if (Last != null)
                    {
                        Last.Evaluate(deltaTime);
                        if (Last.IsDone)
                        {
                            Remove(Last);
                            Last = null;
                        }
                    }
                }
            }
        }

        public void Play(TimelineAsset timeline)
        {
            if (Last != null)
            {
                Remove(Last);
                Last = null;
            }
            if (Current != null)
            {
                if (Application.isPlaying)
                {
                    Last = Current;
                }
                else
                {
                    Remove(Current);
                }
            }
            Current = Add<Timeline, TimelineAsset>(timeline);
        }


        public void Set(int frame)
        {
            if (PlayableGraph.IsValid())
            {                
                Current?.Set(frame);
                if (Last != null)
                {
                    Last.Set(frame);
                    if (Last.IsDone)
                    {
                        Remove(Last);
                        Last = null;
                    }
                }
            }
        }
    }
}
