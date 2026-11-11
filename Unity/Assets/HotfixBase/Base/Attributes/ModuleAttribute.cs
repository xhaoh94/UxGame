using System;
namespace Ux
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ModuleAttribute : Attribute
    {
        public ModuleAttribute()
        {
            this.priority = 0;
        }

        public ModuleAttribute(int priority)
        {
            this.priority = priority;
        }

        public int priority { get; }
    }
}