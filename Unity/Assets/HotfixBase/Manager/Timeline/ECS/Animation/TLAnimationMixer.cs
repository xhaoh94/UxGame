using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Ux
{
    public class TLAnimationMixer : TLAnimationBase, IAwakeSystem
    {
        public Timeline Timeline => ParentAs<Timeline>();
        public TimelineComponent Component => Timeline.Component;
        AnimationMixerPlayable _mixer;
        void IAwakeSystem.OnAwake()
        {
            _mixer = AnimationMixerPlayable.Create(Component.PlayableGraph);
            SetSourcePlayable(Component.PlayableGraph, _mixer);
            var root = Component.Get<TLAnimationRoot>();
            root ??= Component.Add<TLAnimationRoot, TimelineComponent>(Component);
            root.Play(this);
        }
        public void Add(TLAnimationTrack track)
        {            
            int inputCount = _mixer.GetInputCount();
            int layer = track.Asset.Layer;
            if (layer == 0 && inputCount == 0)
            {
                _mixer.SetInputCount(1);
            }
            else
            {
                if (layer > inputCount - 1)
                {
                    _mixer.SetInputCount(layer + 1);
                }
            }

            track.Connect(_mixer, layer);
        }       
    }
}
