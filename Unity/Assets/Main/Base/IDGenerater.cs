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
            var id = a > b ? (uint)b | ((ulong)a << 32) :
                        (uint)a | ((ulong)b << 32);
            return (long)id;
        }
        public static long GenerateId(int a, int b, int c)
        {
            return (long)((((ulong)a) << 58) | (ulong)GenerateId(b, c));
        }

        public static Guid NewGuid()
        {
            return Guid.NewGuid();
        }
    }
}