using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Ux
{
    public partial class TLAnimationRoot : Entity, IAwakeSystem
    {
        public TimelineComponent Component =>ParentAs<TimelineComponent>();
        public AnimationLayerMixerPlayable MixerRoot { get; private set; }
        private readonly List<TLAnimationLayer> _mixers = new(5);
        void IAwakeSystem.OnAwake()
        {            
            var animator = Component.Parent.Model.GetComponentInChildren<Animator>();
            string name = animator.gameObject.name;
            MixerRoot = AnimationLayerMixerPlayable.Create(Component.PlayableGraph);
            var _output = AnimationPlayableOutput.Create(Component.PlayableGraph, name, animator);
            _output.SetSourcePlayable(MixerRoot);
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            _mixers.Clear();
            if (MixerRoot.IsValid())
            {
                Component.PlayableGraph.DestroySubgraph(MixerRoot);
            }
        }
        public void Add(TLAnimationLayer layer)
        {
            if (layer == null)
                return;
            if (!IsContains(layer))
            {                
                int index = _mixers.FindIndex(x => x == null);
                if (index == -1)
                {
                    int inputCount = MixerRoot.GetInputCount();
                    MixerRoot.SetInputCount(inputCount + 1);

                    layer.Connect(inputCount);
                    _mixers.Add(layer);
                }
                else
                {
                    layer.Connect(index);
                    _mixers[index] = layer;
                }
            }
            for (int i = 0; i < _mixers.Count; i++)
            {
                var mixer = _mixers[i];
                if (mixer == null)
                    continue;

                if (mixer == layer)
                {
                    mixer.StartWeightFade(1f, 0.3f);
                }
                else
                {
                    mixer.StartWeightFade(0f, 0.3f);
                }
            }
        }
        public void Remove(TLAnimationLayer layer)
        {
            if (layer == null)
                return;            
            for (int i = 0; i < _mixers.Count; i++)
            {
                var mixer = _mixers[i];
                if(mixer == layer)
                {
                    _mixers[i] = null;
                    break;
                }
            }
            int index = _mixers.FindIndex(x => x != null);
            if (index == -1)
            {
                Parent = null;
            }            
        }
        public bool IsContains(TLAnimationLayer timeline)
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
