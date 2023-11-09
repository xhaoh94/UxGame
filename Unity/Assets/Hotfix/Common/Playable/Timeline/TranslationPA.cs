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
}
