﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ux
{
    public class CustomCondition : StateConditionBase
    {
        public override bool IsValid => false;

        public override ConditionType Condition => ConditionType.Custom;
        public CustomCondition()
        {

        }
        public CustomCondition(string args)
        {

        }
    }
}
