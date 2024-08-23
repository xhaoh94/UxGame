using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    public class TimelineAsset : ScriptableObject
    {
        //����֡��
        public float FrameRate;

        [SerializeReference]
        public List<TimelineTrackAsset> tracks = new List<TimelineTrackAsset>();

        public bool SetFrameRate(float frameRate)
        {
            if (frameRate == FrameRate) return false;
            var rate = frameRate / FrameRate;
            FrameRate = frameRate;
            foreach (var track in tracks)
            {
                int lastEnd = -1;
                TimelineClipAsset lastClip = null;
                foreach (var clip in track.clips)
                {
                    var frame = clip.EndFrame - clip.StartFrame;
                    if (lastEnd > 0 && lastClip!=null)
                    {
                        var off = Mathf.CeilToInt(rate * (clip.StartFrame - lastEnd));
                        clip.StartFrame = lastClip.EndFrame + off;
                    }
                    else
                    {
                        clip.StartFrame = Mathf.CeilToInt(rate * clip.StartFrame);
                    }
                    lastEnd = clip.EndFrame;
                    clip.EndFrame = clip.StartFrame + Mathf.CeilToInt(rate * frame);
                    lastClip = clip;
                }
            }
            return true;
        }
    }
}
