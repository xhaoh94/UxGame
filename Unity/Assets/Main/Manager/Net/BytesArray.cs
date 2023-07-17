using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;

namespace Ux
{
    public class BytesArray
    {
        public const int ChunkSize = 8192;

        private readonly Queue<byte[]> buffers = new Queue<byte[]>();

        private readonly Queue<byte[]> caches = new Queue<byte[]>();


        int WritePosition;
        int ReadPosition;
        byte[] LastBuffer { get; set; }
        byte[] FirstBuffer => buffers.Peek();

        public BytesArray()
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
                    c = (buffers.Count - 1) * ChunkSize + (WritePosition - ReadPosition);
                }
                return c;
            }
        }

        void Add()
        {
            var buffer = caches.Count > 0 ? caches.Dequeue() : new byte[ChunkSize];
            buffers.Enqueue(buffer);
            LastBuffer = buffer;
            WritePosition = 0;
        }
        void Remove()
        {
            caches.Enqueue(buffers.Dequeue());
            ReadPosition = 0;
        }

        public void Dispose()
        {
            buffers.Clear();
            caches.Clear();
            WritePosition = 0;
            ReadPosition = 0;
        }

        #region Write
        
        public void WriteBytesTransferred(WebSocketReceiveResult receiveResult)
        {
            WritePosition += receiveResult.Count;
            if (WritePosition == ChunkSize)
            {
                Add();
            }
        }
        public async UniTask<WebSocketReceiveResult> WriteWebSocketAsync(WebSocket webSocket, CancellationToken token)
        {
            var count = ChunkSize - WritePosition;
            try
            {
                return await webSocket.ReceiveAsync(new ArraySegment<byte>(LastBuffer, WritePosition, count), token);
            }
            catch (Exception e)
            {
                throw new Exception($"socket set buffer error: {Length}, {WritePosition}, {count}", e);
            }

        }
        public int WriteKcp(IntPtr kcp, int count)
        {
            int writeLen = 0;
            while (writeLen < count)
            {
                if (WritePosition == ChunkSize)
                {
                    Add();
                }
                int temWriteLen = count;
                if (WritePosition + count > ChunkSize)
                {
                    temWriteLen = ChunkSize - WritePosition;
                }
                temWriteLen = Kcp.KcpRecv(kcp, LastBuffer, WritePosition, temWriteLen);
                WritePosition += temWriteLen;
                writeLen += temWriteLen;
            }
            return writeLen;
        }
        public bool WriteSocket(Socket socket, SocketAsyncEventArgs eventArgs)
        {
            if (eventArgs == null)
            {
                return false;
            }

            var count = ChunkSize - WritePosition;

            try
            {
                eventArgs.SetBuffer(LastBuffer, WritePosition, count);
                if (socket.ReceiveAsync(eventArgs))
                {
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                throw new Exception($"socket set buffer error: {Length}, {WritePosition}, {count}", e);
            }
        }
        public void WriteBytesTransferred(SocketAsyncEventArgs eventArgs)
        {
            WritePosition += eventArgs.BytesTransferred;
            if (WritePosition == ChunkSize)
            {
                Add();
            }
        }
        public void WriteStream(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            int count = (int)(stream.Length - stream.Position);
            int writeLen = 0;
            while (writeLen < count)
            {
                if (WritePosition == ChunkSize)
                {
                    Add();
                }
                int temWriteLen = count;
                if (WritePosition + count > ChunkSize)
                {
                    temWriteLen = ChunkSize - WritePosition;
                }
                stream.Read(LastBuffer, WritePosition, temWriteLen);
                WritePosition += temWriteLen;
                writeLen += temWriteLen;
            }
        }

        public void WriteBytes(byte[] bytes, int index, int count)
        {
            int writeLen = 0;
            while (writeLen < count)
            {
                if (WritePosition == ChunkSize)
                {
                    Add();
                }
                int temWriteLen = count;
                if (WritePosition + count > ChunkSize)
                {
                    temWriteLen = ChunkSize - WritePosition;
                }
                Array.Copy(bytes, writeLen + index, LastBuffer, WritePosition, temWriteLen);
                WritePosition += temWriteLen;
                writeLen += temWriteLen;
            }
        }

        public void WriteBytes(byte[] bytes)
        {
            WriteBytes(bytes, 0, bytes.Length);
        }

        public void WriteByte(byte v)
        {
            if (WritePosition == ChunkSize)
            {
                Add();
            }
            LastBuffer[WritePosition] = v;
            WritePosition += 1;
        }

        public void WriteInt16(short num)
        {
            WriteByte((byte)(num & 0xFF));
            WriteByte((byte)((num >> 8) & 0xFF));
        }
        public void WriteUInt16(ushort num)
        {
            WriteByte((byte)(num & 0xFF));
            WriteByte((byte)((num >> 8) & 0xFF));
        }
        public void WriteInt32(int num)
        {
            WriteByte((byte)(num & 0xFF));
            WriteByte((byte)((num >> 8) & 0xFF));
            WriteByte((byte)((num >> 16) & 0xFF));
            WriteByte((byte)((num >> 24) & 0xFF));
        }
        public void WriteUInt32(uint num)
        {
            WriteByte((byte)(num & 0xFF));
            WriteByte((byte)((num >> 8) & 0xFF));
            WriteByte((byte)((num >> 16) & 0xFF));
            WriteByte((byte)((num >> 24) & 0xFF));
        }
        public void WriteInt64(long num)
        {
            WriteByte((byte)(num & 0xFF));
            WriteByte((byte)((num >> 8) & 0xFF));
            WriteByte((byte)((num >> 16) & 0xFF));
            WriteByte((byte)((num >> 24) & 0xFF));
            WriteByte((byte)((num >> 32) & 0xFF));
            WriteByte((byte)((num >> 40) & 0xFF));
            WriteByte((byte)((num >> 48) & 0xFF));
            WriteByte((byte)((num >> 56) & 0xFF));
        }
        public void WriteUInt64(ulong num)
        {
            WriteByte((byte)(num & 0xFF));
            WriteByte((byte)((num >> 8) & 0xFF));
            WriteByte((byte)((num >> 16) & 0xFF));
            WriteByte((byte)((num >> 24) & 0xFF));
            WriteByte((byte)((num >> 32) & 0xFF));
            WriteByte((byte)((num >> 40) & 0xFF));
            WriteByte((byte)((num >> 48) & 0xFF));
            WriteByte((byte)((num >> 56) & 0xFF));
        }

        #endregion


        #region Read
        public void ReadBytesTransferred(SocketAsyncEventArgs eventArgs)
        {
            ReadPosition += eventArgs.BytesTransferred;
            if (ReadPosition == ChunkSize)
            {
                Remove();
            }
        }
        public bool ReadToSocket(Socket socket, SocketAsyncEventArgs eventArgs)
        {
            if (eventArgs == null)
            {
                return false;
            }

            var count = ChunkSize - ReadPosition;
            if (count > Length)
            {
                count = (int)Length;
            }

            try
            {
                eventArgs.SetBuffer(FirstBuffer, ReadPosition, count);
                if (socket.SendAsync(eventArgs))
                {
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                throw new Exception($"socket set buffer error: {Length}, {ReadPosition}, {count}", e);
            }
        }

        public void ReadToKcp(IntPtr kcp, int maxSendLen)
        {
            var count = (int)Length;
            if (count > maxSendLen)
            {
                count = maxSendLen;
            }

            int readLen = 0;
            while (readLen < count)
            {
                if (ReadPosition == ChunkSize)
                {
                    Remove();
                }
                int temReadLen = count;
                if (ReadPosition + count > ChunkSize)
                {
                    temReadLen = ChunkSize - ReadPosition;
                }
                Kcp.KcpSend(kcp, FirstBuffer, ReadPosition, temReadLen);
                ReadPosition += temReadLen;
                readLen += temReadLen;
            }
        }

        public async UniTask ReadToWebSocketAsync(WebSocket webSocket, int maxSendLen, CancellationToken token)
        {
            var count = (int)Length;
            if (count > maxSendLen)
            {
                count = maxSendLen;
            }

            int readLen = 0;
            while (readLen < count)
            {
                if (ReadPosition == ChunkSize)
                {
                    Remove();
                }
                int temReadLen = count;
                if (ReadPosition + count > ChunkSize)
                {
                    temReadLen = ChunkSize - ReadPosition;
                }
                await webSocket.SendAsync(new ArraySegment<byte>(FirstBuffer, ReadPosition, temReadLen), WebSocketMessageType.Binary, true, token);
                ReadPosition += temReadLen;
                readLen += temReadLen;
            }
        }

        public int ReadToMemoryStream(MemoryStream memoryStream, int offset, int count)
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

            int readLen = 0;
            while (readLen < count)
            {
                if (ReadPosition == ChunkSize)
                {
                    Remove();
                }
                int temReadLen = count;
                if (ReadPosition + count > ChunkSize)
                {
                    temReadLen = ChunkSize - ReadPosition;
                }
                memoryStream.Seek(readLen + offset, SeekOrigin.Begin);
                //for (int i = 0; i < temReadLen; i++)
                //{                    
                //    memoryStream.WriteByte(FirstBuffer[ReadPosition + i]);
                //}
                memoryStream.Write(FirstBuffer, ReadPosition, temReadLen);
                ReadPosition += temReadLen;
                readLen += temReadLen;
            }           
            return count;
        }

        public void ReadToStream(Stream stream, int count)
        {
            if (count > Length)
            {
                throw new Exception($"bufferList length < count, {Length} {count}");
            }

            int readLen = 0;
            while (readLen < count)
            {
                if (ReadPosition == ChunkSize)
                {
                    Remove();
                }
                int temReadLen = count;
                if (ReadPosition + count > ChunkSize)
                {
                    temReadLen = ChunkSize - ReadPosition;
                }
                stream.Write(FirstBuffer, ReadPosition, temReadLen);
                ReadPosition += temReadLen;
                readLen += temReadLen;
            }
        }
        
        public void ReadToBytes(byte[] bytes)
        {
            ReadToBytes(bytes, 0, bytes.Length);
        }
        public void ReadToBytes(byte[] bytes, int count)
        {
            ReadToBytes(bytes, 0, count);
        }
        public void ReadToBytes(byte[] bytes, int index, int count)
        {
            if (count > Length)
            {
                throw new Exception($"bufferList length < count, {Length} {count}");
            }

            int readLen = 0;
            while (readLen < count)
            {
                if (ReadPosition == ChunkSize)
                {
                    Remove();
                }
                int temReadLen = count;
                if (ReadPosition + count > ChunkSize)
                {
                    temReadLen = ChunkSize - ReadPosition;
                }
                Array.Copy(FirstBuffer, ReadPosition, bytes, readLen + index, temReadLen);
                ReadPosition += temReadLen;
                readLen += temReadLen;
            }
        }

        public byte[] ReadBytes(int count)
        {
            var bytes = new byte[count];
            ReadToBytes(bytes, 0, count);
            return bytes;
        }
        public byte ReadByte()
        {
            if (Length <= 0)
            {
                throw new Exception($"bufferList length < 0, {Length}");
            }
            if (ReadPosition == ChunkSize)
            {
                Remove();
            }
            return FirstBuffer[ReadPosition++];
        }

        public short ReadInt16()
        {
            if (Length < 2)
            {
                throw new Exception($"bufferList length < 2, {Length}");
            }
            if (ChunkSize - ReadPosition >= 2)
            {
                var index = ReadPosition;
                ReadPosition += 2;
                return BitConverter.ToInt16(FirstBuffer, index);
            }
            else
            {
                var bytes = ReadBytes(2);
                return BitConverter.ToInt16(bytes, 0);
            }
        }
        public ushort ReadUInt16()
        {
            if (Length < 2)
            {
                throw new Exception($"bufferList length < 2, {Length}");
            }
            if (ChunkSize - ReadPosition >= 2)
            {
                var index = ReadPosition;
                ReadPosition += 2;
                return BitConverter.ToUInt16(FirstBuffer, index);
            }
            else
            {
                var bytes = ReadBytes(2);
                return BitConverter.ToUInt16(bytes, 0);
            }
        }
        public int ReadInt32()
        {
            if (Length < 4)
            {
                throw new Exception($"bufferList length < 4, {Length}");
            }
            if (ChunkSize - ReadPosition >= 4)
            {
                var index = ReadPosition;
                ReadPosition += 4;
                return BitConverter.ToInt32(FirstBuffer, index);
            }
            else
            {
                var bytes = ReadBytes(4);
                return BitConverter.ToInt32(bytes, 0);
            }
        }
        public uint ReadUInt32()
        {
            if (Length < 4)
            {
                throw new Exception($"bufferList length < 4, {Length}");
            }
            if (ChunkSize - ReadPosition >= 4)
            {
                var index = ReadPosition;
                ReadPosition += 4;
                return BitConverter.ToUInt32(FirstBuffer, index);
            }
            else
            {
                var bytes = ReadBytes(4);
                return BitConverter.ToUInt32(bytes, 0);
            }
        }
        public long ReadInt64()
        {
            if (Length < 8)
            {
                throw new Exception($"bufferList length < 8, {Length}");
            }
            if (ChunkSize - ReadPosition >= 8)
            {
                var index = ReadPosition;
                ReadPosition += 8;
                return BitConverter.ToInt64(FirstBuffer, index);
            }
            else
            {
                var bytes = ReadBytes(8);
                return BitConverter.ToInt64(bytes, 0);
            }
        }
        public ulong ReadUInt64()
        {
            if (Length < 8)
            {
                throw new Exception($"bufferList length < 8, {Length}");
            }
            if (ChunkSize - ReadPosition >= 8)
            {
                var index = ReadPosition;
                ReadPosition += 8;
                return BitConverter.ToUInt64(FirstBuffer, index);
            }
            else
            {
                var bytes = ReadBytes(8);
                return BitConverter.ToUInt64(bytes, 0);
            }
        }
        #endregion
    }

}