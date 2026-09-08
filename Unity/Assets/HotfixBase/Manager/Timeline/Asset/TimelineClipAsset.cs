using System;
using UnityEngine;

namespace Ux
{
    [Serializable]
    public abstract class TimelineClipAsset
    {
        [SerializeField]
        private string id = Guid.NewGuid().ToString("N");

        public string clipName;

        // Clip 生效区间为 [StartFrame, EndFrame)。
        public int StartFrame;
        public int EndFrame = 1;
        // 混入结束帧，使用绝对帧；0 表示不混入。
        public int InFrame;
        // 混出开始帧，使用绝对帧；0 表示不混出。
        public int OutFrame;

        public string Id => id;
        public int DurationFrames => EndFrame - StartFrame;
        public abstract Type ClipType { get; }

        public virtual void RescaleFrames(float scale)
        {
            StartFrame = ScaleFrame(StartFrame, scale);
            EndFrame = ScaleFrame(EndFrame, scale);
            if (InFrame > 0)
            {
                InFrame = ScaleFrame(InFrame, scale);
            }
            if (OutFrame > 0)
            {
                OutFrame = ScaleFrame(OutFrame, scale);
            }
            NormalizeFrames();
        }

        public virtual void ValidateData()
        {
            EnsureId();
            NormalizeFrames();
        }

        public void RegenerateId()
        {
            id = Guid.NewGuid().ToString("N");
        }

        private void EnsureId()
        {
            if (string.IsNullOrEmpty(id))
            {
                RegenerateId();
            }
        }

        private void NormalizeFrames()
        {
            StartFrame = Mathf.Max(0, StartFrame);
            EndFrame = Mathf.Max(StartFrame + 1, EndFrame);

            if (InFrame > 0)
            {
                InFrame = Mathf.Clamp(InFrame, StartFrame, EndFrame);
            }
            if (OutFrame > 0)
            {
                OutFrame = Mathf.Clamp(OutFrame, StartFrame, EndFrame);
            }
        }

        protected static int ScaleFrame(int frame, float scale)
        {
            if (frame == int.MaxValue)
            {
                return int.MaxValue;
            }
            return Mathf.Max(0, Mathf.RoundToInt(frame * scale));
        }
    }
}
