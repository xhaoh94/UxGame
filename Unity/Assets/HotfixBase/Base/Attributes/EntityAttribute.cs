using System;

namespace Ux
{
    /// <summary>
    /// 监听添加了某个实体 只对同个父类实体且同层级的生效
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ListenAddEntityAttribute : Attribute
    {
        public Type ListenType { get; }
        public ListenAddEntityAttribute(Type type)
        {
            this.ListenType = type;
        }
    }
    /// <summary>
    /// 监听移除了某个实体 只对同个父类实体且同层级的生效
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ListenRemoveEntityAttribute : Attribute
    {
        public Type ListenType { get; }
        public ListenRemoveEntityAttribute(Type type)
        {
            this.ListenType = type;
        }
    }


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
