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


        public static Guid NewGuid()
        {
            return Guid.NewGuid();
        }
    }
}