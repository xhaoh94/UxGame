using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Ux
{
    public class TimelineAsset : ScriptableObject
    {
        [SerializeReference]
        public List<TimelineTrackAsset> tracks = new List<TimelineTrackAsset>();           
    }
}
