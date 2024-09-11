namespace Ux
{
    public class TemBoolVarCondition : StateConditionBase
    {
        public override ConditionType Condition => ConditionType.TempBoolVar;
        public string Key { get; }        

        public TemBoolVarCondition(string key)
        {
            Key = key;            
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
