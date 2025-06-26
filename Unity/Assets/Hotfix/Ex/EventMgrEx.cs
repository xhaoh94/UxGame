using System;

namespace Ux
{
    public static class EventMgrEx
    {
        public static void On(this EventMgr mgr, EventType eType, object tag, Action action)
        {
            (mgr as IEventOn).On((int)eType, tag, action);
        }
        public static void On<A>(this EventMgr mgr, EventType eType, object tag, Action<A> action)
        {
            (mgr as IEventOn).On((int)eType, tag, action);
        }
        public static void On<A, B>(this EventMgr mgr, EventType eType, object tag, Action<A, B> action)
        {
            (mgr as IEventOn).On((int)eType, tag, action);
        }
        public static void On<A, B, C>(this EventMgr mgr, EventType eType, object tag, Action<A, B, C> action)
        {
            (mgr as IEventOn).On((int)eType, tag, action);
        }
        public static void Off(this EventMgr mgr, EventType eType, object tag, Action action)
        {
            (mgr as IEventOff).Off((int)eType, tag, action);
        }
        public static void Off<A>(this EventMgr mgr, EventType eType, object tag, Action<A> action)
        {
            (mgr as IEventOff).Off((int)eType, tag, action);
        }
        public static void Off<A, B>(this EventMgr mgr, EventType eType, object tag, Action<A, B> action)
        {
            (mgr as IEventOff).Off((int)eType, tag, action);
        }
        public static void Off<A, B, C>(this EventMgr mgr, EventType eType, object tag, Action<A, B, C> action)
        {
            (mgr as IEventOff).Off((int)eType, tag, action);
        }
        public static EventMgr.EventSystem Run(this EventMgr mgr, EventType eType)
        {
            return (mgr as IEventRun).Run((int)eType);
        }
        public static EventMgr.EventSystem Run<A>(this EventMgr mgr, EventType eType, A a)
        {
            return (mgr as IEventRun).Run((int)eType, a);
        }
        public static EventMgr.EventSystem Run<A, B>(this EventMgr mgr, EventType eType, A a, B b)
        {
            return (mgr as IEventRun).Run((int)eType, a, b);
        }
        public static EventMgr.EventSystem Run<A, B, C>(this EventMgr mgr, EventType eType, A a, B b, C c)
        {
            return (mgr as IEventRun).Run((int)eType, a, b, c);
        }

    }
}
