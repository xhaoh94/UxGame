using System.Collections;
using UnityEngine;

namespace Ux
{
    public class UnitStateMachine : StateMachine
    {
        public long ID;
        public override bool Enter(string entryNode)
        {
            bool changed = false;
            if (CurrentNode == null || CurrentNode.Name != entryNode)
            {
                changed = true;
            }
            var b = base.Enter(entryNode);
            if (b && changed)
            {
                //StateMgr.Ins.Update(ID);
            }
            return b;
        }        
    }
}