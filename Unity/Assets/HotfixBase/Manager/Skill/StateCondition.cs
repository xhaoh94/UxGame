using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ux
{
    public class StateCondition : StateConditionBase
    {
        public StateConditionBase.State StateType { get; }
        public HashSet<string> States { get; }

        public StateCondition(StateMachine machine, StateConditionBase.State _type, HashSet<string> _states)
            : base(machine)
        {
            StateType = _type;
            States = _states;
        }

        public override bool IsValid
        {
            get
            {
                switch (StateType)
                {
                    case State.Any: return true;
                    case State.Include:
                        {
                            var cur = Machine.CurrentNode;
                            if (cur != null)
                            {
                                return States.Contains(cur.Name);
                            }
                            break;
                        }
                    case State.Exclude:
                        {
                            var cur = Machine.CurrentNode;
                            if (cur != null)
                            {
                                return !States.Contains(cur.Name);
                            }
                            break;
                        }
                }
                return base.IsValid;
            }

        }
    }
}
