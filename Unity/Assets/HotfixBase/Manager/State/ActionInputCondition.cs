using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ux
{
    public class ActionInputCondition : StateConditionBase
    {
        public override Type ConditionType => Type.Action_Input;
        public Input Inp { get; }
        public Trigger Tri { get; }
        public ActionInputCondition(Input input, Trigger trigger)
        {
            this.Inp = input;
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
