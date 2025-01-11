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

        private float _fadeSpeed = 0f;
        private float _fadeWeight = 0f;
        private bool _isFading = false;
        /// <summary>
        /// 输入端口
        /// </summary>
        public int InputPort { private set; get; }
        /// <summary>
        /// 权重值
        /// </summary>
        public float Weight
        {
            set
            {
                Output?.Mixer.SetInputWeight(InputPort, value);
            }
            get
            {
                if (Output == null) return 0;
                return Output.Mixer.GetInputWeight(InputPort);
            }
        }

        protected override void OnStart(TimelineTrackAsset asset)
        {
            Asset = asset as AnimationTrackAsset;
            Root = Component.GetOrAdd<TLAnimationRoot>();
            Mixer = AnimationMixerPlayable.Create(PlayableGraph, Asset.clips.Count);
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (PlayableGraph.IsValid())
            {
                Output?.Disconnect(this);
                if (Mixer.IsValid())
                {
                    PlayableGraph.DestroySubgraph(Mixer);
                }
            }
            Asset = null;
        }

        public override void OnBinding()
        {
            var animator = Component.GetBindObj<Animator>(Asset.trackName);
            Output = Root.GetOutput(animator);
            if (Output != null)
            {
                Output.Connect(this);
            }
        }
        public override void StartWeightFade(float destWeight, float fadeDuration)
        {
            if (fadeDuration <= 0 || !Application.isPlaying)
            {
                Weight = destWeight;
                _isFading = false;
                return;
            }

            //注意：保持统一的渐变速度
            _fadeSpeed = 1f / fadeDuration;
            _fadeWeight = destWeight;
            _isFading = true;
        }
        protected override void OnEvaluate(float deltaTime)
        {
            if (_isFading && Output != null)
            {
                Weight = Mathf.MoveTowards(Weight, _fadeWeight, _fadeSpeed * deltaTime);
                if (Mathf.Approximately(Weight, _fadeWeight))
                {
                    _isFading = false;
                }
            }
        }

        /// <summary>
        /// 连接到父节点
        /// </summary>
        /// <param name="parent">父节点对象</param>
        /// <param name="inputPort">父节点上的输入端口</param>
        public void Connect(int parentInputPort)
        {
            if (Output == null) return;
            InputPort = parentInputPort;

            // 重置节点
            _fadeSpeed = 0;
            _fadeWeight = 0;
            _isFading = false;
            Weight = 0;

            // 连接
            PlayableGraph.Connect(Mixer, 0, Output.Mixer, parentInputPort);
            if (Asset.avatarMask != null)
            {
                Output.Mixer.SetLayerMaskFromAvatarMask((uint)parentInputPort, Asset.avatarMask);
            }
            if (Asset.isAdditive)
            {
                Output.Mixer.SetLayerAdditive((uint)parentInputPort, true);
            }
        }
        /// <summary>
        /// 同父节点断开连接
        /// </summary>
        public void Disconnect()
        {
            _isFading = false;
            _fadeSpeed = 0;
            _fadeWeight = 0;
            // 断开
            if (Output != null && PlayableGraph.IsValid() && Output.Mixer.IsValid())
            {
                PlayableGraph.Disconnect(Output.Mixer, InputPort);
            }
            InputPort = 0;
        }
    }
}
