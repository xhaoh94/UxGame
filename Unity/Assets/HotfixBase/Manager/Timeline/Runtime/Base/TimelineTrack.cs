using System.Collections.Generic;
using UnityEngine.Playables;

namespace Ux
{
    public abstract class TimelineTrack : Entity, IAwakeSystem<TimelineTrackAsset>
    {
        public PlayableGraph PlayableGraph => Component.PlayableGraph;
        public TimelineComponent Component => Timeline.Component;
        public Timeline Timeline => ParentAs<Timeline>();
        public float Time => Timeline.Time;
        public bool IsDone { get; private set; }

        List<TimelineClip> _clips = new();
        void IAwakeSystem<TimelineTrackAsset>.OnAwake(TimelineTrackAsset asset)
        {
            OnStart(asset);
            foreach (var clipAsset in asset.clips)
            {
                var clip = Add(clipAsset.ClipType, clipAsset) as TimelineClip;
                _clips.Add(clip);
            }
        }
        protected override void OnDestroy()
        {
            _clips.Clear();
            IsDone = false;
        }
        public virtual void OnBinding()
        {

        }
        public void Evaluate(float deltaTime)
        {
            IsDone = true;
            foreach (var clip in _clips)
            {
                clip.Evaluate(deltaTime);
                if (!clip.IsDone && IsDone)
                {
                    IsDone = false;
                }
            }
            OnEvaluate(deltaTime);
        }

        protected abstract void OnStart(TimelineTrackAsset asset);
        protected abstract void OnEvaluate(float deltaTime);
        public virtual void StartWeightFade(float destWeight, float fadeDuration) { }
    }
}
