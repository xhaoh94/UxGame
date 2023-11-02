using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Ux
{
    public class AnimComponent : Entity, IAwakeSystem<Animator>, IUpdateSystem, IApplicationQuitSystem
    {
        private Dictionary<string, AnimClip> _animClips = new Dictionary<string, AnimClip>();
        private Dictionary<int, AnimMixer> _animMixers = new Dictionary<int, AnimMixer>();

        private PlayableGraph _graph;
        private AnimationPlayableOutput _output;
        private AnimationLayerMixerPlayable _mixerRoot;
        public Animator Animator { get; private set; }
        public void OnAwake(Animator animator)
        {
            Animator = animator;
            string name = animator.gameObject.name;
            _graph = PlayableGraph.Create(name);
            _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);

            _mixerRoot = AnimationLayerMixerPlayable.Create(_graph);
            _output = AnimationPlayableOutput.Create(_graph, name, animator);
            _output.SetSourcePlayable(_mixerRoot);
        }
        public void OnUpdate()
        {
            var deltaTime = Time.deltaTime;
            _graph.Evaluate(deltaTime);
        }

        public void OnApplicationQuit()
        {
            _graph.Destroy();
        }

        protected override void OnDestroy()
        {
            _graph.Destroy();
        }
        /// <summary>
		/// Play the graph
		/// </summary>
		public void PlayGraph()
        {
            _graph.Play();
        }

        /// <summary>
        /// Stop the graph
        /// </summary>
        public void StopGraph()
        {
            _graph.Stop();
        }

        /// <summary>
        /// 检测动画是否正在播放
        /// </summary>
        /// <param name="name">动画名称</param>
        public bool IsPlaying(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                Log.Warning("检测动画是否正在播放，名字为空");
                return false;
            }
            var animClip = GetAnimClip(name);
            if (animClip == null)
                return false;

            return animClip.IsConnect && animClip.IsPlaying;
        }

        /// <summary>
        /// 播放一个动画
        /// </summary>
        /// <param name="name">动画名称</param>
        /// <param name="fadeLength">融合时间</param>
        public void Play(string name, float fadeLength = 0.3f)
        {
            if (string.IsNullOrEmpty(name))
            {
                Log.Warning("播放一个动画，名字为空");
                return;
            }

            var animClip = GetAnimClip(name);
            if (animClip == null)
            {
                Log.Warning($"没有找到动画片段： {name}");
                return;
            }

            int layer = animClip.Layer;
            var animMixer = GetAnimMixer(layer);
            if (animMixer == null)
                animMixer = CreateAnimMixer(layer);

            if (animMixer.IsConnect == false)
                animMixer.Connect(_mixerRoot, animMixer.Layer);

            animMixer.Play(animClip, fadeLength);
        }

        /// <summary>
        /// 停止一个动画
        /// </summary>
        /// <param name="name">动画名称</param>
        public void Stop(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                Log.Warning("停止一个动画，名字为空");
                return;
            }

            var animClip = GetAnimClip(name);
            if (animClip == null)
            {
                Log.Warning($"没有找到动画片段： {name}");
                return;
            }

            if (animClip.IsConnect == false)
                return;

            var animMixer = GetAnimMixer(animClip.Layer);
            if (animMixer == null)
                throw new System.Exception("没获取到动画混合器");

            animMixer.Stop(animClip.Name);
        }
        /// <summary>
        /// 添加一个动画片段
        /// </summary>
        /// <param name="name">动画名称</param>
        /// <param name="clip">动画片段</param>
        /// <param name="layer">动画层级</param>
        public bool AddAnimation(string name, AnimationClip clip, int layer = 0)
        {
            if (string.IsNullOrEmpty(name))
                throw new System.ArgumentException("添加一个动画片段，名字为空");
            if (clip == null)
                throw new System.ArgumentNullException("添加一个动画片段，AnimationClip为空");
            if (layer < 0)
                throw new System.Exception("动画片段层级不能小于0");

            if (Has(name))
            {
                Log.Warning($"已存在动画片段: {name}");
                return false;
            }
            var animClip = AddChild<AnimClip, PlayableGraph, string, AnimationClip, int>(_graph, name, clip, layer);
            _animClips.Add(name, animClip);
            return true;
        }

        /// <summary>
		/// 移除一个动画片段
		/// </summary>
		/// <param name="name">动画名称</param>
		public bool RemoveAnimation(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                Log.Warning("移除动画片段，名字为空");
                return false;
            }
            if (!Has(name))
            {
                Log.Warning($"找不到动画片段 : {name}");
                return false;
            }

            var animClip = GetAnimClip(name);
            var animMixer = GetAnimMixer(animClip.Layer);
            if (animMixer != null)
                animMixer.RemoveClip(animClip.Name);

            animClip.Destroy();
            _animClips.Remove(animClip.Name);
            return true;
        }

        /// <summary>
		/// 是否包含一个动画状态
		/// </summary>
		/// <param name="name">动画名称</param>
		public bool Has(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                Log.Warning("是否包含一个动画状态，名字为空");
                return false;
            }
            return _animClips.ContainsKey(name);
        }

        private AnimClip GetAnimClip(string name)
        {
            _animClips.TryGetValue(name, out var animClip);
            return animClip;
        }
        private AnimMixer GetAnimMixer(int layer)
        {
            _animMixers.TryGetValue(layer, out var animMixer);
            return animMixer;
        }
        private AnimMixer CreateAnimMixer(int layer)
        {
            // Increase input count
            int inputCount = _mixerRoot.GetInputCount();
            if (layer == 0 && inputCount == 0)
            {
                _mixerRoot.SetInputCount(1);
            }
            else
            {
                if (layer > inputCount - 1)
                {
                    _mixerRoot.SetInputCount(layer + 1);
                }
            }
            var animMixer = AddChild<AnimMixer, PlayableGraph, int>(_graph, layer);
            _animMixers.Add(layer, animMixer);
            return animMixer;
        }
    }
}
