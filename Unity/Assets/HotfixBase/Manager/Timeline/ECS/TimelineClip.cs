using UnityEngine.Playables;

namespace Ux
{
    public interface ITimelineClip
    {
        void OnEnable();
        void OnDisable();
        void SetTime(float time);
    }
    public class TimelineClip
    {        
        public float Time { get; private set; }        
        public bool Active { get; private set; }

        public TimelineTrack Track { get; private set; }
        public TimelineClipAsset Asset { get; private set; }
        public PlayableGraph PlayableGraph=> Track.Component.PlayableGraph;
        float PlaySpeed => Track.Component.PlaySpeed;
        ITimelineClip _clip;
        public void Init(TimelineTrack track, TimelineClipAsset asset)
        {
            Track=track;
            Asset=asset;
            _clip = track.Track.Add(asset.ClipType, this) as ITimelineClip;            
        }

        public void Release()
        {
            _clip = null;
            Asset = null;
            Track = null;
            Pool.Push(this);
        }

        public void Evaluate(float deltaTime)
        {                                               
            SetTime(Time + deltaTime);
        }
        public void OnEnable()
        {
            _clip?.OnEnable();
        }
        public void OnDisable()
        {
            _clip?.OnDisable();
        }

        public void SetTime(float time)
        {
            Time = time;
            if (!Active && Asset.StartTime <= time && time <= Asset.EndTime)
            {
                Active = true;
                OnEnable();
            }
            else if (Active && (time < Asset.StartTime || Asset.EndTime < time))
            {
                Active = false;
                OnDisable();
            }
            _clip?.SetTime(time);
        }
    }
}
