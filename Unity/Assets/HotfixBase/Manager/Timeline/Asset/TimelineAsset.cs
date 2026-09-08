using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    public class TimelineAsset : ScriptableObject
    {
        public const int DefaultFrameRate = 60;

        [SerializeField, Min(1)]
        private int frameRate = DefaultFrameRate;

        [SerializeReference]
        public List<TimelineTrackAsset> tracks = new();

        public int FrameRate => frameRate;
        public int DurationFrames
        {
            get
            {
                var duration = 0;
                foreach (var track in tracks)
                {
                    if (track != null)
                    {
                        duration = Mathf.Max(duration, track.GetEndFrame());
                    }
                }
                return duration;
            }
        }
        public float Duration => FrameToTime(DurationFrames);

        public float FrameToTime(int frame)
        {
            return frame / (float)frameRate;
        }

        public int TimeToFrame(float time)
        {
            return Mathf.FloorToInt(time * frameRate);
        }

        /// <summary>
        /// 修改帧率时保持所有关键帧对应的时间位置不变。
        /// </summary>
        public bool SetFrameRate(int value)
        {
            var targetFrameRate = Mathf.Max(1, value);
            if (frameRate == targetFrameRate)
            {
                return false;
            }

            var scale = targetFrameRate / (float)frameRate;
            foreach (var track in tracks)
            {
                track?.RescaleFrames(scale);
            }

            frameRate = targetFrameRate;
            ValidateData();
            return true;
        }

        public T FindTrack<T>() where T : TimelineTrackAsset
        {
            foreach (var track in tracks)
            {
                if (track is T value)
                {
                    return value;
                }
            }
            return null;
        }

        public TimelineTrackAsset FindTrack(string trackId)
        {
            if (string.IsNullOrEmpty(trackId))
            {
                return null;
            }

            foreach (var track in tracks)
            {
                if (track?.Id == trackId)
                {
                    return track;
                }
            }
            return null;
        }

        public void ValidateData()
        {
            frameRate = Mathf.Max(1, frameRate);
            tracks ??= new List<TimelineTrackAsset>();

            var ids = new HashSet<string>();
            foreach (var track in tracks)
            {
                if (track == null)
                {
                    continue;
                }

                track.ValidateData();
                EnsureUniqueId(track, ids);
                foreach (var clip in track.clips)
                {
                    if (clip != null)
                    {
                        EnsureUniqueId(clip, ids);
                    }
                }
            }
        }

        private static void EnsureUniqueId(TimelineTrackAsset track, HashSet<string> ids)
        {
            if (!ids.Add(track.Id))
            {
                track.RegenerateId();
                ids.Add(track.Id);
            }
        }

        private static void EnsureUniqueId(TimelineClipAsset clip, HashSet<string> ids)
        {
            if (!ids.Add(clip.Id))
            {
                clip.RegenerateId();
                ids.Add(clip.Id);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ValidateData();
        }
#endif
    }
}
