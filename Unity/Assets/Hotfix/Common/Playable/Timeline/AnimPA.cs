using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackClipType(typeof(AnimPB))]
[TrackBindingType(typeof(Animator))]
public class AnimTrack : AnimationTrack
{

}
[System.Serializable]
public class AnimPA : AnimationPlayableAsset//PlayableAsset
{
    private AnimPB template = new AnimPB();
    public override Playable CreatePlayable(PlayableGraph graph, GameObject go)
    {
        var outp = graph.GetOutput(0);
        Log.Debug(outp);
        //var mixer = AnimationMixerPlayable.Create(graph, 2);

        var clipPlayable = AnimationClipPlayable.Create(graph, clip);
        //graph.Connect(clipPlayable, 0, mixer, 0);
        return clipPlayable;

        //var playable = base.CreatePlayable(graph, go);
        //return ScriptPlayable<AnimPB>.Create(graph, template);
    }
}
