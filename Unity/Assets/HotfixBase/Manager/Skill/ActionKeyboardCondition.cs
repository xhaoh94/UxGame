using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.InputSystem;

namespace Ux
{
    public class ActionKeyboardCondition : StateConditionBase
    {
        public Key Key { get; }
        public Trigger Tri { get; }
        public ActionKeyboardCondition(StateMachine machine, Key key, Trigger trigger) : base(machine)
        {
            this.Key = key;
            this.Tri = trigger;
        }

        public override bool IsValid
        {
            get
            {
                return false;
            }
        }
    }
}
