using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

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
        [Serializable]
        public class StateData
        {
            public string ClsName;
            public string StateName = "Empty";
            public string Desc = "未设置";
            public StateViewType ViewType;
            public string AnimName;
            public string TimeLineName;
            public List<StateCondition> Conditions = new List<StateCondition>();
        }
        [Serializable]
        public class StateCondition
        {
            public StateConditionBase.Type Type;

            public string key;
            public bool value;

            public StateConditionBase.State stateType;
            public List<string> states = new List<string>();

            public StateConditionBase.Trigger triggerType;
            public Key keyType;
            public StateConditionBase.Input inputType;
        }

        public string path = "Assets/Hotfix/CodeGen/State";
        public string ns = "Ux";

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



}
