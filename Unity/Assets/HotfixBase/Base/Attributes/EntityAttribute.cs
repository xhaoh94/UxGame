using System;

namespace Ux
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class, AllowMultiple = false)]
    public class EEViewerAttribute : Attribute
    {
        public string Name { get; }
        public Action<object> CB { get; }
        public EEViewerAttribute()
        {
            CB = null;
        }
        public EEViewerAttribute(string name = "")
        {
            Name = name;
            CB = null;
        }
        public EEViewerAttribute(Action<object> cb)
        {
            CB = cb;
        }
        public EEViewerAttribute(string name, Action<object> cb)
        {
            CB = cb;
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class NonEEViewerAttribute : Attribute
    {

    }
}
