using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor.VersionControl;
using UnityEngine.Animations;
using UnityEngine.Playables;
using static PlasticPipe.Server.MonitorStats;

namespace Ux
{

    public class TLAnimationTrack : TLAnimationBase, IAwakeSystem<TimelineTrack>
    {
        public TimelineTrack Track { get; private set; }
        public AnimationTrackAsset Asset =>Track.Asset as AnimationTrackAsset;        

        private AnimationMixerPlayable _mixer;
        void IAwakeSystem<TimelineTrack>.OnAwake(TimelineTrack track)
        {
            Track = track;
            _mixer = AnimationMixerPlayable.Create(Track.PlayableGraph, Asset.clips.Count);
            SetSourcePlayable(Track.PlayableGraph, _mixer);

            var timeline = ParentAs<Timeline>();
            var layer = timeline.Get<TLAnimationMixer>();
            layer ??= timeline.Add<TLAnimationMixer>();
            layer.Add(this);
        }

        public void ConnectClip(TLAnimationClip clip, int index)
        {
            clip.Connect(_mixer, index);
        }
    }
}
