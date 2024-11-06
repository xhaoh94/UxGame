using System;
using System.Collections.Generic;

namespace Ux
{
    [Serializable]
    public class StateCondition : StateConditionBase
    {
        public override ConditionType Condition => ConditionType.State;
        public StateType State;
        public List<string> States = new();

        public override bool IsValid
        {
            get
            {
                switch (State)
                {
                    case StateType.Any: return true;
                    case StateType.Include:
                        {
                            foreach (var state in States)
                            {
                                if (!UnitState.StateMachine.IsPlaying(state))
                                {
                                    return false;
                                }
                            }
                            return true;                            
                        }
                    case StateType.Exclude:
                        {
                            foreach (var state in States)
                            {
                                if (UnitState.StateMachine.IsPlaying(state))
                                {
                                    return false;
                                }
                            }
                            return true;
                        }
                }
                return false;
            }

        }
    }
}
