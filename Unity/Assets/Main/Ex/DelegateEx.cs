using System;

public delegate TResult FuncEx<T1, T2, out TResult>(T1 arg1, out T2 arg2);

public static class DelegateEx
{
    public static string MethodName(this Delegate method, object target = null)
    {
        if (target == null)
        {
            target = method.Target;
        }
        if (target == null)
        {
            return method.Method.ReflectedType != null
                ? $"静态：{method.Method.ReflectedType.FullName}.{method.Method.Name}"
                : string.Empty;
        }
        var targetType = target.GetType();
        return targetType.Name.Contains("<>c")
            ? $"匿名：{targetType.FullName}.{method.Method.Name}"
            : $"方法：{targetType.FullName}.{method.Method.Name}";
    }
}