using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    public class TimelineAsset : ScriptableObject
    {
        [SerializeReference]
        public List<TimelineTrackAsset> tracks = new List<TimelineTrackAsset>();
    }
}
