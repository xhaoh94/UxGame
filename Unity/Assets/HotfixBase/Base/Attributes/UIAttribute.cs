using System;

namespace Ux
{
    /// <summary>
    /// 界面注册声明。
    /// 只承载「声明」，不承载运行时数据：id 由注册流程分配（0 = 自动），
    /// 父界面关系用 Type 表达，构造函数不允许有副作用——
    /// 注册扫描/编辑器工具随时会读取它，不能在读元数据时拉起单例。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class UIAttribute : Attribute
    {
        /// <summary>顶层界面，ID 自动分配。</summary>
        public UIAttribute()
        {
        }

        /// <summary>tab 子界面，声明父界面类型。</summary>
        public UIAttribute(Type parent)
        {
            this.parent = parent;
        }

        /// <summary>显式指定界面 ID。</summary>
        public UIAttribute(int id)
        {
            this.id = id;
        }

        /// <summary>显式指定界面 ID，并声明父界面类型。</summary>
        public UIAttribute(int id, Type parent)
        {
            this.id = id;
            this.parent = parent;
        }

        /// <summary>界面 ID，0 表示由 UIMgr 自动分配。</summary>
        public readonly int id;

        /// <summary>tab 子界面的父界面类型，null 表示顶层界面。</summary>
        public readonly Type parent;
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class LazyloadAttribute : Attribute
    {
        public LazyloadAttribute(params string[] lazyloads)
        {
            this.lazyloads = lazyloads;
        }
        public string[] lazyloads { get; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class TabTitleAttribute : Attribute
    {
        public TabTitleAttribute(object title)
        {
            this.Title = title;
        }
        public object Title { get; }
    }
}
