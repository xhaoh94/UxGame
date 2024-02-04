using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Ux
{
    public class PlayableDirectorComponent : Entity, IAwakeSystem<PlayableDirector>
    {
        PlayableDirector _director;
        Dictionary<string, UnityEngine.Object> _bingObjs;
        public void OnAwake(PlayableDirector a)
        {
            _director = a;
            _director.playOnAwake = false;
            _director.played += OnPlayStart;
            _director.stopped += OnPlayEnd;
            _director.paused += OnPlayPause;
        }

        protected override void OnDestroy()
        {
            _director.played -= OnPlayStart;
            _director.stopped -= OnPlayEnd;
            _director.paused -= OnPlayPause;
            _director = null;
            _bingObjs?.Clear();
        }
        public Action<PlayableDirector> OnPlayStartEvent;
        public Action<PlayableDirector> OnPlayPauseEvent;
        public Action<PlayableDirector> OnPlayEndEvent;
        void OnPlayStart(PlayableDirector playableDirector)
        {            
            OnPlayStartEvent?.Invoke(playableDirector);
        }
        void OnPlayPause(PlayableDirector playableDirector)
        {
            OnPlayPauseEvent?.Invoke(playableDirector);
        }
        void OnPlayEnd(PlayableDirector playableDirector)
        {
            OnPlayEndEvent?.Invoke(playableDirector);
        }


        public void SetBinding(string trackName, UnityEngine.Object o)
        {
            if (_bingObjs == null)
            {
                _bingObjs = new Dictionary<string, UnityEngine.Object>();
            }
            _bingObjs[trackName] = o;
        }

        public T GetTrack<T>(StateTimeline asset, string trackName) where T : TrackAsset
        {
            if (asset == null)
            {
                return null;
            }
            return asset.GetTrack<T>(trackName);
        }

        public T GetClip<T>(StateTimeline asset, string trackName, string clipName) where T : PlayableAsset
        {
            if (asset == null)
            {
                return null;
            }
            return asset.GetClip<T>(trackName, clipName);
        }

        public void Play(StateTimeline asset, DirectorWrapMode mode)
        {
            if (asset == null)
            {
                return;
            }

            if (_bingObjs != null)
            {
                foreach (var kv in _bingObjs)
                {
                    var track = GetTrack<TrackAsset>(asset, kv.Key);
                    if (track == null) continue;

                    _director.SetGenericBinding(track, kv.Value);
                }
            }
            _director.Play(asset.Asset, mode);
        }
    }

}
