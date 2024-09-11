using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;

namespace Ux
{
    public class StateCondition : StateConditionBase
    {
        public override ConditionType Condition => ConditionType.State;
        public StateType State { get; }
        public HashSet<string> States { get; }

        public StateCondition(StateType type, HashSet<string> states)
        {
            State = type;
            States = states;
        }

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
                                if (!UnitState.Machine.Contains(state))
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
                                if (UnitState.Machine.Contains(state))
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
