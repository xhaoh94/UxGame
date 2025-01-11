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
        int _port;
        void IAwakeSystem<Animator, int>.OnAwake(Animator animator, int port)
        {
            _port = port;
            Animator = animator;
            string name = animator.gameObject.name;
            Mixer = AnimationLayerMixerPlayable.Create(PlayableGraph);
            PlayableOutput = AnimationPlayableOutput.Create(PlayableGraph, name, animator);
            PlayableOutput.SetSourcePlayable(Mixer, port);
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            Animator = null;
            _tracks.Clear();
            Root.RemoveOutput(_port);
            _port = 0;
            if (Mixer.IsValid())
            {
                PlayableGraph.DestroySubgraph(Mixer);                
            }
            if (PlayableOutput.IsOutputValid())
            {
                PlayableGraph.DestroyOutput(PlayableOutput);
            }
        }

        public void Connect(TLAnimationTrack track)
        {
            if (track == null)
                return;
            if (!IsContains(track))
            {
                int index = _tracks.FindIndex(x => x == null);
                if (index == -1)
                {
                    int inputCount = Mixer.GetInputCount();
                    Mixer.SetInputCount(inputCount + 1);

                    track.Connect(inputCount);
                    _tracks.Add(track);
                }
                else
                {
                    track.Connect(index);
                    _tracks[index] = track;
                }
            }
        }
        public void Disconnect(TLAnimationTrack track)
        {
            if (track == null)
                return;
            for (int i = 0; i < _tracks.Count; i++)
            {
                var mixer = _tracks[i];
                if (mixer == track)
                {
                    mixer.Disconnect();
                    _tracks[i] = null;
                    break;
                }
            }
            int index = _tracks.FindIndex(x => x != null);
            if (index == -1)
            {
                Parent = null;
            }
        }
        public bool IsContains(TLAnimationTrack timeline)
        {
            foreach (var _timeline in _tracks)
            {
                if (_timeline == timeline)
                    return true;
            }
            return false;
        }
    }
}