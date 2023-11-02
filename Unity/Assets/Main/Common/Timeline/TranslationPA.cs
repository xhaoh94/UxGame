using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace Ux
{
    [System.Serializable]
    public class TranslationPA : PlayableAsset
    {
        public TranslationPB template = new TranslationPB();
        // Factory method that generates a playable based on this asset
        public override Playable CreatePlayable(PlayableGraph graph, GameObject go)
        {
            var playable = ScriptPlayable<TranslationPB>.Create(graph, template);
            return playable;
        }
    }
}
