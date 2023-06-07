using System;

namespace Ux
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class TagAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class BindTagAttribute : Attribute
    {
        public Type TagType { get; }
        public BindTagAttribute(Type type)
        {
            TagType = type;
        }
    }
}