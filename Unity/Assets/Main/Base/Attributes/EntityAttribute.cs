using System;

namespace Ux
{
    /// <summary>
    /// 监听添加了某个组件或实体 只对同个父类实体且同层级的生效
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ListenAddEntityAttribute : Attribute
    {
        public Type type;
        public ListenAddEntityAttribute(Type type)
        {
            this.type = type;
        }
    }
    /// <summary>
    /// 监听移除了某个组件或实体 只对同个父类实体且同层级的生效
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ListenRemoveEntityAttribute : Attribute
    {
        public Type type;
        public ListenRemoveEntityAttribute(Type type)
        {
            this.type = type;
        }
    }
}
