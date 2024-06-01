using System;

namespace Ux
{
    public static class IDGenerater
    {
        private static ushort value;
        public static long GenerateId()
        {
#if UNITY_EDITOR
            long time = 0;
            if (UnityEngine.Application.isPlaying)
            {
                time = TimeMgr.Ins.LocalTime.TimeStamp / 10;
            }
            else
            {
                time = DateTime.Now.ToTimeStamp() / 10;
            }            
#else
            long time = TimeMgr.Ins.LocalTime.TimeStamp / 10;
#endif            

            return (time << 18) + ++value;
        }
        public static long GenerateId(int a, int b)
        {
            return a > b
                ? (b & 0xFFFFFFFFL) | (((long)a & 0x7fffffff) << 32)
                : (a & 0xFFFFFFFFL) | (((long)b & 0x7fffffff) << 32);
        }

        public static long GenerateId(int a, int b, int c)
        {
            var ab = GenerateId(a, b);
            return (long)(ab | (((long)c & 0x7fffffff) << 32));
        }

        public static Guid NewGuid()
        {
            return Guid.NewGuid();
        }
    }
}