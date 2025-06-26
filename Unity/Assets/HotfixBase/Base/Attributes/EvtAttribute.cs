using System;

namespace Ux
{
    public interface IEvtAttribute
    {
        int EType { get; }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public class MainEvtAttribute : Attribute, IEvtAttribute
    {
        public int EType { get; }

        public MainEvtAttribute(int eType)
        {
            EType = eType;
        }
        public MainEvtAttribute(MainEventType eType)
        {
            EType = (int)eType;
        }

    }
}
