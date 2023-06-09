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

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class MainEvtAttribute : Attribute, IEvtAttribute
    {
#if UNITY_EDITOR
        public string ETypeStr { get; }
#endif
        public int EType { get; }

        public MainEvtAttribute(int eType)
        {
            this.EType = eType;
        }
        public MainEvtAttribute(MainEventType eType)
        {
            this.EType = (int)eType;
#if UNITY_EDITOR
            this.ETypeStr = $"Main.{nameof(MainEventType)}.{eType}";
#endif
        }

    }
}
