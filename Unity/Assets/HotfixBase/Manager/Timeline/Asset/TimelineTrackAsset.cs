using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    [Serializable]
    public abstract class TimelineTrackAsset
    {
        [SerializeField]
        private string id = Guid.NewGuid().ToString("N");

        public string trackName;

        [SerializeReference]
        public List<TimelineClipAsset> clips = new();

        public string Id => id;
        public abstract Type TrackType { get; }

        public int GetEndFrame()
        {
            var endFrame = 0;
            foreach (var clip in clips)
            {
                if (clip != null)
                {
                    endFrame = Mathf.Max(endFrame, clip.EndFrame);
                }
            }
            return endFrame;
        }

        public virtual void RescaleFrames(float scale)
        {
            foreach (var clip in clips)
            {
                clip?.RescaleFrames(scale);
            }
        }

        public virtual void ValidateData()
        {
            EnsureId();
            clips ??= new List<TimelineClipAsset>();
            foreach (var clip in clips)
            {
                clip?.ValidateData();
            }
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
    }
}
