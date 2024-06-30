using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    [Serializable]
    public abstract class TimelineTrackAsset
    {
        public string Name;
        [SerializeReference]
        public List<TimelineClipAsset> clips = new List<TimelineClipAsset>();

        public abstract Type TrackType { get; }

        public float MaxTime
        {
            get
            {
                float _maxTime = 0;
                foreach (var clip in clips)
                {
                    if (_maxTime < clip.EndTime)
                    {
                        _maxTime = clip.EndTime;
                    }
                }
                return _maxTime;
            }
        }
    }
}
