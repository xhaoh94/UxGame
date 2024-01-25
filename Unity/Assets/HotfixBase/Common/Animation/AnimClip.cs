using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Ux
{
    public class AnimClip : AnimNode, IAwakeSystem<PlayableGraph, string, AnimationClip, int>
    {
        /// <summary>
        /// 动画层级
        /// </summary>
        public int Layer { get; private set; } = 0;        
        public AnimationClip Clip { get; private set; }

        private AnimationClipPlayable _clipPlayable;

        /// <summary>
        /// 动画长度
        /// </summary>
        public float ClipLength
        {
            get
            {
                if (Clip == null)
                    return 0f;
                if (Speed == 0f)
                    return Mathf.Infinity;
                return Clip.length / Speed;
            }
        }
        /// <summary>
		/// 归一化时间轴
		/// </summary>
		public float NormalizedTime
        {
            set
            {
                if (Clip == null)
                    return;
                Time = Clip.length * value;
            }

            get
            {
                if (Clip == null)
                    return 1f;
                return Time / Clip.length;
            }
        }

        /// <summary>
        /// 动画模式
        /// </summary>
        public WrapMode WrapMode
        {
            set
            {
                if (Clip != null)
                    Clip.wrapMode = value;
            }
            get
            {
                if (Clip == null)
                    return WrapMode.Default;
                return Clip.wrapMode;
            }
        }

        public void OnAwake(PlayableGraph graph, string a, AnimationClip b, int c)
        {            
            Name = a;
            Clip = b;
            Layer = c;

            _clipPlayable = AnimationClipPlayable.Create(graph, b);
            _clipPlayable.SetApplyFootIK(false);
            _clipPlayable.SetApplyPlayableIK(false);

            SetSourcePlayable(graph, _clipPlayable);
            if (b.wrapMode == WrapMode.Once)
            {
                _clipPlayable.SetDuration(b.length);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Layer = 0;            
            Clip = null;
        }
    }
}
