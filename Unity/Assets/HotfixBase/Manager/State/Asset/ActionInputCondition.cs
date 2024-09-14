namespace Ux
{
    public class ActionInputCondition : StateConditionBase
    {
        public override ConditionType Condition => ConditionType.Action_Input;
        public InputType Input;
        public TriggerType Trigger;

        public override bool IsValid
        {
            get
            {
                return false;
            }
        }
    }
}
