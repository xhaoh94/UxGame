using UnityEngine;

namespace Ux
{
    public class TLParticleTrack : TimelineTrack
    {
        public ParticleAssetTrack Asset { get; private set; }
        public ParticleSystem BoundParticle { get; private set; }

        protected override void OnStart(TimelineTrackAsset asset)
        {
            Asset = asset as ParticleAssetTrack;
        }

        public override void OnBinding()
        {
            BoundParticle = Component.GetBinding<ParticleSystem>(Asset);
        }

        protected override void OnEvaluate(in TimelineEvaluationContext context)
        {
        }

        protected override void OnDestroy()
        {
            BoundParticle = null;
            Asset = null;
            base.OnDestroy();
        }
    }
}
