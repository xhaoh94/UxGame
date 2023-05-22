using System;
using UnityEngine;

namespace Ux
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class UIAttribute : Attribute
    {
        public UIAttribute()
        {

        }
        public UIAttribute(int id)
        {
            this.id = id;
        }

        public UIAttribute(int pId, string title, int tagId = 0)
        {
            tabData = new UITabData(pId, title, tagId);
        }

        public UIAttribute(Type pId, string title, int tagId = 0)
        {
            tabData = new UITabData(Animator.StringToHash(pId.FullName), title, tagId);
        }

        public UIAttribute(int id, int pId, string title, int tagId = 0)
        {
            this.id = id;
            tabData = new UITabData(pId, title, tagId);
        }
        public UIAttribute(int id, Type pId, string title, int tagId = 0)
        {
            this.id = id;
            tabData = new UITabData(Animator.StringToHash(pId.FullName), title, tagId);
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