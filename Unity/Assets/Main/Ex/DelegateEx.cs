using System;

namespace Ux
{
    public delegate TResult FuncEx<T1, T2, out TResult>(T1 arg1, out T2 arg2);
}
