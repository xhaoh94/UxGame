using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Ux
{
    public class StateTimeline
    {        
        public UnityEngine.Timeline.TimelineAsset Asset { get; }
        Dictionary<string, PlayableBinding> bindings;
        Dictionary<string, Dictionary<string, PlayableAsset>> clips;

        public StateTimeline(UnityEngine.Timeline.TimelineAsset asset)
        {
            Asset = asset;

            bindings = new Dictionary<string, PlayableBinding>();
            clips = new Dictionary<string, Dictionary<string, PlayableAsset>>();

            foreach (var o in asset.outputs)
            {
                var trackName = o.streamName;
                bindings.Add(trackName, o);
                var track = o.sourceObject as TrackAsset;
                var clipList = track.GetClips();
                foreach (var c in clipList)
                {
                    if (!clips.ContainsKey(trackName))
                    {
                        clips[trackName] = new Dictionary<string, PlayableAsset>();
                    }
                    var name2Clips = clips[trackName];
                    if (!name2Clips.ContainsKey(c.displayName))
                    {
                        name2Clips.Add(c.displayName, c.asset as PlayableAsset);
                    }
                }
            }
        }
        public T GetTrack<T>(string trackName) where T : TrackAsset
        {
            if (Asset == null)
            {
                return null;
            }
            if (bindings.TryGetValue(trackName, out var bing))
            {
                return bing.sourceObject as T;
            }
            return null;
        }

        public T GetClip<T>(string trackName, string clipName) where T : PlayableAsset
        {
            if (Asset == null)
            {
                return null;
            }
            if (clips.ContainsKey(trackName))
            {
                var track = clips[trackName];
                if (track.ContainsKey(clipName))
                {
                    return track[clipName] as T;
                }
                else
                {
                    Log.Error("GetClip Error, Track does not contain clip, trackName: " + trackName + ", clipName: " + clipName);
                }
            }
            else
            {
                Log.Error("GetClip Error, Track does not contain clip, trackName: " + trackName + ", clipName: " + clipName);
            }
            return null;
        }

    }
}
