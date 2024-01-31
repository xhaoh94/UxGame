using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Ux
{
    public enum StateEnterConditionType
    {
        Any,
        State,
        TempBoolVar,
        Action_Move,
        Action_Keyboard,
        Action_Input,
    }
    [Serializable]
    public class StateEnterCondition
    {
        public StateEnterConditionType Type = StateEnterConditionType.Any;
        public StateItemData ItemData = new StateItemData();
    }

    [Serializable]
    public class StateItemData
    {
        public enum ValidType
        {
            Include,
            Exclude
        }
        public enum InputType
        {

        }

        public string key;
        public bool value;

        public ValidType validType;
        public List<string> states = new List<string>();

        public Key keyType;
        public InputType inputType;
    }

    public class SkillMgr : Singleton<SkillMgr>
    {
        Dictionary<string, SkillAsset> resToData = new Dictionary<string, SkillAsset>();
        public async UniTask<SkillAsset> GetSkillAssetAsync(string res)
        {
            if (resToData.TryGetValue(res, out var data))
            {
                return data;
            }
            var asset = await ResMgr.Ins.LoadAssetAsync<TimelineAsset>(res);

            data = new SkillAsset(asset);
            resToData[res] = data;
            return data;
        }


    }
}
