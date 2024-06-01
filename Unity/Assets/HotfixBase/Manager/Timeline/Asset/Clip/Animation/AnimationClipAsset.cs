using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Ux
{    
    public class AnimationClipAsset : TimelineClipAsset
    {              
        public AnimationClip clip;
        public override Type ClipType => typeof(TLAnimationClip);
    }
}
