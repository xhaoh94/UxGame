using System;
using System.Buffers.Binary;
using UnityEngine;

namespace Ux
{
    public static class BinaryTools
    {
        //Ä¬ÈÏÐ¡¶Ë
        public static bool IsLittleEndian { get; set; } = true;
        public static void WriteInt16(Span<byte> destination, short num)
        {
            if (IsLittleEndian)
            {
                BinaryPrimitives.WriteInt16LittleEndian(destination, num);
            }
            else
            {
                BinaryPrimitives.WriteInt16BigEndian(destination, num);
            }
        }
        public static void WriteUInt16(Span<byte> destination, ushort num)
        {
            if (IsLittleEndian)
            {
                BinaryPrimitives.WriteUInt16LittleEndian(destination, num);
            }
            else
            {
                BinaryPrimitives.WriteUInt16BigEndian(destination, num);
            }
        }
        public static void WriteInt32(Span<byte> destination, int num)
        {
            if (IsLittleEndian)
            {
                BinaryPrimitives.WriteInt32LittleEndian(destination, num);
            }
            else
            {
                BinaryPrimitives.WriteInt32BigEndian(destination, num);
            }
        }
        public static void WriteUInt32(Span<byte> destination, uint num)
        {
            if (IsLittleEndian)
            {
                BinaryPrimitives.WriteUInt32LittleEndian(destination, num);
            }
            else
            {
                BinaryPrimitives.WriteUInt32BigEndian(destination, num);
            }
        }
        public static void WriteInt64(Span<byte> destination, long num)
        {
            if (IsLittleEndian)
            {
                BinaryPrimitives.WriteInt64LittleEndian(destination, num);
            }
            else
            {
                BinaryPrimitives.WriteInt64BigEndian(destination, num);
            }
        }
        public static void WriteUInt64(Span<byte> destination, ulong num)
        {
            if (IsLittleEndian)
            {
                BinaryPrimitives.WriteUInt64LittleEndian(destination, num);
            }
            else
            {
                BinaryPrimitives.WriteUInt64BigEndian(destination, num);
            }
        }

        public static short ReadInt16(Span<byte> destination)
        {
            if (IsLittleEndian)
            {
                return BinaryPrimitives.ReadInt16LittleEndian(destination);
            }
            else
            {
                return BinaryPrimitives.ReadInt16BigEndian(destination);
            }            
        }
        public static ushort ReadUInt16(Span<byte> destination)
        {
            if (IsLittleEndian)
            {
                return BinaryPrimitives.ReadUInt16LittleEndian(destination);
            }
            else
            {
                return BinaryPrimitives.ReadUInt16BigEndian(destination);
            }
        }
        public static int ReadInt32(Span<byte> destination)
        {
            if (IsLittleEndian)
            {
                return BinaryPrimitives.ReadInt32LittleEndian(destination);
            }
            else
            {
                return BinaryPrimitives.ReadInt32BigEndian(destination);
            }
        }
        public static uint ReadUInt32(Span<byte> destination)
        {
            if (IsLittleEndian)
            {
                return BinaryPrimitives.ReadUInt32LittleEndian(destination);
            }
            else
            {
                return BinaryPrimitives.ReadUInt32BigEndian(destination);
            }
        }
        public static long ReadInt64(Span<byte> destination)
        {
            if (IsLittleEndian)
            {
                return BinaryPrimitives.ReadInt64LittleEndian(destination);
            }
            else
            {
                return BinaryPrimitives.ReadInt64BigEndian(destination);
            }
        }
        public static ulong ReadUInt64(Span<byte> destination)
        {
            if (IsLittleEndian)
            {
                return BinaryPrimitives.ReadUInt64LittleEndian(destination);
            }
            else
            {
                return BinaryPrimitives.ReadUInt64BigEndian(destination);
            }
        }
    }
}
