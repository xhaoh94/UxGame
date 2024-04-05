using System;

namespace Ux
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class UIAttribute : Attribute
    {
        public UIAttribute()
        {

        }
        public UIAttribute(int id)
        {
            this.id = id;
        }
        public UIAttribute(int id, int pId)
        {
            this.id = id;
            tabData = new UITabData(pId);
        }
        public UIAttribute(int id, Type pId)
        {
            this.id = id;
            tabData = new UITabData(pId.FullName.ToHash());
        }

        public UIAttribute(Type pId)
        {
            tabData = new UITabData(pId.FullName.ToHash());
        }

        public UIAttribute(IUITabData tabData)
        {
            this.tabData = tabData;
        }
        public UIAttribute(int id, IUITabData tabData)
        {
            this.id = id;
            this.tabData = tabData;
        }
        public readonly int id;
        public readonly IUITabData tabData;
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class PackageAttribute : Attribute
    {
        public PackageAttribute(params string[] pkgs)
        {
            this.pkgs = pkgs;
        }

        public string[] pkgs { get; }
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

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class UIComponentAttribute : Attribute
    {
        public UIComponentAttribute()
        {
        }

        public UIComponentAttribute(Type t)
        {
            Component = t;
        }

        public Type Component { get; }
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

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ItemUrlAttribute : Attribute
    {
        public ItemUrlAttribute(string url)
        {
            Url = url;
        }

        public string Url { get; }
    }
}