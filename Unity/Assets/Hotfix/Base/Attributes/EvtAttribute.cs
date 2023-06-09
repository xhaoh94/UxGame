using System;

namespace Ux
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class EvtAttribute : Attribute, IEvtAttribute
    {
#if UNITY_EDITOR
        public string ETypeStr { get; }
#endif
        public int EType { get; }
        public EvtAttribute(MainEventType eType)
        {
            EType = (int)eType;
#if UNITY_EDITOR
            ETypeStr = $"Main.{nameof(EventType)}.{eType}";
#endif
        }

        public EvtAttribute(int eType)
        {
            EType = eType;
        }
        public EvtAttribute(EventType eType)
        {
            EType = (int)eType;
#if UNITY_EDITOR
            ETypeStr = $"Hotfix.{nameof(EventType)}.{eType}";
#endif
        }

    }
}