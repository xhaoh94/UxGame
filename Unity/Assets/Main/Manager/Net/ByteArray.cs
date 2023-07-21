using Cysharp.Threading.Tasks;
using SJ;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;

namespace Ux
{
    public class ByteArray
    {
        public const int ChunkSize = 8192;

        private readonly Queue<byte[]> buffers = new Queue<byte[]>();

        private readonly Queue<byte[]> caches = new Queue<byte[]>();


        int PushPosition;
        int PopPosition;
        byte[] LastBuffer { get; set; }
        byte[] FirstBuffer => buffers.Peek();

        public ByteArray()
        {
            Add();
        }

        public long Length
        {
            get
            {
                int c;
                if (buffers.Count == 0)
                {
                    c = 0;
                }
                else
                {
                    c = (buffers.Count - 1) * ChunkSize + (PushPosition - PopPosition);
                }
                return c;
            }
        }

        void Add()
        {
            var buffer = caches.Count > 0 ? caches.Dequeue() : new byte[ChunkSize];
            buffers.Enqueue(buffer);
            LastBuffer = buffer;
            PushPosition = 0;
        }
        void Remove()
        {
            caches.Enqueue(buffers.Dequeue());
            PopPosition = 0;
        }

        public void Dispose()
        {
            buffers.Clear();
            caches.Clear();
            PushPosition = 0;
            PopPosition = 0;
        }

        #region Push
        public void PushTransferred(int count)
        {
            int len = 0;
            while (len < count)
            {
                if (PushPosition == ChunkSize)
                {
                    Add();
                }
                int temLen = count;
                if (PushPosition + count > ChunkSize)
                {
                    temLen = ChunkSize - PushPosition;
                }
                PushPosition += temLen;
                len += temLen;
            }
        }
        public async UniTask<WebSocketReceiveResult> PushByWebSocketAsync(WebSocket webSocket, CancellationToken token)
        {
            var count = ChunkSize - PushPosition;
            try
            {
                return await webSocket.ReceiveAsync(new ArraySegment<byte>(LastBuffer, PushPosition, count), token);
            }
            catch (Exception e)
            {
                throw new Exception($"socket set buffer error: {Length}, {PushPosition}, {count}", e);
            }

        }
        //public int PushByKcp(IntPtr kcp, int count)
        //{
        //    int len = 0;
        //    while (len < count)
        //    {
        //        if (PushPosition == ChunkSize)
        //        {
        //            Add();
        //        }
        //        int temLen = count;
        //        if (PushPosition + count > ChunkSize)
        //        {
        //            temLen = ChunkSize - PushPosition;
        //        }
        //        temLen = Kcp_cpp.KcpRecv(kcp, LastBuffer, PushPosition, temLen);
        //        PushPosition += temLen;
        //        len += temLen;
        //    }
        //    return len;
        //}
        public int PushByKcp(Kcp kcp, int count)
        {
            int len = 0;
            while (len < count)
            {
                if (PushPosition == ChunkSize)
                {
                    Add();
                }
                int temLen = count;
                if (PushPosition + count > ChunkSize)
                {
                    temLen = ChunkSize - PushPosition;
                }
                kcp.Receive(LastBuffer.AsSpan(PushPosition, temLen));
                PushPosition += temLen;
                len += temLen;
            }
            return len;
        }
        public bool PushBySocket(Socket socket, SocketAsyncEventArgs eventArgs)
        {
            if (eventArgs == null)
            {
                return false;
            }

            var count = ChunkSize - PushPosition;

            try
            {
                eventArgs.SetBuffer(LastBuffer, PushPosition, count);
                if (socket.ReceiveAsync(eventArgs))
                {
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                throw new Exception($"socket set buffer error: {Length}, {PushPosition}, {count}", e);
            }
        }
        public void PushStream(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            int count = (int)(stream.Length - stream.Position);
            int len = 0;
            while (len < count)
            {
                if (PushPosition == ChunkSize)
                {
                    Add();
                }
                int temLen = count;
                if (PushPosition + count > ChunkSize)
                {
                    temLen = ChunkSize - PushPosition;
                }
                stream.Read(LastBuffer, PushPosition, temLen);
                PushPosition += temLen;
                len += temLen;
            }
        }
        public void PushBytes(byte[] bytes, int index, int count)
        {
            int len = 0;
            while (len < count)
            {
                if (PushPosition == ChunkSize)
                {
                    Add();
                }
                int temLen = count;
                if (PushPosition + count > ChunkSize)
                {
                    temLen = ChunkSize - PushPosition;
                }
                Array.Copy(bytes, len + index, LastBuffer, PushPosition, temLen);
                PushPosition += temLen;
                len += temLen;
            }
        }
        public void PushBytes(byte[] bytes)
        {
            PushBytes(bytes, 0, bytes.Length);
        }
        public void PushByte(byte v)
        {
            if (PushPosition == ChunkSize)
            {
                Add();
            }
            LastBuffer[PushPosition] = v;
            PushPosition += 1;
        }
        public void PushInt16(short num)
        {
            PushByte((byte)(num & 0xFF));
            PushByte((byte)((num >> 8) & 0xFF));
        }
        public void PushUInt16(ushort num)
        {
            PushByte((byte)(num & 0xFF));
            PushByte((byte)((num >> 8) & 0xFF));
        }
        public void PushInt32(int num)
        {
            PushByte((byte)(num & 0xFF));
            PushByte((byte)((num >> 8) & 0xFF));
            PushByte((byte)((num >> 16) & 0xFF));
            PushByte((byte)((num >> 24) & 0xFF));
        }
        public void PushUInt32(uint num)
        {
            PushByte((byte)(num & 0xFF));
            PushByte((byte)((num >> 8) & 0xFF));
            PushByte((byte)((num >> 16) & 0xFF));
            PushByte((byte)((num >> 24) & 0xFF));
        }
        public void PushInt64(long num)
        {
            PushByte((byte)(num & 0xFF));
            PushByte((byte)((num >> 8) & 0xFF));
            PushByte((byte)((num >> 16) & 0xFF));
            PushByte((byte)((num >> 24) & 0xFF));
            PushByte((byte)((num >> 32) & 0xFF));
            PushByte((byte)((num >> 40) & 0xFF));
            PushByte((byte)((num >> 48) & 0xFF));
            PushByte((byte)((num >> 56) & 0xFF));
        }
        public void PushUInt64(ulong num)
        {
            PushByte((byte)(num & 0xFF));
            PushByte((byte)((num >> 8) & 0xFF));
            PushByte((byte)((num >> 16) & 0xFF));
            PushByte((byte)((num >> 24) & 0xFF));
            PushByte((byte)((num >> 32) & 0xFF));
            PushByte((byte)((num >> 40) & 0xFF));
            PushByte((byte)((num >> 48) & 0xFF));
            PushByte((byte)((num >> 56) & 0xFF));
        }

        #endregion


        #region Pop
        public void PopTransferred(int count)
        {
            int len = 0;
            while (len < count)
            {
                if (PopPosition == ChunkSize)
                {
                    Remove();
                }
                int temLen = count;
                if (PopPosition + count > ChunkSize)
                {
                    temLen = ChunkSize - PopPosition;
                }
                PopPosition += temLen;
                len += temLen;
            }
        }
        public bool PopToSocket(Socket socket, SocketAsyncEventArgs eventArgs)
        {
            if (eventArgs == null)
            {
                return false;
            }

            var count = ChunkSize - PopPosition;
            if (count > Length)
            {
                count = (int)Length;
            }

            try
            {
                eventArgs.SetBuffer(FirstBuffer, PopPosition, count);
                if (socket.SendAsync(eventArgs))
                {
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                throw new Exception($"socket set buffer error: {Length}, {PopPosition}, {count}", e);
            }
        }
        //public void PopToKcp(IntPtr kcp, int maxSendLen)
        //{
        //    var count = (int)Length;
        //    if (count > maxSendLen)
        //    {
        //        count = maxSendLen;
        //    }

        //    int len = 0;
        //    while (len < count)
        //    {
        //        if (PopPosition == ChunkSize)
        //        {
        //            Remove();
        //        }
        //        int temLen = count;
        //        if (PopPosition + count > ChunkSize)
        //        {
        //            temLen = ChunkSize - PopPosition;
        //        }
        //        Kcp_cpp.KcpSend(kcp, FirstBuffer, PopPosition, temLen);
        //        PopPosition += temLen;
        //        len += temLen;
        //    }
        //}
        public void PopToKcp(Kcp kcp, int maxSendLen)
        {
            var count = (int)Length;
            if (count > maxSendLen)
            {
                count = maxSendLen;
            }

            int len = 0;
            while (len < count)
            {
                if (PopPosition == ChunkSize)
                {
                    Remove();
                }
                int temLen = count;
                if (PopPosition + count > ChunkSize)
                {
                    temLen = ChunkSize - PopPosition;
                }
                kcp.Send(FirstBuffer.AsSpan(PopPosition, temLen));
                PopPosition += temLen;
                len += temLen;
            }
        }

        public async UniTask PopToWebSocketAsync(WebSocket webSocket, int maxSendLen, CancellationToken token)
        {
            var count = (int)Length;
            if (count > maxSendLen)
            {
                count = maxSendLen;
            }

            int len = 0;
            while (len < count)
            {
                if (PopPosition == ChunkSize)
                {
                    Remove();
                }
                int temLen = count;
                if (PopPosition + count > ChunkSize)
                {
                    temLen = ChunkSize - PopPosition;
                }
                await webSocket.SendAsync(new ArraySegment<byte>(FirstBuffer, PopPosition, temLen), WebSocketMessageType.Binary, true, token);
                PopPosition += temLen;
                len += temLen;
            }
        }

        public int PopToMemoryStream(MemoryStream memoryStream, int offset, int count)
        {
            memoryStream.Seek(offset, SeekOrigin.Begin);
            memoryStream.SetLength(count);

            if (memoryStream.Capacity < offset + count)
            {
                throw new Exception($"bufferList length < coutn, buffer length: {memoryStream.Capacity} {offset} {count}");
            }

            long length = Length;
            if (length < count)
            {
                count = (int)length;
            }

            int len = 0;
            while (len < count)
            {
                if (PopPosition == ChunkSize)
                {
                    Remove();
                }
                int temLen = count;
                if (PopPosition + count > ChunkSize)
                {
                    temLen = ChunkSize - PopPosition;
                }
                memoryStream.Seek(len + offset, SeekOrigin.Begin);
                //for (int i = 0; i < temReadLen; i++)
                //{                    
                //    memoryStream.WriteByte(FirstBuffer[ReadPosition + i]);
                //}
                memoryStream.Write(FirstBuffer, PopPosition, temLen);
                PopPosition += temLen;
                len += temLen;
            }
            return count;
        }

        public void PopToStream(Stream stream, int count)
        {
            if (count > Length)
            {
                throw new Exception($"bufferList length < count, {Length} {count}");
            }

            int len = 0;
            while (len < count)
            {
                if (PopPosition == ChunkSize)
                {
                    Remove();
                }
                int temLen = count;
                if (PopPosition + count > ChunkSize)
                {
                    temLen = ChunkSize - PopPosition;
                }
                stream.Write(FirstBuffer, PopPosition, temLen);
                PopPosition += temLen;
                len += temLen;
            }
        }

        public void PopToBytes(byte[] bytes)
        {
            PopToBytes(bytes, 0, bytes.Length);
        }
        public void PopToBytes(byte[] bytes, int count)
        {
            PopToBytes(bytes, 0, count);
        }
        public void PopToBytes(byte[] bytes, int index, int count)
        {
            if (count > Length)
            {
                throw new Exception($"bufferList length < count, {Length} {count}");
            }

            int len = 0;
            while (len < count)
            {
                if (PopPosition == ChunkSize)
                {
                    Remove();
                }
                int temLen = count;
                if (PopPosition + count > ChunkSize)
                {
                    temLen = ChunkSize - PopPosition;
                }
                Array.Copy(FirstBuffer, PopPosition, bytes, len + index, temLen);
                PopPosition += temLen;
                len += temLen;
            }
        }

        public byte[] PopBytes(int count)
        {
            var bytes = new byte[count];
            PopToBytes(bytes, 0, count);
            return bytes;
        }
        public byte PopByte()
        {
            if (Length <= 0)
            {
                throw new Exception($"bufferList length < 0, {Length}");
            }
            if (PopPosition == ChunkSize)
            {
                Remove();
            }
            return FirstBuffer[PopPosition++];
        }
        public short PopInt16()
        {
            if (Length < 2)
            {
                throw new Exception($"bufferList length < 2, {Length}");
            }
            if (ChunkSize - PopPosition >= 2)
            {
                var index = PopPosition;
                PopPosition += 2;
                return BitConverter.ToInt16(FirstBuffer, index);
            }
            else
            {
                var bytes = PopBytes(2);
                return BitConverter.ToInt16(bytes, 0);
            }
        }
        public ushort PopUInt16()
        {
            if (Length < 2)
            {
                throw new Exception($"bufferList length < 2, {Length}");
            }
            if (ChunkSize - PopPosition >= 2)
            {
                var index = PopPosition;
                PopPosition += 2;
                return BitConverter.ToUInt16(FirstBuffer, index);
            }
            else
            {
                var bytes = PopBytes(2);
                return BitConverter.ToUInt16(bytes, 0);
            }
        }
        public int PopInt32()
        {
            if (Length < 4)
            {
                throw new Exception($"bufferList length < 4, {Length}");
            }
            if (ChunkSize - PopPosition >= 4)
            {
                var index = PopPosition;
                PopPosition += 4;
                return BitConverter.ToInt32(FirstBuffer, index);
            }
            else
            {
                var bytes = PopBytes(4);
                return BitConverter.ToInt32(bytes, 0);
            }
        }
        public uint PopUInt32()
        {
            if (Length < 4)
            {
                throw new Exception($"bufferList length < 4, {Length}");
            }
            if (ChunkSize - PopPosition >= 4)
            {
                var index = PopPosition;
                PopPosition += 4;
                return BitConverter.ToUInt32(FirstBuffer, index);
            }
            else
            {
                var bytes = PopBytes(4);
                return BitConverter.ToUInt32(bytes, 0);
            }
        }
        public long PopInt64()
        {
            if (Length < 8)
            {
                throw new Exception($"bufferList length < 8, {Length}");
            }
            if (ChunkSize - PopPosition >= 8)
            {
                var index = PopPosition;
                PopPosition += 8;
                return BitConverter.ToInt64(FirstBuffer, index);
            }
            else
            {
                var bytes = PopBytes(8);
                return BitConverter.ToInt64(bytes, 0);
            }
        }
        public ulong PopUInt64()
        {
            if (Length < 8)
            {
                throw new Exception($"bufferList length < 8, {Length}");
            }
            if (ChunkSize - PopPosition >= 8)
            {
                var index = PopPosition;
                PopPosition += 8;
                return BitConverter.ToUInt64(FirstBuffer, index);
            }
            else
            {
                var bytes = PopBytes(8);
                return BitConverter.ToUInt64(bytes, 0);
            }
        }
        #endregion
    }

}