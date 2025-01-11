using System;
using UnityEngine;

namespace Ux
{
    [TLTrack("¶¯»­", 111, 212, 201)]
    [TLTrackClipType(typeof(AnimationClipAsset))]
    public class AnimationTrackAsset : TimelineTrackAsset
    {                
        public bool isAdditive;
        public AvatarMask avatarMask; 
        public override Type TrackType=> typeof(TLAnimationTrack);

        //[NonSerialized]
        //public Animator animator;
        //public override void SetBindObj(UnityEngine.Object obj)
        //{
        //    animator = obj as Animator;
        //}
    }   
}
