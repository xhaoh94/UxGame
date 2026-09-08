using System;
using UnityEngine;

namespace Ux
{
    [Serializable]
    public class AnimationClipAsset : TimelineClipAsset
    {
        public enum PostExtrapolate
        {
            None,
            Hold,
            Loop
        }

        public AnimationClip clip;
        public PostExtrapolate pre;
        public PostExtrapolate post;
        public override Type ClipType => typeof(TLAnimationClip);

        [HideInInspector]
        public int PreFrame = -1;
        [HideInInspector]
        public int PostFrame = -1;

        public override void RescaleFrames(float scale)
        {
            base.RescaleFrames(scale);
            if (PreFrame > 0)
            {
                PreFrame = ScaleFrame(PreFrame, scale);
            }
            if (PostFrame > 0)
            {
                PostFrame = ScaleFrame(PostFrame, scale);
            }
            ValidateData();
        }

        public override void ValidateData()
        {
            base.ValidateData();
            if (PreFrame >= 0)
            {
                PreFrame = Mathf.Min(PreFrame, StartFrame);
            }
            if (PostFrame >= 0 && PostFrame != int.MaxValue)
            {
                PostFrame = Mathf.Max(PostFrame, EndFrame);
            }
        }
    }
}
