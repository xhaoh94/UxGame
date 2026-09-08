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
            Mixer = AnimationLayerMixerPlayable.Create(PlayableGraph, 0);
            PlayableOutput = AnimationPlayableOutput.Create(PlayableGraph, animator.gameObject.name, animator);
            // AnimationLayerMixerPlayable 只有一个输出端口，不能使用 Output 在列表中的索引。
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
            if (track == null || IsContains(track))
            {
                return;
            }

            var index = _tracks.FindIndex(x => x == null);
            if (index < 0)
            {
                index = _tracks.Count;
                _tracks.Add(track);
                Mixer.SetInputCount(_tracks.Count);
            }
            else
            {
                _tracks[index] = track;
            }

            track.Connect(index);
        }

        public void Disconnect(TLAnimationTrack track)
        {
            if (track == null)
            {
                return;
            }

            for (var i = 0; i < _tracks.Count; i++)
            {
                if (_tracks[i] != track)
                {
                    continue;
                }

                track.Disconnect();
                _tracks[i] = null;
                break;
            }

            if (_tracks.FindIndex(x => x != null) < 0)
            {
                Parent = null;
            }
        }

        public bool IsContains(TLAnimationTrack timeline)
        {
            foreach (var track in _tracks)
            {
                if (track == timeline)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
