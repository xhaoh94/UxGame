using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    public class TimelineAsset : ScriptableObject
    {
        //����֡��
        public int FrameRate;

        [SerializeReference]
        public List<TimelineTrackAsset> tracks = new List<TimelineTrackAsset>();
    }
}
