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
            _inputPort = Track.Asset.clips.IndexOf(animAsset);
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            animAsset = null;
            _inputPort = 0;
            if (_source.IsValid())
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
            PlayableGraph.Disconnect(Track.Mixer, _inputPort);
        }
        protected override void OnEvaluate(float deltaTime)
        {
            if (Time < animAsset.StartTime)
            {
                Weight = 0;
                _source.SetTime(0);
            }
            else if (animAsset.StartTime <= Time && Time <= animAsset.EndTime)
            {
                _source.SetTime(Time);
                if (Time > animAsset.InTime && Time < animAsset.OutTime)
                {
                    Weight = 1;
                }
                else if (Time < animAsset.InTime)
                {
                    Weight = (Time - animAsset.StartTime) / (animAsset.InTime - animAsset.StartTime);
                }
                else if (Time > animAsset.OutTime)
                {
                    Weight = 1 - ((Time - animAsset.OutTime) / (animAsset.EndTime - animAsset.OutTime));
                }
            }
            else if (Time > animAsset.EndTime)
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
