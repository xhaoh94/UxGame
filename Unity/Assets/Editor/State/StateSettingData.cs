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
        public string StateName = "Empty";
        public string Desc = "未设置";
        public StateViewType ViewType;
        public string AnimName;
        public string TimeLineName;
        public List<StateCondition> Conditions = new List<StateCondition>();
    }
    public enum StateEnterConditionType
    {
        State,
        TempBoolVar,
        Action_Move,
        Action_Keyboard,
        Action_Input,
    }
    [Serializable]
    public class StateCondition
    {
        public StateEnterConditionType Type;
        public enum ValidType
        {
            Any,
            //包括
            Include,
            //排除
            Exclude
        }
        public enum InputType
        {
            Attack,
            Skill01,
            Skill02,
            Skill03,
        }
        public enum TriggerTimeType
        {
            Down,
            Up,
            Click
        }


        public string key;
        public bool value;

        public ValidType validType;
        public List<string> states = new List<string>();

        public TriggerTimeType triggerType = TriggerTimeType.Click;
        public Key keyType;
        public InputType inputType;
    }
}
