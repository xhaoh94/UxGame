using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ux
{
    internal class ActionInputCondition : StateConditionBase
    {
        public Input Inp { get; }
        public Trigger Tri { get; }
        public ActionInputCondition(StateMachine machine, Input input, Trigger trigger) : base(machine)
        {
            this.Inp = input;
            this.Tri = trigger;
        }

        public override bool IsValid
        {
            get
            {
                return base.IsValid;
            }
        }
    }
}
