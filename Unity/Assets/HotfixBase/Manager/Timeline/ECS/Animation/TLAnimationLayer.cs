using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Ux
{
    public class TLAnimationLayer : Entity, IAwakeSystem
    {
        public Timeline Timeline => ParentAs<Timeline>();
        public TimelineComponent Component => Timeline.Component;
        public PlayableGraph PlayableGraph => Component.PlayableGraph;
        public TLAnimationRoot Root { get; private set; }
        public AnimationLayerMixerPlayable Mixer { get; private set; }


        private float _fadeSpeed = 0f;
        private float _fadeWeight = 0f;
        private bool _isFading = false;
        void IAwakeSystem.OnAwake()
        {
            Mixer = AnimationLayerMixerPlayable.Create(PlayableGraph);
            Root = Component.GetOrAdd<TLAnimationRoot>();
            Root.Connect(this);            
        }
        protected override void OnDestroy()
        {
            if (PlayableGraph.IsValid())
            {
                Root.Disconnect(this);
                if (Mixer.IsValid())
                {                    
                    PlayableGraph.DestroySubgraph(Mixer);
                }
            }
            Root = null;
        }

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
                Root.MixerRoot.SetInputWeight(InputPort, value);
            }
            get
            {
                return Root.MixerRoot.GetInputWeight(InputPort);
            }
        }

        public void OnEvaluate(float deltaTime)
        {
            if (_isFading)
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
            InputPort = parentInputPort;

            // 重置节点
            _fadeSpeed = 0;
            _fadeWeight = 0;
            _isFading = false;
            Weight = 0;
            
            // 连接
            PlayableGraph.Connect(Mixer, 0, Root.MixerRoot, parentInputPort);
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
            if (PlayableGraph.IsValid() && Root.MixerRoot.IsValid())
            {
                PlayableGraph.Disconnect(Root.MixerRoot, InputPort);
            }
            InputPort = 0;
        }

        /// <summary>
        /// 开始权重值过渡
        /// </summary>
        /// <param name="destWeight">目标权重值</param>
        /// <param name="fadeDuration">过渡时间</param>
        public void StartWeightFade(float destWeight, float fadeDuration)
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

    }
}
