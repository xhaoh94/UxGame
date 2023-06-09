using System;
using System.Collections.Generic;

namespace Ux
{
    public class ConditionMgr : Singleton<ConditionMgr>
    {
        public readonly struct ConditionParse
        {
            public readonly int id;
            public readonly Type type;

            public ConditionParse(Type condition, int conditionID)
            {
                type = condition;
                id = conditionID;
            }
        }

        Dictionary<int, ICondition> _conditions = new Dictionary<int, ICondition>();
        Dictionary<int, Type> _idConditionType = new Dictionary<int, Type>();

        public void Add(List<ConditionParse> conditions)
        {
            foreach (var condition in conditions)
            {
                _idConditionType.Add(condition.id, condition.type);
            }
        }

        public bool IsValid()
        {
            return true;
        }
    }
}