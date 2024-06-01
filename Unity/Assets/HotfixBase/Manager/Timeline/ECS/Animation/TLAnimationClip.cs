using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Ux
{
    public class TLAnimationClip : TLAnimationBase, IAwakeSystem, ITimelineClip
    {
        public TimelineClip Clip => ParentAs<TimelineClip>();
        public AnimationClipAsset Asset => Clip.Asset as AnimationClipAsset;
        public TimelineComponent Component => Clip.Track.Component;
        public TLAnimationTrack Track => Clip.Track.GetComponent<TLAnimationTrack>();

        private AnimationClipPlayable _source;
        private Playable _parent;

        void IAwakeSystem.OnAwake()
        {
            _source = AnimationClipPlayable.Create(Component.PlayableGraph, Asset.clip);
            SetSourcePlayable(Component.PlayableGraph, _source);
        }

        float _time;
        public void SetTime(float time)
        {
            if (!Clip.Active) return;
            Time = time;
            TimelineMgr.Ins.Lerp(time, time, _Lerp, ref _time);
        }
        void _Lerp(float deltaTime)
        {
            if (_time < Asset.StartTime)
            {
                Weight = 0;
                Time = 0;
            }
            else if (Asset.StartTime <= _time && _time <= Asset.EndTime)
            {
                Time = _time;
                if (_time > Asset.InTime && _time < Asset.OutTime)
                {
                    Weight = 1;
                }
                else if (_time < Asset.InTime)
                {
                    Weight = (_time - Asset.StartTime) / (Asset.InTime - Asset.StartTime);
                }
                else if (_time > Asset.OutTime)
                {
                    Weight = 1 - ((_time - Asset.OutTime) / (Asset.EndTime - Asset.OutTime));
                }
            }
            else if (_time > Asset.EndTime)
            {
                Weight = 0;
            }
        }

        void ITimelineClip.OnEnable()
        {
            var index = Track.Asset.clips.IndexOf(Clip.Asset);
            Track.AddClip(this, index);
        }

        void ITimelineClip.OnDisable()
        {
            Disconnect();
        }
    }
}
