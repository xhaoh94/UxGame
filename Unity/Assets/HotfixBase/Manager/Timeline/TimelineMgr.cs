using System;
using System.Collections;
using UnityEngine;

namespace Ux
{
    public class TimelineMgr:Singleton<TimelineMgr>
    {
        public int FrameRate = 60;
        public float DeltaTime
        {
            get
            {
                if (Application.isPlaying)
                    return UnityEngine.Time.deltaTime;
                return 1f / FrameRate;
            }
        }

        public void Lerp(float targetTime, float deltaTime, Action<float> evaluateSplitDeltaTime, ref float lastTime)
        {
            if (Mathf.Abs(deltaTime) > DeltaTime)
            {
                int direction = deltaTime > 0 ? 1 : -1; //正向or逆向
                while (lastTime != targetTime)
                {
                    float splitDeltaTime = direction * DeltaTime;
                    if (direction == 1)
                    {
                        splitDeltaTime = Mathf.Min(splitDeltaTime, targetTime - lastTime);
                    }
                    else
                    {
                        splitDeltaTime = Mathf.Max(splitDeltaTime, targetTime - lastTime);
                    }
                    evaluateSplitDeltaTime(splitDeltaTime);
                    lastTime += splitDeltaTime;
                }
            }
            else
            {
                evaluateSplitDeltaTime(deltaTime);
                lastTime += deltaTime;
            }
        }

    }
}