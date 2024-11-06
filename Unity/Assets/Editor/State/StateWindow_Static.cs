using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Ux.Editor.State
{
    public partial class StateWindow
    {
        public static StateAsset Asset { get; set; }
        public static void SetMute(StateConditionBase condition, bool isMute)
        {
            condition.isMute = isMute;
            if (isMute)
            {
                var oldIndex = StateWindow.Asset.conditions.FindIndex(x => x == condition);
                if (oldIndex != -1)
                {
                    StateWindow.Asset.conditions.RemoveAt(oldIndex);
                }
                StateWindow.Asset.conditions.Add(condition);
            }
        }
        public static StateConditionBase CreateCondition(StateConditionBase.ConditionType condition)
        {
            Type type = null;
            switch (condition)
            {
                case StateConditionBase.ConditionType.State:
                    return new StateCondition();
                case StateConditionBase.ConditionType.TempBoolVar:
                    type = typeof(TemBoolVarCondition);
                    break;
                case StateConditionBase.ConditionType.Action_Keyboard:
                    type = typeof(ActionKeyboardCondition);
                    break;
                case StateConditionBase.ConditionType.Action_Input:
                    type = typeof(ActionInputCondition);
                    break;
            }
            if (type == null)
            {
                Log.Error($"没有找到名字为{condition}的条件");
                return null;
            }
            return (StateConditionBase)Activator.CreateInstance(type);
        }
    }
}