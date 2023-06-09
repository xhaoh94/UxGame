using System;
namespace Ux
{
    public static class MainEventMgrEx
    {
        public static void On(this EventMgr mgr, MainEventType eType, Action action)
        {
#if UNITY_EDITOR
            mgr.On($"Main.{nameof(MainEventType)}.{eType}" , (int)eType, action);
#else
            mgr.On((int)eType, action);
#endif
        }
        public static void On<A>(this EventMgr mgr, MainEventType eType, Action<A> action)
        {
#if UNITY_EDITOR
            mgr.On($"Main.{nameof(MainEventType)}.{eType}", (int)eType, action);
#else
            mgr.On((int)eType, action);
#endif
        }
        public static void On<A, B>(this EventMgr mgr, MainEventType eType, Action<A, B> action)
        {
#if UNITY_EDITOR
            mgr.On($"Main.{nameof(MainEventType)}.{eType}", (int)eType, action);
#else
            mgr.On((int)eType, action);
#endif
        }
        public static void On<A, B, C>(this EventMgr mgr, MainEventType eType, Action<A, B, C> action)
        {
#if UNITY_EDITOR
            mgr.On($"Main.{nameof(MainEventType)}.{eType}", (int)eType, action);
#else
            mgr.On((int)eType, action);
#endif
        }
        public static void Off(this EventMgr mgr, MainEventType eType, Action action)
        {
            mgr.Off((int)eType, action);
        }
        public static void Off<A>(this EventMgr mgr, MainEventType eType, Action<A> action)
        {
            mgr.Off((int)eType, action);
        }
        public static void Off<A, B>(this EventMgr mgr, MainEventType eType, Action<A, B> action)
        {
            mgr.Off((int)eType, action);
        }
        public static void Off<A, B, C>(this EventMgr mgr, MainEventType eType, Action<A, B, C> action)
        {
            mgr.Off((int)eType, action);
        }
        public static void Send(this EventMgr mgr, MainEventType eType)
        {
            mgr.Send((int)eType);
        }
        public static void Send<A>(this EventMgr mgr, MainEventType eType, A a)
        {
            mgr.Send((int)eType, a);
        }
        public static void Send<A, B>(this EventMgr mgr, MainEventType eType, A a, B b)
        {
            mgr.Send((int)eType, a, b);
        }
        public static void Send<A, B, C>(this EventMgr mgr, MainEventType eType, A a, B b, C c)
        {
            mgr.Send((int)eType, a, b, c);
        }

    }
}
