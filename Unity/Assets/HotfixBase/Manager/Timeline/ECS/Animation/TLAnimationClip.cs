using UnityEditor.VersionControl;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Ux
{
    public class TLAnimationClip : TLAnimationBase, IAwakeSystem<TimelineClip>, ITimelineClip
    {
        public TimelineClip Clip { get; private set; }
        public AnimationClipAsset Asset => Clip.Asset as AnimationClipAsset;
        public TLAnimationTrack Track => ParentAs<TLAnimationTrack>();

        private AnimationClipPlayable _source;
        private Playable _parent;

        void IAwakeSystem<TimelineClip>.OnAwake(TimelineClip clip)
        {
            Clip = clip;
            _source = AnimationClipPlayable.Create(Clip.PlayableGraph, Asset.clip);
            SetSourcePlayable(Clip.PlayableGraph, _source);            
        }

        float _time;
        public void SetTime(float time)
        {
            _time = Time = time;
            _Lerp(0);
            //TimelineMgr.Ins.Lerp(time, time, _Lerp, ref _time);
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

            PlayNode();
        }

        void ITimelineClip.OnEnable()
        {
            Log.Debug("OnEnable");
            var index = Track.Asset.clips.IndexOf(Asset);
            Track.ConnectClip(this, index);
        }

        void ITimelineClip.OnDisable()
        {
            Disconnect();
        }
    }
}
