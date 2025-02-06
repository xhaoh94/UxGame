using System;
using UnityEngine;

namespace Ux
{
    public class ParticleClipAsset : TimelineClipAsset
    {
        public override Type ClipType => typeof(TLParticleClip);
       
        public ParticleSystem particle;
    }
}
