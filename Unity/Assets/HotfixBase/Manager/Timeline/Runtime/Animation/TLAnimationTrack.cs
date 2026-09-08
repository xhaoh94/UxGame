using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Ux
{
    public class TLAnimationTrack : TimelineTrack
    {
        public AnimationTrackAsset Asset { get; private set; }
        public AnimationMixerPlayable Mixer { get; private set; }
        public TLAnimationRoot Root { get; private set; }
        public TLAnimationOutput Output { get; private set; }
        public override bool IsWeightFadeComplete =>
            !HasValidOutput || (!_isFading && Mathf.Approximately(Weight, _fadeWeight));

        private float _fadeSpeed;
        private float _fadeWeight;
        private bool _isFading;

        public int InputPort { get; private set; }
        public float Weight
        {
            get => HasValidOutput ? Output.Mixer.GetInputWeight(InputPort) : 0;
            private set
            {
                if (HasValidOutput)
                {
                    Output.Mixer.SetInputWeight(InputPort, Mathf.Clamp01(value));
                }
            }
        }

        private bool HasValidOutput =>
            Output != null &&
            Output.Mixer.IsValid() &&
            Output.IsContains(this) &&
            InputPort >= 0 &&
            InputPort < Output.Mixer.GetInputCount();

        protected override void OnStart(TimelineTrackAsset asset)
        {
            Asset = asset as AnimationTrackAsset;
            Root = Component.GetOrAdd<TLAnimationRoot>();
            Mixer = AnimationMixerPlayable.Create(PlayableGraph, Asset?.clips?.Count ?? 0);
            _fadeWeight = 0;
        }

        protected override void OnDestroy()
        {
            if (PlayableGraph.IsValid())
            {
                Output?.Disconnect(this);
                if (Mixer.IsValid())
                {
                    PlayableGraph.DestroySubgraph(Mixer);
                }
            }

            Output = null;
            Root = null;
            Asset = null;
            base.OnDestroy();
        }

        public override void OnBinding()
        {
            var animator = Component.GetBinding<Animator>(Asset);
            var nextOutput = Root == null ? null : Root.GetOutput(animator);
            if (nextOutput == Output)
            {
                if (Output != null && !Output.IsContains(this))
                {
                    Output.Connect(this);
                }
                return;
            }

            Output?.Disconnect(this);
            Output = nextOutput;
            if (Output != null)
            {
                Output.Connect(this);
                if (!_isFading)
                {
                    Weight = _fadeWeight;
                }
            }
        }

        public override void StartWeightFade(float destWeight, float fadeDuration)
        {
            _fadeWeight = Mathf.Clamp01(destWeight);
            if (fadeDuration <= 0 || !Application.isPlaying || !HasValidOutput)
            {
                Weight = _fadeWeight;
                _isFading = false;
                return;
            }

            _fadeSpeed = 1f / fadeDuration;
            _isFading = true;
        }

        protected override void OnEvaluate(in TimelineEvaluationContext context)
        {
            if (!_isFading)
            {
                return;
            }
            if (!HasValidOutput)
            {
                _isFading = false;
                return;
            }

            Weight = Mathf.MoveTowards(Weight, _fadeWeight, _fadeSpeed * Mathf.Abs(context.DeltaTime));
            if (Mathf.Approximately(Weight, _fadeWeight))
            {
                _isFading = false;
            }
        }

        public void Connect(int parentInputPort)
        {
            if (Output == null || !Mixer.IsValid())
            {
                return;
            }

            InputPort = parentInputPort;
            PlayableGraph.Connect(Mixer, 0, Output.Mixer, parentInputPort);
            Weight = _isFading ? 0 : _fadeWeight;

            if (Asset.avatarMask != null)
            {
                Output.Mixer.SetLayerMaskFromAvatarMask((uint)parentInputPort, Asset.avatarMask);
            }
            Output.Mixer.SetLayerAdditive((uint)parentInputPort, Asset.isAdditive);
        }

        public void Disconnect()
        {
            if (Output != null && PlayableGraph.IsValid() && Output.Mixer.IsValid() &&
                InputPort < Output.Mixer.GetInputCount())
            {
                PlayableGraph.Disconnect(Output.Mixer, InputPort);
            }
            InputPort = 0;
            if (Mathf.Approximately(_fadeWeight, 0f))
            {
                _isFading = false;
            }
        }
    }
}
