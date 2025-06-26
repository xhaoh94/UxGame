using System;
using System.Collections.Generic;

namespace Ux
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public class EvtAttribute : Attribute, IEvtAttribute
    {
        public int EType { get; }
        public EvtAttribute(MainEventType eType)
        {
            EType = (int)eType;
        }

        public EvtAttribute(EventType eType)
        {
            EType = (int)eType;
        }
    }
}