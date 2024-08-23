using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Ux
{
    public class TLAnimationClip : TimelineClip
    {
        private AnimationClipPlayable _source;
        private int _inputPort;
        TLAnimationTrack Track => ParentAs<TLAnimationTrack>();
        public PlayableGraph PlayableGraph => Track.Component.PlayableGraph;
        AnimationClipAsset animAsset;
        protected override void OnStart(TimelineClipAsset asset)
        {
            animAsset = asset as AnimationClipAsset;
            _source = AnimationClipPlayable.Create(PlayableGraph, animAsset.clip);
            _source.SetDuration(animAsset.clip.length);
            _inputPort = Track.Asset.clips.IndexOf(animAsset);
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            animAsset = null;
            _inputPort = 0;
            if (PlayableGraph.IsValid() && _source.IsValid())
            {
                PlayableGraph.DestroySubgraph(_source);
            }
        }
        protected override void OnEnable()
        {
            PlayableGraph.Connect(_source, 0, Track.Mixer, _inputPort);
        }
        protected override void OnDisable()
        {
            if (PlayableGraph.IsValid() && Track.Mixer.IsValid())
            {
                PlayableGraph.Disconnect(Track.Mixer, _inputPort);
            }
        }
        protected override void OnEvaluate(float deltaTime)
        {
            if (Active)
            {
                float _time = Time - animAsset.StartTime;
                _source.SetTime(_time);
                var _inTime = animAsset.InTime;
                var _outTime = animAsset.OutTime;
                var _startTime = animAsset.StartTime;
                var _endTime = animAsset.EndTime;
                if ((_inTime == 0 || Time >= _inTime) && (_outTime == 0 || Time <= _outTime))
                {
                    Weight = 1;
                }
                else if (Time < _inTime)
                {
                    Weight = _time / (_inTime - _startTime);
                }
                else if (Time > _outTime)
                {
                    Weight = 1 - ((Time - _outTime) / (_endTime - _outTime));
                }
            }
            else
            {
                Weight = 0;
            }
        }
        /// <summary>
        /// 权重值
        /// </summary>
        float Weight
        {
            set
            {
                Track.Mixer.SetInputWeight(_inputPort, value);
            }
        }
    }
}
