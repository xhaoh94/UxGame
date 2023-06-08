using System;

namespace Ux
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ConditionAttribute: Attribute
    {
        public int conditionID;
        public ConditionAttribute(ConditionType type)
        {
            conditionID = (int)type;
        }
    }
}