using UnityEngine;

namespace Ux
{
    public class TLParticleClip : TimelineClip
    {
        private ParticleClipAsset _clipAsset;
        private new TLParticleTrack Track => ParentAs<TLParticleTrack>();
        private ParticleSystem Particle => Track.BoundParticle;

        protected override void OnStart(TimelineClipAsset asset)
        {
            _clipAsset = asset as ParticleClipAsset;
        }

        protected override void OnStop()
        {
            StopParticle();
            _clipAsset = null;
        }

        protected override void OnEvaluate(in TimelineEvaluationContext context)
        {
            var particle = Particle;
            if (Status != TLClipStatus.Ing || particle == null)
            {
                return;
            }

            var localTime = FrameToTime(context.CurrentFrame - _clipAsset.StartFrame);
            particle.Simulate(Mathf.Max(0, localTime), true, true, false);
        }

        protected override void OnEnable()
        {
            var particle = Particle;
            if (particle != null)
            {
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        protected override void OnDisable()
        {
            StopParticle();
        }

        private void StopParticle()
        {
            var particle = Particle;
            if (particle != null)
            {
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }
}
