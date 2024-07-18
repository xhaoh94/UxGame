using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    public class TimelineAsset : ScriptableObject
    {
        [SerializeReference]
        public List<TimelineTrackAsset> tracks = new List<TimelineTrackAsset>();

        public float MaxTime
        {
            get
            {
                float _maxTime = 0;
                foreach (var track in tracks)
                {
                    if (_maxTime < track.MaxTime)
                    {
                        _maxTime = track.MaxTime;
                    }
                }
                return _maxTime;
            }
        }
    }
}
