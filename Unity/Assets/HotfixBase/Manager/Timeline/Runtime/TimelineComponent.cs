using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace Ux
{
    public partial class TimelineComponent : Entity, IAwakeSystem, IFixedUpdateSystem
    {
        public Timeline Current { get; private set; }
        public Timeline Last { get; private set; }
        List<Timeline> _additives = new List<Timeline>();
        Dictionary<string, object> _bindObjs = new ();
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
            PlayableGraph = PlayableGraph.Create(Parent.Name);
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
                    for (var i = _additives.Count - 1; i >= 0; i--)
                    {
                        var additive = _additives[i];
                        additive.Evaluate(deltaTime);
                        if (additive.IsDone)
                        {
                            Remove(additive);
                            _additives.RemoveAt(i);
                        }
                    }
                }
            }
        }

        public void Play(TimelineAsset timeline, bool isAdditive = false)
        {
            if (isAdditive)
            {
                var additive = Add<Timeline, TimelineAsset, bool>(timeline, isAdditive);
                additive.StartWeightFade(1, 0.3f);
                _additives.Insert(0, additive);
            }
            else
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
                Current = Add<Timeline, TimelineAsset, bool>(timeline, isAdditive);
                Current?.StartWeightFade(1, 0.3f);
                Last?.StartWeightFade(0, 0.3f);
            }
        }

        public void SetBindObj(string key,object obj)
        {
            _bindObjs[key] = obj;
            if (PlayableGraph.IsValid())
            {
                Current?.OnBinding();
                Last?.OnBinding();
                for (var i = _additives.Count - 1; i >= 0; i--)
                {
                    var additive = _additives[i];
                    additive.OnBinding();
                }
            }
        }
        public T GetBindObj<T>(string key)
        {
            if (_bindObjs.TryGetValue(key,out var obj))
            {
                return (T)obj;
            }
            return default;
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

                for (var i = _additives.Count - 1; i >= 0; i--)
                {
                    var additive = _additives[i];
                    additive.Set(frame);
                    if (additive.IsDone)
                    {
                        Remove(additive);
                        _additives.RemoveAt(i);
                    }
                }
            }
        }
    }
}
