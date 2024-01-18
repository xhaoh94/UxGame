using System;
namespace Ux
{
    public static class MainEventMgrEx
    {
        public static void On(this EventMgr mgr, MainEventType eType, object tag, Action action)
        {
#if UNITY_EDITOR
            (mgr as IEvenEditor).On($"Main.{nameof(MainEventType)}.{eType}", (int)eType, tag, action);
#else
            mgr.On((int)eType,tag, action);
#endif
        }
        public static void On<A>(this EventMgr mgr, MainEventType eType, object tag, Action<A> action)
        {
#if UNITY_EDITOR
            (mgr as IEvenEditor).On($"Main.{nameof(MainEventType)}.{eType}", (int)eType, tag, action);
#else
            mgr.On((int)eType,tag, action);
#endif
        }
        public static void On<A, B>(this EventMgr mgr, MainEventType eType, object tag, Action<A, B> action)
        {
#if UNITY_EDITOR
            (mgr as IEvenEditor).On($"Main.{nameof(MainEventType)}.{eType}", (int)eType, tag, action);
#else
            mgr.On((int)eType,tag, action);
#endif
        }
        public static void On<A, B, C>(this EventMgr mgr, MainEventType eType, object tag, Action<A, B, C> action)
        {
#if UNITY_EDITOR
            (mgr as IEvenEditor).On($"Main.{nameof(MainEventType)}.{eType}", (int)eType, tag, action);
#else
            mgr.On((int)eType,tag, action);
#endif
        }
        public static void Off(this EventMgr mgr, MainEventType eType, object tag, Action action)
        {
            mgr.Off((int)eType, tag, action);
        }
        public static void Off<A>(this EventMgr mgr, MainEventType eType, object tag, Action<A> action)
        {
            mgr.Off((int)eType, tag, action);
        }
        public static void Off<A, B>(this EventMgr mgr, MainEventType eType, object tag, Action<A, B> action)
        {
            mgr.Off((int)eType, tag, action);
        }
        public static void Off<A, B, C>(this EventMgr mgr, MainEventType eType, object tag, Action<A, B, C> action)
        {
            mgr.Off((int)eType, tag, action);
        }
        public static void Send(this EventMgr mgr, MainEventType eType)
        {
            mgr.Run((int)eType);
        }
        public static void Send<A>(this EventMgr mgr, MainEventType eType, A a)
        {
            mgr.Run((int)eType, a);
        }
        public static void Send<A, B>(this EventMgr mgr, MainEventType eType, A a, B b)
        {
            mgr.Run((int)eType, a, b);
        }
        public static void Send<A, B, C>(this EventMgr mgr, MainEventType eType, A a, B b, C c)
        {
            mgr.Run((int)eType, a, b, c);
        }

    }
}
