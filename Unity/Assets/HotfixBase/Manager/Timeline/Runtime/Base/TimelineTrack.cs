using System.Collections.Generic;
using UnityEngine.Playables;

namespace Ux
{
    public abstract class TimelineTrack : Entity, IAwakeSystem<TimelineTrackAsset>
    {
        public PlayableGraph PlayableGraph => Component.PlayableGraph;
        public TimelineComponent Component => Timeline.Component;
        public Timeline Timeline => ParentAs<Timeline>();
        public int CurrentFrame => Timeline.CurrentFrame;
        public TimelineTrackAsset BaseAsset { get; private set; }
        public bool IsDone => CurrentFrame >= (BaseAsset?.GetEndFrame() ?? 0);
        public virtual bool IsWeightFadeComplete => true;

        private readonly List<TimelineClip> _clips = new();

        void IAwakeSystem<TimelineTrackAsset>.OnAwake(TimelineTrackAsset asset)
        {
            BaseAsset = asset;
            OnStart(asset);
            if (asset?.clips == null)
            {
                return;
            }

            for (var index = 0; index < asset.clips.Count; index++)
            {
                var clipAsset = asset.clips[index];
                if (clipAsset?.ClipType == null)
                {
                    continue;
                }

                if (Add(clipAsset.ClipType, clipAsset, index) is TimelineClip clip)
                {
                    _clips.Add(clip);
                }
            }
        }

        protected override void OnDestroy()
        {
            _clips.Clear();
            BaseAsset = null;
        }

        public virtual void OnBinding()
        {
        }

        public void StopImmediate()
        {
            foreach (var clip in _clips)
            {
                clip.StopImmediate();
            }
        }

        public void Evaluate(in TimelineEvaluationContext context)
        {
            foreach (var clip in _clips)
            {
                clip.Evaluate(context);
            }
            OnEvaluate(context);
        }

        protected abstract void OnStart(TimelineTrackAsset asset);
        protected abstract void OnEvaluate(in TimelineEvaluationContext context);
        public virtual void StartWeightFade(float destWeight, float fadeDuration) { }
    }
}
