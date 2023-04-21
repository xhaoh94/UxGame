using System;
using System.Collections.Generic;

namespace Ux
{
    public static class TypeEx
    {
        public static T GetAttribute<T>(this Type type)
        {
            var t = typeof(T);
            foreach (var ator in type.GetCustomAttributes(t, false))
                return (T)ator;

            return default(T);
        }
        public static List<T> GetAttributes<T>(this Type type)
        {
            var t = typeof(T);
            var list = new List<T>();
            foreach (var ator in type.GetCustomAttributes(t, false))
            {
                list.Add((T)ator);
            }
            return list;
        }
    }
}