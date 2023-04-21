using System;

namespace Ux
{
    public interface IEvtAttribute
    {
#if UNITY_EDITOR
        string ETypeStr { get; }
#endif
        int EType { get; }
    }
}

namespace Ux.Main
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class EvtAttribute : Attribute, IEvtAttribute
    {
#if UNITY_EDITOR
        public string ETypeStr { get; }
#endif
        public int EType { get; }

        public EvtAttribute(int eType)
        {
            this.EType = eType;
        }
        public EvtAttribute(EventType eType)
        {
            this.EType = (int)eType;
#if UNITY_EDITOR
            this.ETypeStr = $"Main.{nameof(EventType)}.{eType}";
#endif
        }

    }
}