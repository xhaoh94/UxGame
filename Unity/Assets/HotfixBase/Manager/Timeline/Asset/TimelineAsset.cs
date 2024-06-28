using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;

namespace Ux
{
    public class TimelineAsset : ScriptableObject
    {
        [SerializeReference]
        public List<TimelineTrackAsset> tracks = new List<TimelineTrackAsset>();
#if UNITY_EDITOR
        public void Save()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
#endif
    }
}
