using System;

namespace Ux
{
    public static class TimerHelper
    {
        public const string format1 = "yyyy-MM-dd HH:mm:ss";
        public const string format2 = "yyyy年MM月dd日 HH小时mm分ss秒";
        public const string format3 = "HH:mm:ss";
        /// <summary>
        /// 时间戳转字符串
        /// </summary>
        /// <param name="time"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string TimeStampToString(long time, string format = format1)
        {
            DateTime temp = time.ToDateTime();

            if (!string.IsNullOrEmpty(format))
            {
                return temp.ToString(format);
            }
            else
            {
                return temp.ToLongTimeString();
            }
        }

        public static string TimeSpanToString(long time)
        {
            DateTime temp = time.ToDateTime();
            return TimeSpanToString(TimeMgr.Ins.ServerTime.Now - temp);
        }
        public static string TimeSpanToString(TimeSpan timeSpan)
        {
            if (timeSpan.TotalDays >= 1)
                return string.Format("{0}天", timeSpan.Days);
            else
                return timeSpan.ToString(format3);
        }
    }
}
