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
        public GameObject GameObject { get; private set; }
        
        public Timeline CurTimeline { get; private set; }


        bool _isInit;
        public PlayableGraph PlayableGraph { get; private set; }

        double _playSpeed;
        public double PlaySpeed
        {
            get => Math.Round(Math.Max(0.001f, _playSpeed), 2);
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
#if UNITY_EDITOR
                _EditorInit();
#endif
            }
        }
        void IAwakeSystem.OnAwake()
        {
            PlaySpeed = 1;
            IsPlaying = false;
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            PlayableGraph.Destroy();
        }

        void IFixedUpdateSystem.OnFixedUpdate()
        {
            Evaluate(Time.deltaTime * (float)PlaySpeed);
        }

        public void Init(GameObject gameObject)
        {
            this.GameObject = gameObject;
            if (PlayableGraph.IsValid())
            {
                PlayableGraph.Destroy();
            }
            PlayableGraph = PlayableGraph.Create(gameObject.name);
            _isInit = true;
        }

        public void SetTimeline(TimelineAsset setting)
        {
            //if(!_entitys.TryGetValue(setting,out var entity))
            //{
            //    entity=AddChild<Timeline, TimelineAsset>(setting);
            //    _entitys.Add(setting, entity);
            //}
            if (CurTimeline != null)
            {
                CurTimeline.Destroy();
            }
            RemoveComponent<TLAnimationRoot>();
            var entity = AddChild<Timeline, TimelineAsset, TimelineComponent>(setting, this);
            CurTimeline = entity;
        }

        void Evaluate(float deltaTime)
        {
            if (!_isInit) return;
            if (PlayableGraph.IsValid())
            {
                PlayableGraph.Evaluate(deltaTime);
            }
        }

        public void SetTime(float time)
        {
            if (PlayableGraph.IsValid())
            {
                CurTimeline?.SetTime(time);
            }
        }

    }
}
