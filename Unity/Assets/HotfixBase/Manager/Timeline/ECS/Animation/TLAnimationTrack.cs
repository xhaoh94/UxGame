using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Animations;

namespace Ux
{

    public class TLAnimationTrack : TLAnimationBase, IAwakeSystem
    {
        public TimelineTrack Track => ParentAs<TimelineTrack>();
        public AnimationTrackAsset Asset => Track.Asset as AnimationTrackAsset;

        private AnimationMixerPlayable _mixer;
        void IAwakeSystem.OnAwake()
        {
            _mixer = AnimationMixerPlayable.Create(Track.Component.PlayableGraph, Asset.clips.Count);
            SetSourcePlayable(Track.Component.PlayableGraph, _mixer);

            var layer = Track.Timeline.GetComponent<TLAnimationMixer>();
            if (layer == null)
            {
                layer = Track.Timeline.AddComponent<TLAnimationMixer>();
            }
            layer.Add(Asset.Layer);
        }

        public void AddClip(TLAnimationClip clip, int index)
        {
            clip.Connect(_mixer, index);
        }
    }
}
