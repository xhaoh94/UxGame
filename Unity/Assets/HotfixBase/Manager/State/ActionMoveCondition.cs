using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ux
{
    public class ActionMoveCondition : StateConditionBase
    {
        public override Type ConditionType => Type.Action_Move;
        public ActionMoveCondition()
        {

        }

        public override bool IsValid
        {
            get
            {                
                return StateMgr.Ins.CheckMove(UnitState.OwnerID);
            }
        }
    }
}
