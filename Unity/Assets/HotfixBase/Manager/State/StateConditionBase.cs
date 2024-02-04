using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ux
{
    public abstract class StateConditionBase
    {
        public enum Type
        {
            State,
            TempBoolVar,            
            Action_Keyboard,
            Action_Input,
            Custom,
        }
        public enum State
        {
            Any,
            //包括
            Include,
            //排除
            Exclude
        }
        public enum Input
        {
            Attack,
            Skill01,
            Skill02,
            Skill03,
        }
        public enum Trigger
        {
            Down,
            Up,
            Pressed,
        }
        public abstract bool IsValid { get; }
        public abstract Type ConditionType { get; }
        public IUnitState UnitState { get; private set; }
        public void Init(IUnitState state)
        {
            UnitState = state;
        }
    }
}
