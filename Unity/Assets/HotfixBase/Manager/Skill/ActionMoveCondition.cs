using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ux
{
    public class ActionMoveCondition : StateConditionBase
    {
        public ActionMoveCondition(StateMachine machine) : base(machine)
        {

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
