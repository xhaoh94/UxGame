using System;

namespace Ux
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class UIAttribute : Attribute
    {
        public UIAttribute()
        {

        }
        public UIAttribute(string id)
        {
            this.id = id;
        }

        public UIAttribute(string pId, string title, int tagId = 0)
        {
            tabData = new UITabData(pId, title, tagId);
        }
        
        public UIAttribute(Type pId, string title, int tagId = 0)
        {
            tabData = new UITabData(pId.FullName, title, tagId);
        }
        
        public UIAttribute(string id, string pId, string title, int tagId = 0)
        {
            this.id = id;
            tabData = new UITabData(pId, title, tagId);
        }
        public UIAttribute(string id, Type pId, string title, int tagId = 0)
        {
            this.id = id;
            tabData = new UITabData(pId.FullName, title, tagId);
        }
        public UIAttribute(IUITabData tabData)
        {
            this.tabData = tabData;
        }
        public UIAttribute(string id, IUITabData tabData)
        {
            this.id = id;
            this.tabData = tabData;
        }
        public readonly string id;
        public readonly IUITabData tabData;
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class PackageAttribute : Attribute
    {
        public PackageAttribute(params string[] pkgs)
        {
            this.pkgs = pkgs;
        }

        public string[] pkgs { get; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class LazyloadAttribute : Attribute
    {
        public LazyloadAttribute(params string[] lazyloads)
        {
            this.lazyloads = lazyloads;
        }
        public string[] lazyloads { get; }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
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


}