using UnityEngine;

namespace Ux
{
    /// <summary>
    /// UI参数类型枚举
    /// 用于区分多个参数的类型（A、B、C三个槽位）
    /// </summary>
    public enum UIParamType
    {
        /// <summary>
        /// A类型参数
        /// </summary>
        A, 
        
        /// <summary>
        /// B类型参数
        /// </summary>
        B, 
        
        /// <summary>
        /// C类型参数
        /// </summary>
        C
    }

    /// <summary>
    /// UI参数接口，用于在显示UI时传递参数
    /// 支持0-3个任意类型的参数
    /// </summary>
    public interface IUIParam
    {        
        /// <summary>
        /// 尝试获取指定类型的参数
        /// </summary>
        /// <typeparam name="T">要获取的参数类型</typeparam>
        /// <param name="t">输出参数</param>
        /// <param name="type">参数类型槽位</param>
        /// <returns>如果获取成功返回true，否则返回false</returns>
        bool TryGet<T>(out T t, UIParamType type = UIParamType.A);

        /// <summary>
        /// 创建一个单参数UIParam
        /// </summary>
        /// <typeparam name="A">参数类型</typeparam>
        /// <param name="a">参数值</param>
        /// <returns>IUIParam实例</returns>
        public static IUIParam Create<A>(A a)
        {
            return new UIParam<A>(a);
        }
        
        /// <summary>
        /// 创建双参数UIParam
        /// </summary>
        /// <typeparam name="A">第一个参数类型</typeparam>
        /// <typeparam name="B">第二个参数类型</typeparam>
        /// <param name="a">第一个参数值</param>
        /// <param name="b">第二个参数值</param>
        /// <returns>IUIParam实例</returns>
        public static IUIParam Create<A, B>(A a, B b)
        {
            return new UIParam<A, B>(a, b);
        }
        
        /// <summary>
        /// 创建三参数UIParam
        /// </summary>
        /// <typeparam name="A">第一个参数类型</typeparam>
        /// <typeparam name="B">第二个参数类型</typeparam>
        /// <typeparam name="C">第三个参数类型</typeparam>
        /// <param name="a">第一个参数值</param>
        /// <param name="b">第二个参数值</param>
        /// <param name="c">第三个参数值</param>
        /// <returns>IUIParam实例</returns>
        public static IUIParam Create<A, B, C>(A a, B b, C c)
        {
            return new UIParam<A, B, C>(a,b,c);            
        }
    }


    /// <summary>
    /// 单参数UIParam实现类
    /// </summary>
    /// <typeparam name="A">参数类型</typeparam>
    public class UIParam<A> : IUIParam
    {
        A _a;

        /// <summary>
        /// 构造函数
        /// </summary>
        public UIParam(A a)
        {
            _a = a;
        }

        /// <summary>
        /// 尝试获取参数
        /// </summary>
        bool IUIParam.TryGet<T>(out T t, UIParamType type)
        {
            if (type == UIParamType.A && _a is T res)
            {
                t = res;
                return true;
            }
            t = default;
            return false;
        }
    }

    /// <summary>
    /// 双参数UIParam实现类
    /// </summary>
    /// <typeparam name="A">第一个参数类型</typeparam>
    /// <typeparam name="B">第二个参数类型</typeparam>
    public class UIParam<A, B> : IUIParam
    {
        A _a; B _b;

        /// <summary>
        /// 构造函数
        /// </summary>
        public UIParam(A a, B b)
        {
            _a = a; _b = b;
        }

        /// <summary>
        /// 尝试获取参数
        /// </summary>
        bool IUIParam.TryGet<T>(out T t, UIParamType type)
        {
            switch (type)
            {
                case UIParamType.A when _a is T resA:
                    t = resA; return true;
                case UIParamType.B when _b is T resB:
                    t = resB; return true;
            }
            t = default;
            return false;
        }
    }

    /// <summary>
    /// 三参数UIParam实现类
    /// </summary>
    /// <typeparam name="A">第一个参数类型</typeparam>
    /// <typeparam name="B">第二个参数类型</typeparam>
    /// <typeparam name="C">第三个参数类型</typeparam>
    public class UIParam<A, B, C> : IUIParam
    {
        A _a; B _b; C _c;

        /// <summary>
        /// 构造函数
        /// </summary>
        public UIParam(A a, B b, C c)
        {
            _a = a; _b = b; _c = c;
        }
        
        /// <summary>
        /// 尝试获取参数
        /// </summary>
        bool IUIParam.TryGet<T>(out T t, UIParamType type)
        {
            switch (type)
            {
                case UIParamType.A when _a is T resA:
                    t = resA; return true;
                case UIParamType.B when _b is T resB:
                    t = resB; return true;
                case UIParamType.C when _c is T resC:
                    t = resC; return true;
            }
            t = default;
            return false;
        }
    }
}