using UnityEngine;

namespace Ux
{
    public class TLParticleClip : TimelineClip
    {
        ParticleClipAsset clipAsset;
        protected override void OnStart(TimelineClipAsset asset)
        {
            clipAsset = asset as ParticleClipAsset;
        }

        protected override void OnStop()
        {
            clipAsset = null;
        }

        protected override void OnEvaluate(float deltaTime)
        {                                       
            switch (Status)
            {
                case TLClipStatus.Ing:
                    {
                        SetTime(Time - clipAsset.StartTime);
                    }
                    break;                
            }                        
        }

        protected override void OnEnable()
        {

        }

        protected override void OnDisable()
        {

        }

        void SetTime(float value)
        {
            clipAsset.particleSystem.time = value;
        }
    }
}
