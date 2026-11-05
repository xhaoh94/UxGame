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
            tabData = new UITabData(UIMgr.Ins.GetTypeId(pId));
        }

        public UIAttribute(Type pId)
        {
            tabData = new UITabData(UIMgr.Ins.GetTypeId(pId));
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