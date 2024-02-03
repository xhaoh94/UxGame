using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ux
{
    public class TemBoolVarCondition : StateConditionBase
    {
        public override Type ConditionType => Type.TempBoolVar;
        public string Key { get; }
        public bool Value { get; }

        public TemBoolVarCondition(string _key, bool _value)
        {
            this.Key = _key;
            this.Value = _value;
        }

        public override bool IsValid
        {
            get
            {
                return StateMgr.Ins.CheckTempBoolVar(UnitState.OwnerID, Key, Value);
            }
        }
    }
}
