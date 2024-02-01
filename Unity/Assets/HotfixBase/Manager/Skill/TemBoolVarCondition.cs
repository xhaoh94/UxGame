using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ux
{
    public class TemBoolVarCondition : StateConditionBase
    {
        public string Key { get; }
        public bool Value { get; }

        public TemBoolVarCondition(StateMachine machine, string _key, bool _value) : base(machine)
        {
            this.Key = _key;
            this.Value = _value;
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
