using SJ;
using System.Collections.Generic;
using UnityEngine.Playables;

namespace Ux
{
    public class TimelineTrack
    {
        public PlayableGraph PlayableGraph => Component.PlayableGraph;
        public TimelineComponent Component => Timeline.Component;
        public TimelineTrackAsset Asset { get; private set; }
        public Timeline Timeline { get; private set; }
        public Entity Track { get; private set; }

        List<TimelineClip> _clips = new List<TimelineClip>();

        public void Init(Timeline timeline, TimelineTrackAsset asset)
        {
            Timeline = timeline;
            Asset = asset;
            Track = timeline.Add(Asset.TrackType, this);
            foreach (var clipAsset in Asset.clips)
            {
                var clip = Pool.Get<TimelineClip>();
                clip.Init(this, clipAsset);
                _clips.Add(clip);
            }
        }
        public void Release()
        {
            foreach (var clip in _clips)
            {
                clip.Release();
            }
            _clips.Clear();
            Timeline = null;
            Asset = null;
            Track = null;
            Pool.Push(this);
        }
        public void Evaluate(float time)
        {
            foreach (var clip in _clips)
            {
                clip.Evaluate(time);
            }
        }
        public void SetTime(float time)
        {            
            foreach (var clip in _clips)
            {
                clip.SetTime(time);
            }
        }
    }
}
