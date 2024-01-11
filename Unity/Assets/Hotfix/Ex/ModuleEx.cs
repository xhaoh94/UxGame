
using System;
using System.Collections;
using UnityEngine;

namespace Ux
{
    public static class ModuleEx 
    {
        public static void On(this IModule ui, EventType eType, Action fn)
        {
            EventMgr.Ins.On(eType, ui, fn);
        }
        public static void On(this IModule ui, MainEventType eType, Action fn)
        {
            EventMgr.Ins.On(eType, ui, fn);
        }

        public static void On<A>(this IModule ui, EventType eType, Action<A> fn)
        {
            EventMgr.Ins.On(eType, ui, fn);
        }
        public static void On<A>(this IModule ui, MainEventType eType, Action<A> fn)
        {
            EventMgr.Ins.On(eType, ui, fn);
        }

        public static void On<A, B>(this IModule ui, EventType eType, Action<A, B> fn)
        {
            EventMgr.Ins.On(eType, ui, fn);
        }
        public static void On<A, B>(this IModule ui, MainEventType eType, Action<A, B> fn)
        {
            EventMgr.Ins.On(eType, ui, fn);
        }

        public static void On<A, B, C>(this IModule ui, EventType eType, Action<A, B, C> fn)
        {
            EventMgr.Ins.On(eType, ui, fn);
        }
        public static void On<A, B, C>(this IModule ui, MainEventType eType, Action<A, B, C> fn)
        {
            EventMgr.Ins.On(eType, ui, fn);
        }

        public static void Off(this IModule ui, EventType eType, Action fn)
        {
            EventMgr.Ins.Off(eType, ui, fn);
        }
        public static void Off(this IModule ui, MainEventType eType, Action fn)
        {
            EventMgr.Ins.Off(eType, ui, fn);
        }

        public static void Off<A>(this IModule ui, EventType eType, Action<A> fn)
        {
            EventMgr.Ins.Off(eType, ui, fn);
        }
        public static void Off<A>(this IModule ui, MainEventType eType, Action<A> fn)
        {
            EventMgr.Ins.Off(eType, ui, fn);
        }

        public static void Off<A, B>(this IModule ui, EventType eType, Action<A, B> fn)
        {
            EventMgr.Ins.Off(eType, ui, fn);
        }
        public static void Off<A, B>(this IModule ui, MainEventType eType, Action<A, B> fn)
        {
            EventMgr.Ins.Off(eType, ui, fn);
        }

        public static void Off<A, B, C>(this IModule ui, EventType eType, Action<A, B, C> fn)
        {
            EventMgr.Ins.Off(eType, ui, fn);
        }
        public static void Off<A, B, C>(this IModule ui, MainEventType eType, Action<A, B, C> fn)
        {
            EventMgr.Ins.Off(eType, ui, fn);
        }

    }
}