using System;

namespace Ux
{
    public static class IDGenerater
    {
        private static ushort value;
        public static long GenerateId()
        {
            long time = TimeMgr.Ins.LocalTime.TimeStamp / 10;

            return (time << 18) + ++value;
        }
        public static long GenerateId(int a, int b)
        {
            return GenerateId((ulong)a, (ulong)b);
        }
        static long GenerateId(ulong a, ulong b)
        {
            var id = a > b ? b | (a << 32) : a | (b << 32);
            return (long)id;
        }
        public static long GenerateId(int a, int b, int c)
        {
            return GenerateId((ulong)a, (ulong)b, (ulong)c);
        }
        static long GenerateId(ulong a, ulong b, ulong c)
        {
            return (long)((a << 32) | (b > c ? c | (b << 32) : b | (c << 32)));
        }

        public static Guid NewGuid()
        {
            return Guid.NewGuid();
        }
    }
}