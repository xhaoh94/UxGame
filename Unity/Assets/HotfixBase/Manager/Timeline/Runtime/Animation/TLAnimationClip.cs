using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Ux
{
    public class TLAnimationClip : TimelineClip
    {
        private AnimationClipPlayable _source;
        private new TLAnimationTrack Track => ParentAs<TLAnimationTrack>();
        private PlayableGraph PlayableGraph => Track.Component.PlayableGraph;
        private AnimationClipAsset _animAsset;

        protected override void OnStart(TimelineClipAsset asset)
        {
            _animAsset = asset as AnimationClipAsset;
            if (_animAsset?.clip == null || !PlayableGraph.IsValid())
            {
                return;
            }

            _source = AnimationClipPlayable.Create(PlayableGraph, _animAsset.clip);
            _source.SetDuration(_animAsset.clip.length);
            _source.SetSpeed(0);
            PlayableGraph.Connect(_source, 0, Track.Mixer, InputIndex);
            Track.Mixer.SetInputWeight(InputIndex, 0);
        }

        protected override void OnStop()
        {
            if (PlayableGraph.IsValid() && _source.IsValid())
            {
                Track.Mixer.SetInputWeight(InputIndex, 0);
                PlayableGraph.Disconnect(Track.Mixer, InputIndex);
                PlayableGraph.DestroySubgraph(_source);
            }
            _animAsset = null;
        }

        protected override void OnEnable()
        {
        }

        protected override void OnDisable()
        {
        }

        protected override void OnEvaluate(in TimelineEvaluationContext context)
        {
            if (_animAsset?.clip == null || !_source.IsValid())
            {
                return;
            }

            var currentFrame = context.CurrentFrame;
            var sampleFrame = 0;
            var weight = 0f;
            var durationFrames = Mathf.Max(1, _animAsset.DurationFrames);

            switch (Status)
            {
                case TLClipStatus.Ing:
                    sampleFrame = currentFrame - _animAsset.StartFrame;
                    weight = GetActiveWeight(currentFrame);
                    break;
                case TLClipStatus.Pre:
                    if (_animAsset.PreFrame < 0 || currentFrame < _animAsset.PreFrame)
                    {
                        break;
                    }
                    switch (_animAsset.pre)
                    {
                        case AnimationClipAsset.PostExtrapolate.Hold:
                            weight = 1;
                            sampleFrame = 0;
                            break;
                        case AnimationClipAsset.PostExtrapolate.Loop:
                            weight = 1;
                            sampleFrame = PositiveModulo(currentFrame - _animAsset.StartFrame, durationFrames);
                            break;
                    }
                    break;
                case TLClipStatus.Post:
                    if (_animAsset.PostFrame < 0 || currentFrame >= _animAsset.PostFrame)
                    {
                        break;
                    }
                    switch (_animAsset.post)
                    {
                        case AnimationClipAsset.PostExtrapolate.Hold:
                            weight = 1;
                            sampleFrame = durationFrames;
                            break;
                        case AnimationClipAsset.PostExtrapolate.Loop:
                            weight = 1;
                            sampleFrame = PositiveModulo(currentFrame - _animAsset.StartFrame, durationFrames);
                            break;
                    }
                    break;
            }

            SetWeight(weight);
            SetTime(FrameToTime(sampleFrame));
        }

        private float GetActiveWeight(int currentFrame)
        {
            var weight = 1f;
            if (_animAsset.InFrame > _animAsset.StartFrame && currentFrame < _animAsset.InFrame)
            {
                weight = Mathf.InverseLerp(_animAsset.StartFrame, _animAsset.InFrame, currentFrame);
            }
            if (_animAsset.OutFrame > _animAsset.StartFrame && currentFrame > _animAsset.OutFrame)
            {
                weight = Mathf.Min(weight, 1f - Mathf.InverseLerp(_animAsset.OutFrame, _animAsset.EndFrame, currentFrame));
            }
            return Mathf.Clamp01(weight);
        }

        private static int PositiveModulo(int value, int modulo)
        {
            var result = value % modulo;
            return result < 0 ? result + modulo : result;
        }

        private void SetWeight(float value)
        {
            if (_source.IsValid())
            {
                Track.Mixer.SetInputWeight(InputIndex, Mathf.Clamp01(value));
            }
        }

        private void SetTime(float value)
        {
            if (_source.IsValid())
            {
                _source.SetTime(Mathf.Max(0, value));
            }
        }
    }
}
