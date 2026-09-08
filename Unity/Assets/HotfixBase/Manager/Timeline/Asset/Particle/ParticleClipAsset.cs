using System;

namespace Ux
{
    [Serializable]
    public class ParticleClipAsset : TimelineClipAsset
    {
        public override Type ClipType => typeof(TLParticleClip);
    }
}
