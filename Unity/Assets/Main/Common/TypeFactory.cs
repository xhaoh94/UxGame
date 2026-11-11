using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Ux
{
    /// <summary>
    /// 无参实例化的反射替代：缓存 <see cref="ConstructorInfo"/>，
    /// 避免每次都走 <see cref="Activator.CreateInstance(Type)"/> 的类型与构造器查找。
    ///
    /// 这里刻意不用 Expression / DynamicMethod：
    /// IL2CPP 下没有 Reflection.Emit，编译期委托方案在真机上会失效。
    /// </summary>
    public static class TypeFactory
    {
        private static readonly Dictionary<Type, ConstructorInfo> _ctors = new(64);

        /// <summary>创建实例；类型没有无参构造函数时返回 null 并记录错误。</summary>
        public static object Create(Type type)
        {
            if (type == null) return null;

            if (!_ctors.TryGetValue(type, out var ctor))
            {
                ctor = type.GetConstructor(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, Type.EmptyTypes, null);
                _ctors[type] = ctor;
            }

            if (ctor == null)
            {
                Log.Error("{0} 没有无参构造函数，无法创建实例", type.FullName);
                return null;
            }

            try
            {
                return ctor.Invoke(null);
            }
            catch (TargetInvocationException e) when (e.InnerException != null)
            {
                // 和 Activator.CreateInstance 一致：抛出构造函数里的原始异常，而不是包一层反射异常。
                ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                return null;
            }
        }
    }
}
