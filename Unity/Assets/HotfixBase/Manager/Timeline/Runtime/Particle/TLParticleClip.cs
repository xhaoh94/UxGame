using UnityEngine;

namespace Ux
{
    public class TLParticleClip : TimelineClip
    {
        private ParticleClipAsset _clipAsset;
        private bool _hasPose;
        private PoseSnapshot _restorePose;
        private PoseSnapshot _appliedPose;
        private new TLParticleTrack Track => ParentAs<TLParticleTrack>();
        private ParticleSystem Particle => Track.BoundParticle;

        protected override void OnStart(TimelineClipAsset asset)
        {
            _clipAsset = asset as ParticleClipAsset;
            // Clip 会被池化复用，上一个持有者留下的位姿状态不能带过来。
            _hasPose = false;
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
            if (particle == null)
            {
                return;
            }

            if (_clipAsset != null)
            {
                var main = particle.main;
                main.startColor = _clipAsset.startColor;
                ApplyPose(particle);
            }
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        protected override void OnDisable()
        {
            RestorePose();
            StopParticle();
        }

        /// <summary>
        /// 位姿以偏移方式写进轨道绑定的那个粒子系统。同一个系统在同一单位内会被多个 Clip 先后占用，
        /// 所以覆盖与还原必须成栈：退出时只撤销自己写下的值，若已被别人覆盖就保持不动。
        /// </summary>
        private void ApplyPose(ParticleSystem particle)
        {
            var target = particle.transform;
            _restorePose = PoseSnapshot.Capture(target);
            _appliedPose = _restorePose.OffsetBy(
                _clipAsset.positionOffset,
                _clipAsset.rotationEuler,
                ScaleFactor);
            _hasPose = true;
            _appliedPose.Apply(target);
        }

        private void RestorePose()
        {
            if (!_hasPose)
            {
                return;
            }
            _hasPose = false;

            var particle = Particle;
            if (particle == null)
            {
                return;
            }

            var target = particle.transform;
            if (!_appliedPose.Matches(target))
            {
                return;
            }
            _restorePose.Apply(target);
        }

        private float ScaleFactor => _clipAsset.scaleFactor > 0f ? _clipAsset.scaleFactor : 1f;

        private void StopParticle()
        {
            var particle = Particle;
            if (particle != null)
            {
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private struct PoseSnapshot
        {
            private Vector3 position;
            private Quaternion rotation;
            private Vector3 scale;

            public static PoseSnapshot Capture(Transform transform)
            {
                return new PoseSnapshot
                {
                    position = transform.localPosition,
                    rotation = transform.localRotation,
                    scale = transform.localScale,
                };
            }

            public PoseSnapshot OffsetBy(Vector3 positionOffset, Vector3 rotationEuler, float scaleFactor)
            {
                return new PoseSnapshot
                {
                    position = position + positionOffset,
                    rotation = rotation * Quaternion.Euler(rotationEuler),
                    scale = scale * scaleFactor,
                };
            }

            public void Apply(Transform transform)
            {
                transform.localPosition = position;
                transform.localRotation = rotation;
                transform.localScale = scale;
            }

            public bool Matches(Transform transform)
            {
                return transform.localPosition == position &&
                       transform.localRotation == rotation &&
                       transform.localScale == scale;
            }
        }
    }
}
