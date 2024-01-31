using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Ux
{
    public enum StateViewType
    {
        None,
        Anim,
        Timeline
    }
    [CreateAssetMenu(fileName = "StateSettingData")]
    public class StateSettingData : ScriptableObject
    {
        public List<StateData> StateSettings = new List<StateData>();

        /// <summary>
        /// 存储配置文件
        /// </summary>
        public void SaveFile()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            Debug.Log($"{nameof(StateSettingData)}.asset is saved!");
        }
    }
    [Serializable]
    public class StateData
    {
        public string StateName;
        public string Desc;
        public StateViewType ViewType;
        public string AnimName;
        public string TimeLineName;
        public List<StateEnterCondition> Conditions = new List<StateEnterCondition>();

    }
}
