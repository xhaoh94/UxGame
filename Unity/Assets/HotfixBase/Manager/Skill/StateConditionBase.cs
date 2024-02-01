using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ux
{
    public class StateConditionBase
    {
        public enum Type
        {
            State,
            TempBoolVar,
            Action_Move,
            Action_Keyboard,
            Action_Input,
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
            Click
        }

        public StateMachine Machine { get; }
        public StateConditionBase(StateMachine machine)
        {
            Machine = machine;
        }
        public virtual bool IsValid => false;
    }
}
