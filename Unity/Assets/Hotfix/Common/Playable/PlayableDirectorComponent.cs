using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Ux
{
    public class PlayableDirectorComponent : Entity, IAwakeSystem<PlayableDirector>
    {
        PlayableDirector director;
        SkillAsset asset;
        Dictionary<string, Object> bingObjs;
        public void OnAwake(PlayableDirector a)
        {
            director = a;
            director.playOnAwake = false;
        }


        public void SetPlayableAsset(SkillAsset asset)
        {
            director.playableAsset = asset.Asset;
            this.asset = asset;

            if (bingObjs != null)
            {
                foreach (var kv in bingObjs)
                {
                    var track = GetTrack<TrackAsset>(kv.Key);
                    if (track == null) continue;
                    director.SetGenericBinding(track, kv.Value);
                }
            }
        }

        public void SetBinding(string trackName, Object o)
        {
            if (bingObjs == null)
            {
                bingObjs = new Dictionary<string, Object>();
            }
            bingObjs[trackName] = o;

            if (asset == null)
            {
                return;
            }
            var track = asset.GetTrack<TrackAsset>(trackName);
            if (track == null) return;
            director.SetGenericBinding(track, o);
        }

        public T GetTrack<T>(string trackName) where T : TrackAsset
        {
            if (asset == null)
            {
                return null;
            }
            return asset.GetTrack<T>(trackName);
        }

        public T GetClip<T>(string trackName, string clipName) where T : PlayableAsset
        {
            if (asset == null)
            {
                return null;
            }
            return asset.GetClip<T>(trackName, clipName);
        }

        public void Play()
        {
            if (asset == null)
            {
                return;
            }
            director.Play();
        }
    }

}
