using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        bool _isPlaying;
        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                if (_isPlaying == value)
                    return;

                _isPlaying = value;
            }
        }
        void IAwakeSystem.OnAwake()
        {
            PlaySpeed = 1;
            IsPlaying = false;
            PlayableGraph = PlayableGraph.Create(Parent.Model.name);
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            PlayableGraph.Destroy();
        }

        void IFixedUpdateSystem.OnFixedUpdate()
        {
            if (_isPlaying)
            {
                Evaluate(Time.deltaTime * PlaySpeed);
            }
        }

        public void Play(TimelineAsset timeline)
        {
            if (Last != null)
            {
                Last.Destroy();
                Last = null;
            }
            if (Current!=null)
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

        public void Evaluate(float deltaTime)
        {
            if (PlayableGraph.IsValid())
            {
                PlayableGraph.Evaluate(deltaTime);
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
}
