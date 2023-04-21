using System;

namespace Ux
{
    public static class IDGenerater
    {
        private static long head;

        private static ushort value;        

        public static long GenerateId()
        {
            long time = TimeMgr.Instance.LocalTime.TimeStamp / 10;

            return head + (time << 18) + ++value;
        }

        public static Guid NewGuid()
        {
            return Guid.NewGuid();
        }
    }
}