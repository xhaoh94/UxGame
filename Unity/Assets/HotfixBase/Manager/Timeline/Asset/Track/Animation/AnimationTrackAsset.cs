using System;

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
