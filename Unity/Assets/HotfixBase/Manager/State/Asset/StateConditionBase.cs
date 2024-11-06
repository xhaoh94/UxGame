using System;
using UnityEngine;

namespace Ux
{
    [Serializable]
    public abstract class StateConditionBase
    {
        public enum ConditionType
        {
            State,
            TempBoolVar,            
            Action_Keyboard,
            Action_Input,
            Custom,
        }
        public enum StateType
        {
            Any,
            /// <summary>
            /// 包括
            /// </summary>
            Include,
            /// <summary>
            /// 排除
            /// </summary>
            Exclude
        }
        public enum InputType
        {
            Attack,
            Skill01,
            Skill02,
            Skill03,
        }
        public enum TriggerType
        {
            Down,
            Up,
            Pressed,
        }
        [HideInInspector]
        public bool isMute;
        public abstract bool IsValid { get; }
        public abstract ConditionType Condition { get; }
        public IUnitState UnitState { get; private set; }
        public void Init(IUnitState state)
        {
            UnitState = state;
        }
    }
}
