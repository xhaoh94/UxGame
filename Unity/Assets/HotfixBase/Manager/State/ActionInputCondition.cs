namespace Ux
{
    public class ActionInputCondition : StateConditionBase
    {
        public override ConditionType Condition => ConditionType.Action_Input;
        public InputType Input { get; }
        public TriggerType Trigger { get; }
        public ActionInputCondition(InputType input, TriggerType trigger)
        {
            Input = input;
            Trigger = trigger;
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
