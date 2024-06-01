using System.Collections;
using UnityEngine;

namespace Ux
{
    public enum UIParamType
    {
        A,
        B,
        C
    }
    public interface IUIParam
    {
        IUIParam Copy();
        bool TryGet<T>(out T t, UIParamType type);
        void Release();
        public static IUIParam Create<A>(A a)
        {
            var p = Pool.Get<UIParam<A>>();
            p.Init(a, true);
            return p;
        }
        public static IUIParam Create<A, B>(A a, B b)
        {
            var p = Pool.Get<UIParam<A, B>>();
            p.Init(a, b, true);
            return p;
        }
        public static IUIParam Create<A, B, C>(A a, B b, C c)
        {
            var p = Pool.Get<UIParam<A, B, C>>();
            p.Init(a, b, c, true);
            return p;
        }
    }
    public class UIParamBase
    {
        bool _fromPool;
        public bool IsRelease { get; private set; }
        protected void Init(bool fromPool)
        {
            _fromPool = fromPool;
            IsRelease = false;
        }
        public void Release()
        {
            if (IsRelease)
            {
                Log.Error("UIParam:重复回收了");
                return;
            }
            IsRelease = true;
            OnRelease();
            if (_fromPool)
            {
                Pool.Push(this);
                _fromPool = false;
            }
        }
        protected virtual void OnRelease()
        {
        }
    }
    public class UIParam<A> : UIParamBase, IUIParam
    {
        A _a;
        IUIParam IUIParam.Copy()
        {
            if (IsRelease)
            {
                Log.Error("拷贝UIParam错误：UIParam已被回收");
                return null;
            }
            return IUIParam.Create(_a);
        }
        public void Init(A a, bool fromPool)
        {
            Init(fromPool);
            _a = a;
        }
        protected override void OnRelease()
        {
            _a = default;
        }

        bool IUIParam.TryGet<T>(out T t,UIParamType type)
        {
            switch (type)
            {
                case UIParamType.A:
                    if (_a is T a)
                    {
                        t = a;
                        return true;
                    }
                    break;
            }            
            t = default;
            return false;
        }
    }
    public class UIParam<A, B> : UIParamBase, IUIParam
    {
        A _a;
        B _b;
        IUIParam IUIParam.Copy()
        {
            if (IsRelease)
            {
                Log.Error("拷贝UIParam错误：UIParam已被回收");
                return null;
            }
            return IUIParam.Create(_a,_b);
        }
        public void Init(A a, B b, bool fromPool)
        {
            Init(fromPool);
            _a = a;
            _b = b;
        }
        protected override void OnRelease()
        {
            _a = default;
            _b = default;
        }

        bool IUIParam.TryGet<T>(out T t, UIParamType type)
        {
            switch (type)
            {
                case UIParamType.A:
                    if (_a is T a)
                    {
                        t = a;
                        return true;
                    }
                    break;
                case UIParamType.B:
                    if (_b is T b)
                    {
                        t = b;
                        return true;
                    }
                    break;
            }
            t = default;
            return false;
        }
        
    }
    public class UIParam<A, B, C> : UIParamBase, IUIParam
    {
        A _a;
        B _b;
        C _c;
        IUIParam IUIParam.Copy()
        {
            if (IsRelease)
            {
                Log.Error("拷贝UIParam错误：UIParam已被回收");
                return null;
            }
            return IUIParam.Create(_a, _b,_c);
        }
        public void Init(A a, B b, C c, bool fromPool)
        {
            Init(fromPool);
            _a = a;
            _b = b;
            _c = c;
        }
        protected override void OnRelease()
        {
            _a = default;
            _b = default;
            _c = default;
        }

        bool IUIParam.TryGet<T>(out T t,UIParamType type)
        {
            switch (type)
            {
                case UIParamType.A:
                    if (_a is T a)
                    {
                        t = a;
                        return true;
                    }
                    break;
                case UIParamType.B:
                    if (_b is T b)
                    {
                        t = b;
                        return true;
                    }
                    break;
                case UIParamType.C:
                    if (_c is T c)
                    {
                        t = c;
                        return true;
                    }
                    break;
            }
            t = default;
            return false;
        }
    }
}
