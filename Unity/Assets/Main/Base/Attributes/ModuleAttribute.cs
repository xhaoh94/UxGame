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

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class NetAttribute : Attribute
    {
        public NetAttribute(uint cmd)
        {
            this.cmd = cmd;
        }
        public uint cmd { get; }
    }
}