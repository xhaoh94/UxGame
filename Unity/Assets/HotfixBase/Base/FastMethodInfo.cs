using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Ux
{
    public struct FastMethodInfo
    {
        private delegate object ReturnValueDelegate(object instance, object[] arguments);

        private delegate void VoidDelegate(object instance, object[] arguments);

        public object Target { get; }
        public System.Delegate Method => _delegate;
        public bool IsValid => _delegate != null;

        readonly MethodInfo _methodInfo;
        readonly ReturnValueDelegate _delegate;

        public FastMethodInfo(object target, MethodInfo methodInfo)
        {
            _delegate = null;
            Target = target;
            _methodInfo = methodInfo;
            if (_methodInfo == null)
            {
                Log.Error("FastMethodInfo MethodInfo 为空");
                return;
            }

            var instanceExpression = Expression.Parameter(typeof(object), "instance");
            var argumentsExpression = Expression.Parameter(typeof(object[]), "arguments");
            var argumentExpressions = new List<Expression>();
            var parameterInfos = methodInfo.GetParameters();
            for (var i = 0; i < parameterInfos.Length; ++i)
            {
                var parameterInfo = parameterInfos[i];
                argumentExpressions.Add(Expression.Convert(
                    Expression.ArrayIndex(argumentsExpression, Expression.Constant(i)), parameterInfo.ParameterType));
            }

            if (methodInfo.ReflectedType == null) return;
            var callExpression =
                Expression.Call(
                    !methodInfo.IsStatic ? Expression.Convert(instanceExpression, methodInfo.ReflectedType) : null,
                    methodInfo, argumentExpressions);
            if (callExpression.Type == typeof(void))
            {
                var voidDelegate = Expression
                    .Lambda<VoidDelegate>(callExpression, instanceExpression, argumentsExpression).Compile();
                _delegate = (instance, arguments) =>
                {
                    voidDelegate(instance, arguments);
                    return null;
                };
            }
            else
                _delegate = Expression.Lambda<ReturnValueDelegate>(Expression.Convert(callExpression, typeof(object)),
                    instanceExpression, argumentsExpression).Compile();
        }


        public object Invoke(params object[] arguments)
        {
            return _delegate?.Invoke(Target, arguments);
        }

#if UNITY_EDITOR
        public string MethodName
        {
            get
            {                
                if (Target == null)
                    return _methodInfo.ReflectedType != null
                        ? $"静态：{_methodInfo.ReflectedType.FullName}.{_methodInfo.Name}"
                        : string.Empty;
                var TargetType = Target.GetType();
                return TargetType.Name.Contains("<>c")
                    ? $"匿名：{TargetType.FullName}.{_methodInfo.Name}"
                    : $"方法：{TargetType.FullName}.{_methodInfo.Name}";
            }
        }
#endif
    }
}