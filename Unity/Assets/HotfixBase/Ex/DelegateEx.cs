using System;

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