using System;
using System.IO;

namespace Ux
{
    public static class StreamEx
    {
        public static void WriteToByte(this MemoryStream stream, int offset, byte v)
        {
            if (stream == null)
            {
                return;
            }

            stream.Seek(offset, SeekOrigin.Begin);
            stream.WriteByte(v);
        }
        public static void WriteToByte(this MemoryStream stream, byte v)
        {
            stream?.WriteToByte((int)stream.Position, v);
        }

        public static void WriteToBytes(this MemoryStream memorySstreamream, int offset, byte[] bytes)
        {
            if (memorySstreamream != null && bytes != null)
            {
                memorySstreamream.Seek(offset, SeekOrigin.Begin);
                memorySstreamream.Write(bytes, 0, bytes.Length);
            }
        }
        public static void WriteToBytes(this MemoryStream stream, byte[] bytes)
        {
            if (stream != null && bytes != null)
            {
                stream.WriteToBytes((int)stream.Position, bytes);
            }
        }
        public static void WriteToInt16(this MemoryStream stream, int offset, short v)
        {
            if (stream == null)
            {
                return;
            }

            stream.Seek(offset, SeekOrigin.Begin);
            stream.WriteByte((byte)(v & 0xff));
            stream.WriteByte((byte)((v >> 8) & 0xff));
        }
        public static void WriteToInt16(this MemoryStream stream, short v)
        {
            stream?.WriteToInt16((int)stream.Position, v);
        }
        public static void WriteToUInt16(this MemoryStream stream, int offset, ushort num)
        {
            if (stream == null)
            {
                return;
            }

            stream.Seek(offset, SeekOrigin.Begin);
            stream.WriteByte((byte)(num & 0xff));
            stream.WriteByte((byte)((num & 0xff) >> 8));
        }
        public static void WriteToUInt16(this MemoryStream stream, ushort v)
        {
            stream?.WriteToUInt16((int)stream.Position, v);
        }
        public static void WriteToInt32(this MemoryStream stream, int offset, int v)
        {
            if (stream == null)
            {
                return;
            }

            stream.Seek(offset, SeekOrigin.Begin);
            stream.WriteByte((byte)(v & 0xff));
            stream.WriteByte((byte)((v >> 8) & 0xff));
            stream.WriteByte((byte)((v >> 16) & 0xff));
            stream.WriteByte((byte)((v >> 24) & 0xff));
        }
        public static void WriteToInt32(this MemoryStream stream, int v)
        {
            stream?.WriteToInt32((int)stream.Position, v);
        }
        public static void WriteToUInt32(this MemoryStream stream, int offset, uint v)
        {
            if (stream == null)
            {
                return;
            }

            stream.Seek(offset, SeekOrigin.Begin);
            stream.WriteByte((byte)(v & 0xff));
            stream.WriteByte((byte)((v >> 8) & 0xff));
            stream.WriteByte((byte)((v >> 16) & 0xff));
            stream.WriteByte((byte)((v >> 24) & 0xff));
        }
        public static void WriteToUInt32(this MemoryStream stream, uint v)
        {
            stream?.WriteToUInt32((int)stream.Position, v);
        }
        public static void WriteToInt64(this MemoryStream stream, int offset, long v)
        {
            if (stream == null)
            {
                return;
            }

            stream.Seek(offset, SeekOrigin.Begin);
            stream.WriteByte((byte)(v & 0xff));
            stream.WriteByte((byte)((v >> 8) & 0xff));
            stream.WriteByte((byte)((v >> 16) & 0xff));
            stream.WriteByte((byte)((v >> 24) & 0xff));
            stream.WriteByte((byte)((v >> 32) & 0xff));
            stream.WriteByte((byte)((v >> 40) & 0xff));
            stream.WriteByte((byte)((v >> 48) & 0xff));
            stream.WriteByte((byte)((v >> 56) & 0xff));
        }
        public static void WriteToInt64(this MemoryStream stream, long v)
        {
            stream?.WriteToInt64((int)stream.Position, v);
        }
        public static void WriteToUInt64(this MemoryStream stream, int offset, ulong v)
        {
            if (stream == null)
            {
                return;
            }

            stream.Seek(offset, SeekOrigin.Begin);
            stream.WriteByte((byte)(v & 0xff));
            stream.WriteByte((byte)((v >> 8) & 0xff));
            stream.WriteByte((byte)((v >> 16) & 0xff));
            stream.WriteByte((byte)((v >> 24) & 0xff));
            stream.WriteByte((byte)((v >> 32) & 0xff));
            stream.WriteByte((byte)((v >> 40) & 0xff));
            stream.WriteByte((byte)((v >> 48) & 0xff));
            stream.WriteByte((byte)((v >> 56) & 0xff));

        }
        public static void WriteToUInt64(this MemoryStream stream, ulong v)
        {
            stream?.WriteToUInt64((int)stream.Position, v);
        }
        public static void WriteToMessage(this MemoryStream stream, object message)
        {
            WriteToMessage(stream, message, (int)stream.Position);
        }
        public static void WriteToMessage(this MemoryStream stream, object message, int offset)
        {
            stream.Seek(offset, SeekOrigin.Begin);
            ProtoBuf.Serializer.Serialize(stream, message);
        }
        public static byte ReadToByte(this MemoryStream stream, int offset)
        {
            if (stream == null)
            {
                return 0;
            }
            stream.Seek(offset, SeekOrigin.Begin);
            return (byte)stream.ReadByte();
        }
        public static byte ReadToByte(this MemoryStream stream)
        {
            if (stream == null)
            {
                return 0;
            }
            return (byte)stream.ReadToByte((int)stream.Position);
        }

        public static byte[] ReadToBytes(this MemoryStream stream, int offset, int size)
        {
            if (stream == null)
            {
                return null;
            }
            var bytes = new byte[size];
            stream.Read(bytes, offset, size);
            return bytes;
        }
        public static byte[] ReadBytes(this MemoryStream stream, int size)
        {
            if (stream == null)
            {
                return null;
            }
            return stream.ReadToBytes((int)stream.Position, size);
        }
        public static short ReadToInt16(this MemoryStream stream, int offset)
        {
            if (stream == null)
            {
                return 0;
            }

            var v = BitConverter.ToInt16(stream.GetBuffer(), offset);
            stream.Position = offset + 2;
            return v;
        }
        public static short ReadToInt16(this MemoryStream stream)
        {
            if (stream == null)
            {
                return 0;
            }
            return stream.ReadToInt16((int)stream.Position);
        }
        public static ushort ReadToUInt16(this MemoryStream stream, int offset)
        {
            if (stream == null)
            {
                return 0;
            }
            var v = BitConverter.ToUInt16(stream.GetBuffer(), offset);
            stream.Position = offset + 2;
            return v;
        }
        public static ushort ReadToUInt16(this MemoryStream stream)
        {
            if (stream == null)
            {
                return 0;
            }
            return stream.ReadToUInt16((int)stream.Position);
        }

        public static int ReadToInt32(this MemoryStream stream, int offset)
        {
            if (stream == null)
            {
                return 0;
            }
            var v = BitConverter.ToInt32(stream.GetBuffer(), offset);
            stream.Position = offset + 4;
            return v;
        }
        public static int ReadToInt32(this MemoryStream stream)
        {
            if (stream == null)
            {
                return 0;
            }
            return stream.ReadToInt32((int)stream.Position);
        }
        public static uint ReadToUInt32(this MemoryStream stream, int offset)
        {
            if (stream == null)
            {
                return 0;
            }
            var v = BitConverter.ToUInt32(stream.GetBuffer(), offset);
            stream.Position = offset + 4;
            return v;
        }
        public static uint ReadToUInt32(this MemoryStream stream)
        {
            if (stream == null)
            {
                return 0;
            }
            return stream.ReadToUInt32((int)stream.Position);
        }
        public static long ReadToInt64(this MemoryStream stream, int offset)
        {
            if (stream == null)
            {
                return 0;
            }
            var v = BitConverter.ToInt64(stream.GetBuffer(), offset);
            stream.Position = offset + 8;
            return v;
        }
        public static long ReadToInt64(this MemoryStream stream)
        {
            if (stream == null)
            {
                return 0;
            }
            return stream.ReadToInt64((int)stream.Position);
        }

        public static ulong ReadToUInt64(this MemoryStream stream, int offset)
        {
            if (stream == null)
            {
                return 0;
            }
            var v = BitConverter.ToUInt64(stream.GetBuffer(), offset);
            stream.Position = offset + 8;
            return v;
        }
        public static ulong ReadToUInt64(this MemoryStream stream)
        {
            if (stream == null)
            {
                return 0;
            }
            return stream.ReadToUInt64((int)stream.Position);
        }
        public static object ReadToMessage(this MemoryStream stream, Type type, int offset)
        {
            stream.Seek(offset, SeekOrigin.Begin);
            return ProtoBuf.Serializer.Deserialize(type, stream);
        }

    }
}
