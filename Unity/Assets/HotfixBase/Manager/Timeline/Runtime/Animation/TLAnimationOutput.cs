using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Ux
{
    public partial class TLAnimationOutput : Entity, IAwakeSystem<Animator, int>
    {
        public Animator Animator { get; private set; }
        public AnimationLayerMixerPlayable Mixer { get; private set; }
        public AnimationPlayableOutput PlayableOutput { get; private set; }
        public TLAnimationRoot Root => ParentAs<TLAnimationRoot>();
        public PlayableGraph PlayableGraph => Root.Component.PlayableGraph;

        private readonly List<TLAnimationTrack> _tracks = new(5);
        private int _slotIndex;

        void IAwakeSystem<Animator, int>.OnAwake(Animator animator, int slotIndex)
        {
            _slotIndex = slotIndex;
            Animator = animator;
            Mixer = AnimationLayerMixerPlayable.Create(PlayableGraph, 1);
            Mixer.SetInputWeight(0, 0);
            PlayableOutput = AnimationPlayableOutput.Create(PlayableGraph, animator.gameObject.name, animator);
            PlayableOutput.SetSourcePlayable(Mixer, 0);
        }

        protected override void OnDestroy()
        {
            Root?.RemoveOutput(_slotIndex);
            _tracks.Clear();

            if (PlayableOutput.IsOutputValid())
            {
                PlayableGraph.DestroyOutput(PlayableOutput);
            }
            if (Mixer.IsValid())
            {
                PlayableGraph.DestroySubgraph(Mixer);
            }

            Animator = null;
            _slotIndex = 0;
        }

        public void Connect(TLAnimationTrack track)
        {
            if (track == null || track.IsDestroy || _tracks.Contains(track))
            {
                return;
            }

            _tracks.Add(track);
            RebuildMixer();
        }

        public void Disconnect(TLAnimationTrack track)
        {
            if (track == null)
            {
                return;
            }

            if (_tracks.Remove(track))
            {
                track.Disconnect();
                RebuildMixer();
            }
            if (_tracks.Count == 0)
            {
                Parent = null;
            }
        }

        public bool IsContains(TLAnimationTrack timeline)
        {
            return timeline != null && !timeline.IsDestroy && _tracks.Contains(timeline);
        }

        private void RebuildMixer()
        {
            if (!PlayableGraph.IsValid() || !Mixer.IsValid())
            {
                return;
            }

            _tracks.RemoveAll(track => track == null || track.IsDestroy);
            _tracks.Sort(CompareTracks);
            var oldMixer = Mixer;
            foreach (var track in _tracks)
            {
                track.Disconnect();
            }
            var oldInputCount = oldMixer.GetInputCount();
            for (var index = 0; index < oldInputCount; index++)
            {
                PlayableGraph.Disconnect(oldMixer, index);
            }

            var nextMixer = AnimationLayerMixerPlayable.Create(PlayableGraph, _tracks.Count + 1);
            nextMixer.SetInputWeight(0, 0);
            Mixer = nextMixer;
            if (PlayableOutput.IsOutputValid())
            {
                PlayableOutput.SetSourcePlayable(Mixer, 0);
            }

            for (var index = 0; index < _tracks.Count; index++)
            {
                _tracks[index].Connect(index + 1);
            }

            PlayableGraph.DestroySubgraph(oldMixer);
        }

        private static int CompareTracks(TLAnimationTrack first, TLAnimationTrack second)
        {
            var layer = first.Timeline.PlaybackLayer.CompareTo(second.Timeline.PlaybackLayer);
            if (layer != 0)
            {
                return layer;
            }
            var timeline = first.Timeline.PlaybackOrder.CompareTo(second.Timeline.PlaybackOrder);
            return timeline != 0 ? timeline : first.TrackOrder.CompareTo(second.TrackOrder);
        }
    }
}
