using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

namespace Ux
{
    public class StateTempBoolVarPA : PlayableAsset
    {
        public string Key;
        public override Playable CreatePlayable(PlayableGraph graph, GameObject go)
        {
            var playable = ScriptPlayable<StateTempBoolVarPB>.Create(graph);
            var behaviour = playable.GetBehaviour();
            behaviour.Key = Key;
            behaviour.go = go;
            return playable;
        }
    }
    [Serializable]
    public class StateTempBoolVarPB : PlayableBehaviour
    {
        public string Key;
        public GameObject go;
        public override void OnGraphStart(Playable playable)
        {
        }

        public override void OnGraphStop(Playable playable)
        {

        }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            if (go == null) return;
            var mono = go.GetComponent<EntityMono>();
            if (mono == null) return;
            var player = mono.GetEntity<Unit>();
            if (player == null) return;
            StateMgr.Ins.AddTempBoolVar(player.ID, Key);
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (go == null) return;
            var mono = go.GetComponent<EntityMono>();
            if (mono == null) return;
            var player = mono.GetEntity<Unit>();
            if (player == null) return;
            StateMgr.Ins.RevemoTempBoolVar(player.ID, Key);
        }

        public override void PrepareFrame(Playable playable, FrameData info)
        {

        }
    }
}