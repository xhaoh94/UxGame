using System;
using System.Collections.Generic;
using System.Linq;

namespace Ux
{
    public static class HashSetEx
    {
        public static (T, bool) Find<T>(this HashSet<T> hashSet, Func<T, bool> check)
        {
            var e = hashSet.GetEnumerator();
            while (e.MoveNext())
            {
                var curr = e.Current;
                if (check(curr))
                {
                    return (curr, true);
                }
            }
            return (default, false);
        }
        
    }
}