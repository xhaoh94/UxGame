using System;
namespace Ux
{
    public static class UIObjectEx
    {
        #region 事件
        public static void On(this UIObject ui, EventType eType, Action fn)
        {
            EventMgr.Ins.On(eType, ui, fn);
        }
        public static void On(this UIObject ui, MainEventType eType, Action fn)
        {
            EventMgr.Ins.On(eType, ui, fn);
        }

        public static void On<A>(this UIObject ui, EventType eType, Action<A> fn)
        {
            EventMgr.Ins.On(eType, ui, fn);
        }
        public static void On<A>(this UIObject ui, MainEventType eType, Action<A> fn)
        {
            EventMgr.Ins.On(eType, ui, fn);
        }

        public static void On<A, B>(this UIObject ui, EventType eType, Action<A, B> fn)
        {
            EventMgr.Ins.On(eType, ui, fn);
        }
        public static void On<A, B>(this UIObject ui, MainEventType eType, Action<A, B> fn)
        {
            EventMgr.Ins.On(eType, ui, fn);
        }

        public static void On<A, B, C>(this UIObject ui, EventType eType, Action<A, B, C> fn)
        {
            EventMgr.Ins.On(eType, ui, fn);
        }
        public static void On<A, B, C>(this UIObject ui, MainEventType eType, Action<A, B, C> fn)
        {
            EventMgr.Ins.On(eType, ui, fn);
        }

        public static void Off(this UIObject ui, EventType eType, Action fn)
        {
            EventMgr.Ins.Off(eType, ui, fn);
        }
        public static void Off(this UIObject ui, MainEventType eType, Action fn)
        {
            EventMgr.Ins.Off(eType, ui, fn);
        }

        public static void Off<A>(this UIObject ui, EventType eType, Action<A> fn)
        {
            EventMgr.Ins.Off(eType, ui, fn);
        }
        public static void Off<A>(this UIObject ui, MainEventType eType, Action<A> fn)
        {
            EventMgr.Ins.Off(eType, ui, fn);
        }

        public static void Off<A, B>(this UIObject ui, EventType eType, Action<A, B> fn)
        {
            EventMgr.Ins.Off(eType, ui, fn);
        }
        public static void Off<A, B>(this UIObject ui, MainEventType eType, Action<A, B> fn)
        {
            EventMgr.Ins.Off(eType, ui, fn);
        }

        public static void Off<A, B, C>(this UIObject ui, EventType eType, Action<A, B, C> fn)
        {
            EventMgr.Ins.Off(eType, ui, fn);
        }
        public static void Off<A, B, C>(this UIObject ui, MainEventType eType, Action<A, B, C> fn)
        {
            EventMgr.Ins.Off(eType, ui, fn);
        }
        #endregion

        #region 定时器 Time

        /// <summary>
        /// 循环回调
        /// </summary>
        /// <param name="delay">延时秒数</param>
        /// <param name="action">调用方法</param>
        /// <returns></returns>
        public static long DoLoop(this UIObject ui, float delay, Action action)
        {
            return DoTimer(ui, delay, 0, action);
        }

        /// <summary>
        /// 循环回调
        /// </summary>
        /// <param name="first">第一次触发秒数</param>
        /// <param name="delay">延时秒数</param>
        /// <param name="action">调用方法</param>
        /// <returns></returns>
        public static long DoLoop(this UIObject ui, float first, float delay, Action action)
        {
            return DoTimer(ui, first, delay, 0, action);
        }

        /// <summary>
        /// 单次回调
        /// </summary>
        /// <param name="delay">延时秒数</param>
        /// <param name="action">调用方法</param>
        /// <returns></returns>
        public static long DoOnce(this UIObject ui, float delay, Action action)
        {
            return DoTimer(ui, delay, 1, action);
        }

        /// <summary>
        /// 延时调用
        /// </summary>
        /// <param name="delay">延时秒数</param>
        /// <param name="repeat">调用次数 小于或等于0则循环 </param>
        /// <param name="action">调用方法</param>
        /// <param name="complete">结束回调</param>
        /// <returns></returns>
        public static long DoTimer(this UIObject ui, float delay, int repeat, Action action,
            Action complete = null)
        {
            return TimeMgr.Ins.DoTimer(delay, repeat, ui, action, complete);
        }

        public static long DoTimer(this UIObject ui, float first, float delay, int repeat, Action action,
            Action complete = null)
        {
            return TimeMgr.Ins.DoTimer(first, delay, repeat, ui, action, complete);
        }

        public static long DoTimer(this UIObject ui, float delay, int repeat, Action action,
            Action<object> complete, object completeParam)
        {
            return TimeMgr.Ins.DoTimer(delay, repeat, ui, action, complete, completeParam);
        }

        public static long DoTimer(this UIObject ui, float first, float delay, int repeat, Action action,
            Action<object> complete, object completeParam)
        {
            return TimeMgr.Ins.DoTimer(first, delay, repeat, ui, action, complete, completeParam);
        }

        /// <summary>
        /// 延时调用
        /// </summary>
        /// <param name="delay">延时秒数</param>
        /// <param name="repeat">调用次数 小于或等于0则循环 </param>
        /// <param name="action">调用方法</param>
        /// <param name="a">附加参数</param>
        /// <param name="complete">结束回调</param>
        /// <returns></returns>
        public static long DoTimer<A>(this UIObject ui, float delay, int repeat, Action<A> action, A a,
            Action complete = null)
        {
            return TimeMgr.Ins.DoTimer(delay, repeat, ui, action, a, complete);
        }

        public static long DoTimer<A>(this UIObject ui, float first, float delay, int repeat, Action<A> action, A a,
            Action complete = null)
        {
            return TimeMgr.Ins.DoTimer(first, delay, repeat, ui, action, a, complete);
        }

        public static long DoTimer<A>(this UIObject ui, float delay, int repeat, Action<A> action, A a,
            Action<object> complete, object completeParam)
        {
            return TimeMgr.Ins.DoTimer(delay, repeat, ui, action, a, complete, completeParam);
        }

        public static long DoTimer<A>(this UIObject ui, float first, float delay, int repeat, Action<A> action, A a,
            Action<object> complete, object completeParam)
        {
            return TimeMgr.Ins.DoTimer(first, delay, repeat, ui, action, a, complete, completeParam);
        }

        /// <summary>
        /// 延时调用
        /// </summary>
        /// <param name="delay">延时秒数</param>
        /// <param name="repeat">调用次数 小于或等于0则循环 </param>
        /// <param name="action">调用方法</param>
        /// <param name="a">附加参数</param>
        /// <param name="b">附加参数</param>
        /// <param name="complete">结束回调</param>
        /// <returns></returns>
        public static long DoTimer<A, B>(this UIObject ui, float delay, int repeat, Action<A, B> action, A a, B b,
            Action complete = null)
        {
            return TimeMgr.Ins.DoTimer(delay, repeat, ui, action, a, b, complete);
        }

        public static long DoTimer<A, B>(this UIObject ui, float first, float delay, int repeat, Action<A, B> action, A a, B b,
            Action complete = null)
        {
            return TimeMgr.Ins.DoTimer(first, delay, repeat, ui, action, a, b, complete);
        }

        public static long DoTimer<A, B>(this UIObject ui, float delay, int repeat, Action<A, B> action, A a, B b,
            Action<object> complete, object completeParam)
        {
            return TimeMgr.Ins.DoTimer(delay, repeat, ui, action, a, b, complete, completeParam);
        }

        public static long DoTimer<A, B>(this UIObject ui, float first, float delay, int repeat, Action<A, B> action, A a, B b,
            Action<object> complete, object completeParam)
        {
            return TimeMgr.Ins.DoTimer(first, delay, repeat, ui, action, a, b, complete, completeParam);
        }

        /// <summary>
        /// 延时调用
        /// </summary>
        /// <param name="delay">延时秒数</param>
        /// <param name="repeat">调用次数 小于或等于0则循环 </param>
        /// <param name="action">调用方法</param>        
        /// <param name="a">附加参数</param>
        /// <param name="b">附加参数</param>
        /// <param name="c">附加参数</param>
        /// <param name="complete">结束回调</param>
        /// <returns></returns>
        public static long DoTimer<A, B, C>(this UIObject ui, float delay, int repeat, Action<A, B, C> action, A a, B b, C c,
            Action complete = null)
        {
            return TimeMgr.Ins.DoTimer(delay, repeat, ui, action, a, b, c, complete);
        }

        public static long DoTimer<A, B, C>(this UIObject ui, float first, float delay, int repeat, Action<A, B, C> action, A a, B b, C c,
            Action complete = null)
        {
            return TimeMgr.Ins.DoTimer(first, delay, repeat, ui, action, a, b, c, complete);
        }

        public static long DoTimer<A, B, C>(this UIObject ui, float delay, int repeat, Action<A, B, C> action, A a, B b, C c,
            Action<object> complete, object completeParam)
        {
            return TimeMgr.Ins.DoTimer(delay, repeat, ui, action, a, b, c, complete, completeParam);
        }

        public static long DoTimer<A, B, C>(this UIObject ui, float first, float delay, int repeat, Action<A, B, C> action, A a, B b, C c,
            Action<object> complete, object completeParam)
        {
            return TimeMgr.Ins.DoTimer(first, delay, repeat, ui, action, a, b, c, complete, completeParam);
        }

        public static void RemoveTimer(this UIObject ui, Action action)
        {
            TimeMgr.Ins.RemoveTimer(ui, action);
        }

        public static void RemoveTimer<A>(this UIObject ui, Action<A> action)
        {
            TimeMgr.Ins.RemoveTimer(ui, action);
        }

        public static void RemoveTimer<A, B>(this UIObject ui, Action<A, B> action)
        {
            TimeMgr.Ins.RemoveTimer(ui, action);
        }

        public static void RemoveTimer<A, B, C>(this UIObject ui, Action<A, B, C> action)
        {
            TimeMgr.Ins.RemoveTimer(ui, action);
        }

        #endregion

        #region 定时器 Frame

        /// <summary>
        /// 延帧调用
        /// </summary>
        /// <param name="delay">延时帧数</param>
        /// <param name="repeat">调用次数 小于或等于0则循环 </param>
        /// <param name="action">调用方法</param>
        /// <param name="complete">结束回调</param>
        /// <returns></returns>
        public static long DoFrame(this UIObject ui, int delay, int repeat, Action action,
            Action complete = null)
        {
            return TimeMgr.Ins.DoFrame(delay, repeat, action, complete);
        }

        public static long DoFrame(this UIObject ui, int first, int delay, int repeat, Action action,
            Action complete = null)
        {
            return TimeMgr.Ins.DoFrame(first, delay, repeat, action, complete);
        }

        public static long DoFrame(this UIObject ui, int delay, int repeat, Action action,
            Action<object> complete, object completeParam)
        {
            return TimeMgr.Ins.DoFrame(delay, repeat, action, complete, completeParam);
        }

        public static long DoFrame(this UIObject ui, int first, int delay, int repeat, Action action,
            Action<object> complete, object completeParam)
        {
            return TimeMgr.Ins.DoFrame(first, delay, repeat, action, complete, completeParam);
        }

        /// <summary>
        /// 延帧调用
        /// </summary>
        /// <param name="delay">延时帧数</param>
        /// <param name="repeat">调用次数 小于或等于0则循环 </param>
        /// <param name="action">调用方法</param>        
        /// <param name="a">附加参数</param>
        /// <param name="complete">结束回调</param>
        /// <returns></returns>
        public static long DoFrame<A>(this UIObject ui, int delay, int repeat, Action<A> action, A a,
            Action complete = null)
        {
            return TimeMgr.Ins.DoFrame(delay, repeat, ui, action, a, complete);
        }

        public static long DoFrame<A>(this UIObject ui, int first, int delay, int repeat, Action<A> action, A a,
            Action complete = null)
        {
            return TimeMgr.Ins.DoFrame(first, delay, repeat, ui, action, a, complete);
        }

        public static long DoFrame<A>(this UIObject ui, int delay, int repeat, Action<A> action, A a,
            Action<object> complete, object completeParam)
        {
            return TimeMgr.Ins.DoFrame(delay, repeat, ui, action, a, complete, completeParam);
        }

        public static long DoFrame<A>(this UIObject ui, int first, int delay, int repeat, Action<A> action, A a,
            Action<object> complete, object completeParam)
        {
            return TimeMgr.Ins.DoFrame(first, delay, repeat, ui, action, a, complete, completeParam);
        }

        /// <summary>
        /// 延帧调用
        /// </summary>
        /// <param name="delay">延时帧数</param>
        /// <param name="repeat">调用次数 小于或等于0则循环 </param>
        /// <param name="action">调用方法</param>        
        /// <param name="a">附加参数</param>
        /// <param name="b">附加参数</param>
        /// <param name="complete">结束回调</param>
        /// <returns></returns>
        public static long DoFrame<A, B>(this UIObject ui, int delay, int repeat, Action<A, B> action, A a, B b,
            Action complete = null)
        {
            return TimeMgr.Ins.DoFrame(delay, repeat, ui, action, a, b, complete);
        }

        public static long DoFrame<A, B>(this UIObject ui, int first, int delay, int repeat, Action<A, B> action, A a, B b,
            Action complete = null)
        {
            return TimeMgr.Ins.DoFrame(first, delay, repeat, ui, action, a, b, complete);
        }

        public static long DoFrame<A, B>(this UIObject ui, int delay, int repeat, Action<A, B> action, A a, B b,
            Action<object> complete, object completeParam)
        {
            return TimeMgr.Ins.DoFrame(delay, repeat, ui, action, a, b, complete, completeParam);
        }

        public static long DoFrame<A, B>(this UIObject ui, int first, int delay, int repeat, Action<A, B> action, A a, B b,
            Action<object> complete, object completeParam)
        {
            return TimeMgr.Ins.DoFrame(first, delay, repeat, ui, action, a, b, complete, completeParam);
        }

        /// <summary>
        /// 延帧调用
        /// </summary>
        /// <param name="delay">延时帧数</param>
        /// <param name="repeat">调用次数 小于或等于0则循环 </param>
        /// <param name="action">调用方法</param>
        /// <param name="a">附加参数</param>
        /// <param name="b">附加参数</param>
        /// <param name="c">附加参数</param>
        /// <param name="complete">结束回调</param>
        /// <returns></returns>
        public static long DoFrame<A, B, C>(this UIObject ui, int delay, int repeat, Action<A, B, C> action, A a, B b, C c,
            Action complete = null)
        {
            return TimeMgr.Ins.DoFrame(delay, repeat, ui, action, a, b, c, complete);
        }

        public static long DoFrame<A, B, C>(this UIObject ui, int first, int delay, int repeat, Action<A, B, C> action, A a, B b, C c,
            Action complete = null)
        {
            return TimeMgr.Ins.DoFrame(first, delay, repeat, ui, action, a, b, c, complete);
        }

        public static long DoFrame<A, B, C>(this UIObject ui, int delay, int repeat, Action<A, B, C> action, A a, B b, C c,
            Action<object> complete, object completeParam)
        {
            return TimeMgr.Ins.DoFrame(delay, repeat, ui, action, a, b, c, complete, completeParam);
        }

        public static long DoFrame<A, B, C>(this UIObject ui, int first, int delay, int repeat, Action<A, B, C> action, A a, B b, C c,
            Action<object> complete, object completeParam)
        {
            return TimeMgr.Ins.DoFrame(first, delay, repeat, ui, action, a, b, c, complete, completeParam);
        }


        public static void RemoveFrame(this UIObject ui, Action action)
        {
            TimeMgr.Ins.RemoveFrame(ui, action);
        }

        public static void RemoveFrame<A>(this UIObject ui, Action<A> action)
        {
            TimeMgr.Ins.RemoveFrame(ui, action);
        }

        public static void RemoveFrame<A, B>(this UIObject ui, Action<A, B> action)
        {
            TimeMgr.Ins.RemoveFrame(ui, action);
        }

        public static void RemoveFrame<A, B, C>(this UIObject ui, Action<A, B, C> action)
        {
            TimeMgr.Ins.RemoveFrame(ui, action);
        }

        #endregion

        #region 定时器 TimeStamp

        public static long DoTimeStamp(this UIObject ui, DateTime dt, Action action)
        {
            return DoTimeStamp(ui, dt.ToTimeStamp(), action);
        }

        public static long DoTimeStamp(this UIObject ui, long timeStamp, Action action)
        {
            return TimeMgr.Ins.DoTimeStamp(timeStamp, ui, action);
        }

        public static long DoTimeStamp<A>(this UIObject ui, DateTime dt, Action<A> action, A a)
        {
            return DoTimeStamp(ui, dt.ToTimeStamp(), action, a);
        }

        public static long DoTimeStamp<A>(this UIObject ui, long timeStamp, Action<A> action, A a)
        {
            return TimeMgr.Ins.DoTimeStamp(timeStamp, ui, action, a);
        }

        public static long DoTimeStamp<A, B>(this UIObject ui, DateTime dt, Action<A, B> action, A a, B b)
        {
            return DoTimeStamp(ui, dt.ToTimeStamp(), action, a, b);
        }

        public static long DoTimeStamp<A, B>(this UIObject ui, long timeStamp, Action<A, B> action, A a, B b)
        {
            return TimeMgr.Ins.DoTimeStamp(timeStamp, ui, action, a, b);
        }

        public static long DoTimeStamp<A, B, C>(this UIObject ui, DateTime dt, Action<A, B, C> action, A a, B b, C c)
        {
            return DoTimeStamp(ui, dt.ToTimeStamp(),action, a, b, c);
        }

        public static long DoTimeStamp<A, B, C>(this UIObject ui, long timeStamp, Action<A, B, C> action, A a, B b, C c)
        {
            return TimeMgr.Ins.DoTimeStamp(timeStamp, ui, action, a, b, c);
        }

        public static void RemoveTimeStamp(this UIObject ui, Action action)
        {
            TimeMgr.Ins.RemoveTimeStamp(ui, action);
        }

        public static void RemoveTimeStamp<A>(this UIObject ui, Action<A> action)
        {
            TimeMgr.Ins.RemoveTimeStamp(ui, action);
        }

        public static void RemoveTimeStamp<A, B>(this UIObject ui, Action<A, B> action)
        {
            TimeMgr.Ins.RemoveTimeStamp(ui, action);
        }

        public static void RemoveTimeStamp<A, B, C>(this UIObject ui, Action<A, B, C> action)
        {
            TimeMgr.Ins.RemoveTimeStamp(ui, action);
        }

        #endregion TimeStamp

        #region 定时器 Cron

        public static long DoCron(this UIObject ui, string cron, Action action)
        {
            return TimeMgr.Ins.DoCron(cron, ui, action);
        }

        public static long DoCron<A>(this UIObject ui, string cron, Action<A> action, A a)
        {
            return TimeMgr.Ins.DoCron(cron, ui, action, a);
        }

        public static long DoCron<A, B>(this UIObject ui, string cron, Action<A, B> action, A a, B b)
        {
            return TimeMgr.Ins.DoCron(cron, ui, action, a, b);
        }

        public static long DoCron<A, B, C>(this UIObject ui, string cron, Action<A, B, C> action, A a, B b, C c)
        {
            return TimeMgr.Ins.DoCron(cron, ui, action, a, b, c);
        }

        public static void RemoveCron(this UIObject ui, Action action)
        {
            TimeMgr.Ins.RemoveCron(ui, action);
        }

        public static void RemoveCron<A>(this UIObject ui, Action<A> action)
        {
            TimeMgr.Ins.RemoveCron(ui, action);
        }

        public static void RemoveCron<A, B>(this UIObject ui, Action<A, B> action)
        {
            TimeMgr.Ins.RemoveCron(ui, action);
        }

        public static void RemoveCron<A, B, C>(this UIObject ui, Action<A, B, C> action)
        {
            TimeMgr.Ins.RemoveCron(ui, action);
        }

        #endregion
    }
}
