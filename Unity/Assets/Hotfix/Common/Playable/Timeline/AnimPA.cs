using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[System.Serializable]
public class AnimPA : AnimationPlayableAsset //PlayableAsset
{

    public override Playable CreatePlayable(PlayableGraph graph, GameObject go)
    {
        return base.CreatePlayable(graph, go);
        //if (!Application.isPlaying)
        //{
        //}
        //else
        //{
        //    var scriptPlayable = ScriptPlayable<AnimPB>.Create(graph, 1);
        //    var animationBlendPlayable = scriptPlayable.GetBehaviour();
        //    animationBlendPlayable.Init(clip);
        //    return scriptPlayable;
        //}
    }
}
