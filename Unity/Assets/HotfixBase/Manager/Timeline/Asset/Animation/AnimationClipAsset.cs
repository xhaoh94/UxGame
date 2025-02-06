using System;
using UnityEngine;

namespace Ux
{
    public class AnimationClipAsset : TimelineClipAsset
    {
        [Serializable]
        public enum PostExtrapolate
        {
            None,
            Hold,
            Loop
        }
        //动画片段
        public AnimationClip clip;

        //镜头外 -前处理
        public PostExtrapolate pre;

        //镜头外 -后处理
        public PostExtrapolate post;
        public override Type ClipType => typeof(TLAnimationClip);

        [HideInInspector]
        public int PreFrame;
        [HideInInspector]
        public int PostFrame;

        public float PreTime => PreFrame / TimelineMgr.Ins.FrameRate;
        public float PostTime => PostFrame / TimelineMgr.Ins.FrameRate;

    }
}
