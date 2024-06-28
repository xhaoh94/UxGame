using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Ux
{
    public partial class TLAnimationRoot : Entity,IAwakeSystem<TimelineComponent>
    {
        public TimelineComponent Component { get; private set; }
        public AnimationLayerMixerPlayable MixerRoot { get; private set; }
        private readonly List<TLAnimationMixer> _mixers = new List<TLAnimationMixer>(10);
        void IAwakeSystem<TimelineComponent>.OnAwake(TimelineComponent a)
        {
            Component= a;
            var animator=Component.Parent.Model.GetComponentInChildren<Animator>();
            string name = animator.gameObject.name;
            MixerRoot = AnimationLayerMixerPlayable.Create(Component.PlayableGraph);
            var _output = AnimationPlayableOutput.Create(Component.PlayableGraph, name, animator);
            _output.SetSourcePlayable(MixerRoot);
        }

        public void Play(TLAnimationMixer mixer)
        {
            if (mixer == null)
                return;
            if (!IsContains(mixer))
            {
                // 优先插入到一个空位
                int index = _mixers.FindIndex(x => x == null);
                if (index == -1)
                {
                    int inputCount = MixerRoot.GetInputCount();
                    MixerRoot.SetInputCount(inputCount + 1);

                    mixer.Connect(MixerRoot, inputCount);
                    _mixers.Add(mixer);
                }
                else
                {
                    mixer.Connect(MixerRoot, index);
                    _mixers[index] = mixer;
                }
            }

            for (int i = 0; i < _mixers.Count; i++)
            {
                var _mixer = _mixers[i];
                if (_mixer == null)
                    continue;

                if (_mixer == mixer)
                {
                    _mixer.StartWeightFade(1f, 0.3f);
                    _mixer.PlayNode();
                }
                else
                {
                    _mixer.StartWeightFade(0f, 0.3f);
                    _mixer.PauseNode();
                }
            }
        }
        public bool IsContains(TLAnimationMixer timeline)
        {
            foreach (var _timeline in _mixers)
            {
                if (_timeline == timeline)
                    return true;
            }
            return false;
        }
    }
}
