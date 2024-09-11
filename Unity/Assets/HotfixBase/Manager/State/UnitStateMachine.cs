namespace Ux
{
    public class UnitStateMachine : StateMachine
    {
        public long ID;        
        public override IStateNode Enter(string entryNode)
        {
            var b = base.Enter(entryNode);
            StateMgr.Ins.Update(ID, StateConditionBase.ConditionType.State);
            return b;
        }
    }
}