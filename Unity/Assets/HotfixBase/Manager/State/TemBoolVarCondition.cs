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

        public TemBoolVarCondition(string _key)
        {
            this.Key = _key;            
        }

        public override bool IsValid
        {
            get
            {
                return StateMgr.Ins.CheckTempBoolVar(UnitState.OwnerID, Key);
            }
        }
    }
}
