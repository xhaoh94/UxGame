using System;

namespace Ux
{
    public static class DateTimeEx
    {
        static long utc_1970_local_sub = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToUniversalTime().Ticks + TimeZoneInfo.Local.BaseUtcOffset.Ticks;
        /// <summary>
        /// 从1970年起 ,DateTime转毫秒时间戳
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static long ToTimeStamp(this DateTime dt)
        {
            return (dt.Ticks - utc_1970_local_sub) / 10000;
        }
        /// <summary>
        /// 从1970年起，经过毫秒时间戳转DateTime
        /// </summary>
        /// <param name="timeStamp"></param>
        /// <returns></returns>
        public static DateTime ToDateTime(this long timeStamp)
        {
            return new DateTime(utc_1970_local_sub + timeStamp * TimeSpan.TicksPerMillisecond);
        }
        public static int GetWeek(this DateTime dateTime)
        {
            return (int)dateTime.DayOfWeek + 1;
        }
    }
}
