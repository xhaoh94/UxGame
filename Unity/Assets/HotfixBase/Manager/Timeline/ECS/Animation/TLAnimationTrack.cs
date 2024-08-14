using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Ux
{

    public class TLAnimationTrack : TimelineTrack
    {
        public AnimationTrackAsset Asset { get; private set; }

        public AnimationMixerPlayable Mixer { get; private set; }
        TLAnimationLayer _layerMixer;
        protected override void OnStart(TimelineTrackAsset asset)
        {
            Asset = asset as AnimationTrackAsset;
            Mixer = AnimationMixerPlayable.Create(PlayableGraph, Asset.clips.Count);
            _layerMixer = Timeline.GetOrAdd<TLAnimationLayer>();
            _Connect();
        }
        protected override void OnEvaluate(float deltaTime)
        {
            _layerMixer?.OnEvaluate(deltaTime);
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            _DisConnect();
            if (Mixer.IsValid())
            {
                PlayableGraph.DestroySubgraph(Mixer);
            }
            Asset = null;
            _layerMixer = null;
        }

        void _Connect()
        {
            int inputCount = _layerMixer.Mixer.GetInputCount();
            int layer = Asset.Layer;
            if (layer == 0 && inputCount == 0)
            {
                _layerMixer.Mixer.SetInputCount(1);
            }
            else
            {
                if (layer > inputCount - 1)
                {
                    _layerMixer.Mixer.SetInputCount(layer + 1);
                }
            }

            PlayableGraph.Connect(Mixer, 0, _layerMixer.Mixer, layer);
            _layerMixer.Mixer.SetInputWeight(layer, 1);
        }

        void _DisConnect()
        {
            if (Component.PlayableGraph.IsValid())
            {
                Component.PlayableGraph.Disconnect(_layerMixer.Mixer, Asset.Layer);
            }
        }
    }
}
