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
            return (long)(a > b
                ? (b & 0xFFFFFFFFL) | (((long)a & 0x7fffffff) << 32)
                : (a & 0xFFFFFFFFL) | (((long)b & 0x7fffffff) << 32));
        }

        public static long GenerateId(int a, int b, int c)
        {            
            var ab = a > b
                ? (b & 0xFFFFFFFFL) | (((long)a & 0x7fffffff) << 32)
                : (a & 0xFFFFFFFFL) | (((long)b & 0x7fffffff) << 32);
            return (long)(ab | (((long)c & 0x7fffffff) << 32));
        }

        public static Guid NewGuid()
        {
            return Guid.NewGuid();
        }
    }
}