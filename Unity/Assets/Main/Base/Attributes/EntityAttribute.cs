using System;

namespace Ux
{
    /// <summary>
    /// 监听添加了某个组件或实体 只对同个父类实体且同层级的生效
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
    /// 监听移除了某个组件或实体 只对同个父类实体且同层级的生效
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


    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class EntityViewerAttribute : Attribute
    {
        public string Name { get; }
        public Action<object> CB { get; }
        public EntityViewerAttribute()
        {            
            CB = null;
        }
        public EntityViewerAttribute(string name = "")
        {
            Name = name;
            CB = null;
        }
        public EntityViewerAttribute( Action<object> cb)
        {            
            CB = cb;
        }
        public EntityViewerAttribute(string name , Action<object> cb)
        {            
            CB = cb;
        }
    }
}
