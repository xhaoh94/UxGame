using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Ux
{
    public sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
    {
        public static readonly ReferenceEqualityComparer<T> Instance = new();

        public bool Equals(T x, T y) => ReferenceEquals(x, y);

        public int GetHashCode(T obj) => obj == null ? 0 : RuntimeHelpers.GetHashCode(obj);
    }
}
