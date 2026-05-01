using UnityEngine;

namespace Ux
{
    public enum UIParamType
    {
        A, B, C
    }

    public interface IUIParam
    {        
        bool TryGet<T>(out T t, UIParamType type = UIParamType.A);

        public static IUIParam Create<A>(A a)
        {
            return new UIParam<A>(a);
        }
        public static IUIParam Create<A, B>(A a, B b)
        {
            return new UIParam<A, B>(a, b);

        }
        public static IUIParam Create<A, B, C>(A a, B b, C c)
        {
            return new UIParam<A, B, C>(a,b,c);            
        }
    }


    public class UIParam<A> : IUIParam
    {
        A _a;

        public UIParam(A a)
        {
            _a = a;
        }

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

    public class UIParam<A, B> : IUIParam
    {
        A _a; B _b;

        public UIParam(A a, B b)
        {
            _a = a; _b = b;
        }

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

    public class UIParam<A, B, C> : IUIParam
    {
        A _a; B _b; C _c;

        public UIParam(A a, B b, C c)
        {
            _a = a; _b = b; _c = c;
        }
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