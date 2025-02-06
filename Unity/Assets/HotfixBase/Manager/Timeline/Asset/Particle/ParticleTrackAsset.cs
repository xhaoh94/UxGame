using System;
using UnityEngine;

namespace Ux
{
    [TLTrack("¶¯»­", 111, 212, 201)]
    [TLTrackClipType(typeof(ParticleClipAsset))]
    public class ParticleAssetTrack : TimelineTrackAsset
    {
        public override Type TrackType => typeof(TLParticleTrack);
    }
}
