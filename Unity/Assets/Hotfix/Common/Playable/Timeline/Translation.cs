using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace Ux
{
    [System.Serializable]
    public class TranslationPA : PlayableAsset
    {
        public float dis;
        public float time;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject go)
        {
            var playable = ScriptPlayable<TranslationPB>.Create(graph);
            var behaviour = playable.GetBehaviour();
            behaviour.dis = dis;
            behaviour.time = time;
            behaviour.go = go;
            return playable;
        }
    }

    [Serializable]
    public class TranslationPB : PlayableBehaviour
    {
        public float dis;
        public float time;
        public GameObject go;

        public override void OnGraphStart(Playable playable)
        {
        }

        public override void OnGraphStop(Playable playable)
        {
            playable.GetGraph();
        }
        Vector3 target;
        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            if (go == null) return;
            var mono = go.GetComponent<EntityModel>();
            if (mono == null) return;
            var player = mono.GetEntity<Unit>();
            if (player == null) return;
            var forward = go.transform.forward.normalized;
            if (time == 0)
            {
                player.Position += (forward * dis);
            }
            else
            {
                target = player.Position + (forward * dis);
            }
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {

        }

        public override void PrepareFrame(Playable playable, FrameData info)
        {
            if (target != default)
            {
                if (go == null) return;
                var mono = go.GetComponent<EntityModel>();
                if (mono == null) return;
                var player = mono.GetEntity<Unit>();
                if (player == null) return;
                var dir = target - player.Position;
                player.Position += dir.normalized * (Time.fixedDeltaTime * time);
                if (Vector3.SqrMagnitude(dir) <= 0.1f)
                {
                    target = Vector3.zero;
                }
            }
        }
    }
}
