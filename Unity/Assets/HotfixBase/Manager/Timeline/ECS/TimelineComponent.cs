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

        bool _isInit;
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

#if UNITY_EDITOR
            PlayableGraph = PlayableGraph.Create(Parent.Model.name);
#endif
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

        public void SetTimeline(TimelineAsset setting)
        {
            //if(!_entitys.TryGetValue(setting,out var entity))
            //{
            //    entity=AddChild<Timeline, TimelineAsset>(setting);
            //    _entitys.Add(setting, entity);
            //}
            Current?.Destroy();
            Remove<TLAnimationRoot>();
            var entity = Add<Timeline, TimelineAsset>(setting);
            Current = entity;
        }

        void Evaluate(float deltaTime)
        {
            if (!_isInit) return;
            if (PlayableGraph.IsValid())
            {
                PlayableGraph.Evaluate(deltaTime);
                Current?.Evaluate(deltaTime);
            }
        }

        public void SetTime(float time)
        {
            if (PlayableGraph.IsValid())
            {
                Current?.SetTime(time);
            }
        }

    }
}
