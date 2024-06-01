using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Ux
{
    [TLTrack("¶¯»­", 111, 212, 201)]
    [TLTrackClipType(typeof(AnimationClipAsset))]
    public class AnimationTrackAsset : TimelineTrackAsset
    {        
        public int Layer;        

        public override Type TrackType=> typeof(TLAnimationTrack);              

    }   
}
