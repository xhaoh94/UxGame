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
    }
}
