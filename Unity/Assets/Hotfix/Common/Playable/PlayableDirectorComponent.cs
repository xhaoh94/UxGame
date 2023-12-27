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
        SkillAsset _asset;
        Dictionary<string, Object> _bingObjs;
        public void OnAwake(PlayableDirector a)
        {
            _director = a;
            _director.playOnAwake = false;            
        }

        protected override void OnDestroy()
        {
            _director = null;
            _asset = null;
            _bingObjs?.Clear();
        }

        public void SetPlayableAsset(SkillAsset asset)
        {
            _director.playableAsset = asset.Asset;
            this._asset = asset;

            if (_bingObjs != null)
            {
                foreach (var kv in _bingObjs)
                {
                    var track = GetTrack<TrackAsset>(kv.Key);
                    if (track == null) continue;
                    _director.SetGenericBinding(track, kv.Value);
                }
            }
        }

        public void SetBinding(string trackName, Object o)
        {
            if (_bingObjs == null)
            {
                _bingObjs = new Dictionary<string, Object>();
            }
            _bingObjs[trackName] = o;

            if (_asset == null)
            {
                return;
            }
            var track = _asset.GetTrack<TrackAsset>(trackName);
            if (track == null) return;
            _director.SetGenericBinding(track, o);
        }

        public T GetTrack<T>(string trackName) where T : TrackAsset
        {
            if (_asset == null)
            {
                return null;
            }
            return _asset.GetTrack<T>(trackName);
        }

        public T GetClip<T>(string trackName, string clipName) where T : PlayableAsset
        {
            if (_asset == null)
            {
                return null;
            }
            return _asset.GetClip<T>(trackName, clipName);
        }

        public void Play()
        {
            if (_asset == null)
            {
                return;
            }
            _director.Play();
        }
    }

}
