using Cysharp.Threading.Tasks;
using SJ;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;

namespace Ux
{
    public class ByteBuffer
    {
        const int ChunkSize = 8192;
        const int MaxCachedChunks = 32;

        readonly Queue<byte[]> _buffers = new Queue<byte[]>();

        readonly Queue<byte[]> _caches = new Queue<byte[]>();
        readonly ArrayPool<byte> _arrayPool = ArrayPool<byte>.Shared;

        private bool _disposed;
        int _pushPosition;
        int PushPosition
        {
            get => _pushPosition;
            set
            {
                _pushPosition = value;
                if (_pushPosition == ChunkSize)
                {
                    Add();
                }
            }
        }
        int _popPosition;
        int PopPosition
        {
            get => _popPosition;
            set
            {
                _popPosition = value;
                if (_popPosition == ChunkSize)
                {
                    Remove();
                }
            }
        }
        byte[] LastBuffer { get; set; }
        byte[] FirstBuffer => _buffers.Peek();

        public ByteBuffer()
        {
            Add();
        }

        public long Length
        {
            get
            {
                int c;
                if (_buffers.Count == 0)
                {
                    c = 0;
                }
                else
                {
                    c = (_buffers.Count - 1) * ChunkSize + (PushPosition - PopPosition);
                }
                return c;
            }
        }

        void Add()
        {
            var buffer = _caches.Count > 0 ? _caches.Dequeue() : _arrayPool.Rent(ChunkSize);
            _buffers.Enqueue(buffer);
            LastBuffer = buffer;
            _pushPosition = 0;
        }
        void Remove()
        {
            var buffer = _buffers.Dequeue();
            if (_caches.Count < MaxCachedChunks)
            {
                _caches.Enqueue(buffer);
            }
            else
            {
                _arrayPool.Return(buffer);
            }
            _popPosition = 0;
        }

        public void Dispose()
        {
            if (_disposed) return;
            while (_buffers.Count > 0)
            {
                _arrayPool.Return(_buffers.Dequeue());
            }
            while (_caches.Count > 0)
            {
                _arrayPool.Return(_caches.Dequeue());
            }

            _pushPosition = 0;
            _popPosition = 0;
            _disposed = true;
            GC.SuppressFinalize(this);
        }
        private void ProcessTransfer(int count, Action<int, int> action, bool isPush)
        {
            int offsetIndex = 0;
            while (offsetIndex < count)
            {
                int processLen = count - offsetIndex;
                int chunkRemain = isPush ? (ChunkSize - PushPosition) : (ChunkSize - PopPosition);
                processLen = Math.Min(processLen, chunkRemain);
                action?.Invoke(offsetIndex, processLen);
                if (isPush) PushPosition += processLen;
                else PopPosition += processLen;
                offsetIndex += processLen;
            }
        }
        #region Push
        public void PushTransferred(int count, Action<int, int> action = null)
        {
            ProcessTransfer(count, action, true);
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

        public int PushByKcp(Kcp kcp, int count)
        {
            PushTransferred(count, (offset, processLen) =>
            {
                kcp.Receive(LastBuffer.AsSpan(PushPosition, processLen));
            });
            return count;
        }
        public bool PushBySocket(Socket socket, SocketAsyncEventArgs eventArgs)
        {
            var count = ChunkSize - PushPosition;
            try
            {
                eventArgs.SetBuffer(LastBuffer, PushPosition, count);
            }
            catch (Exception e)
            {
                throw new Exception($"socket set buffer error: {Length}, {PushPosition}, {count}", e);
            }
            //返回True则挂起走异步OnRecvComplete。
            //返回False则是同步，不会触发OnRecvComplete。
            return socket.ReceiveAsync(eventArgs);
        }
        public void PushByStream(Stream stream)
        {
            //因为前面序列化的时候Seek(0, SeekOrigin.Begin)，            
            //所以真实长度是Position而不是Length，Length是包含之前部分的            
            int count = (int)stream.Position;
            stream.Seek(0, SeekOrigin.Begin);
            PushTransferred(count, (offset, processLen) =>
            {
                stream.Read(LastBuffer, PushPosition, processLen);
            });
        }
        public void PushSpan(ReadOnlySpan<byte> data)
        {
            int dataLength = data.Length;
            int len = 0;
            while (len < dataLength)
            {
                int processLen = dataLength - len;
                int chunkRemain = ChunkSize - PushPosition;
                processLen = Math.Min(processLen, chunkRemain);
                data.Slice(len, processLen).CopyTo(LastBuffer.AsSpan(PushPosition, processLen));
                len += processLen;
                PushPosition += processLen;
            }
        }
        public void PushBytes(byte[] bytes, int index, int count)
        {
            PushTransferred(count, (offset, processLen) =>
            {
                //Array.Copy(bytes, offset + index, LastBuffer, PushPosition, temLen);
                bytes.AsSpan(offset + index, processLen).CopyTo(LastBuffer.AsSpan(PushPosition, processLen));
            });
        }
        public void PushBytes(byte[] bytes)
        {
            PushBytes(bytes, 0, bytes.Length);
        }
        public void PushByte(byte v)
        {
            LastBuffer[PushPosition] = v;
            PushPosition += 1;
        }

        public void PushInt16(short num)
        {
            if (ChunkSize - PushPosition >= 2)
            {
                BinaryTools.WriteInt16(LastBuffer.AsSpan(PushPosition), num);
                PushPosition += 2;
            }
            else
            {
                Span<byte> buffer = stackalloc byte[2];
                BinaryTools.WriteInt16(buffer, num);
                PushSpan(buffer);
            }
        }
        public void PushUInt16(ushort num)
        {
            if (ChunkSize - PushPosition >= 2)
            {
                BinaryTools.WriteUInt16(LastBuffer.AsSpan(PushPosition), num);
                PushPosition += 2;
            }
            else
            {
                Span<byte> buffer = stackalloc byte[2];
                BinaryTools.WriteUInt16(buffer, num);
                PushSpan(buffer);
            }
        }
        public void PushInt32(int num)
        {
            if (ChunkSize - PushPosition >= 4)
            {
                BinaryTools.WriteInt32(LastBuffer.AsSpan(PushPosition), num);
                PushPosition += 4;
            }
            else
            {
                Span<byte> buffer = stackalloc byte[4];
                BinaryTools.WriteInt32(buffer, num);
                PushSpan(buffer);
            }
        }
        public void PushUInt32(uint num)
        {
            if (ChunkSize - PushPosition >= 4)
            {
                BinaryTools.WriteUInt32(LastBuffer.AsSpan(PushPosition), num);
                PushPosition += 4;
            }
            else
            {
                Span<byte> buffer = stackalloc byte[4];
                BinaryTools.WriteUInt32(buffer, num);
                PushSpan(buffer);
            }
        }
        public void PushInt64(long num)
        {
            if (ChunkSize - PushPosition >= 8)
            {
                BinaryTools.WriteInt64(LastBuffer.AsSpan(PushPosition), num);
                PushPosition += 8;
            }
            else
            {
                Span<byte> buffer = stackalloc byte[8];
                BinaryTools.WriteInt64(buffer, num);
                PushSpan(buffer);
            }
        }
        public void PushUInt64(ulong num)
        {
            if (ChunkSize - PushPosition >= 8)
            {
                BinaryTools.WriteUInt64(LastBuffer.AsSpan(PushPosition), num);
                PushPosition += 8;
            }
            else
            {
                Span<byte> buffer = stackalloc byte[8];
                BinaryTools.WriteUInt64(buffer, num);
                PushSpan(buffer);
            }
        }

        #endregion


        #region Pop

        public void PopTransferred(int count, Action<int, int> action = null)
        {
            ProcessTransfer(count, action, false);
        }
        public bool PopToSocket(Socket socket, SocketAsyncEventArgs eventArgs)
        {
            var count = Math.Min(ChunkSize - PopPosition, (int)Length);
            try
            {
                eventArgs.SetBuffer(FirstBuffer, PopPosition, count);
            }
            catch (Exception e)
            {
                throw new Exception($"socket set buffer error: {Length}, {PopPosition}, {count}", e);
            }
            //返回True则挂起走异步等回调OnSendComplete。
            //返回False则是同步，同步触发OnSendComplete。
            return socket.SendAsync(eventArgs);
        }

        public void PopToKcp(Kcp kcp, int maxSendLen)
        {
            PopTransferred(Math.Min((int)Length, maxSendLen), (offsetIndex, processLen) =>
            {
                kcp.Send(FirstBuffer.AsSpan(PopPosition, processLen));
            });
        }

        List<UniTask> _websockets;
        public async UniTask PopToWebSocketAsync(WebSocket webSocket, int maxSendLen, CancellationToken token)
        {
            int count = Math.Min((int)Length, maxSendLen);
            if (count <= 0) return;
            if (_websockets == null)
            {
                _websockets = new List<UniTask>();
            }
            _websockets.Clear();
            PopTransferred(count, (offsetIndex, processLen) =>
            {
                var task = webSocket.SendAsync(new ArraySegment<byte>(FirstBuffer, PopPosition, processLen),
                    WebSocketMessageType.Binary, true, token);
                _websockets.Add(task.AsUniTask());
            });
            await UniTask.WhenAll(_websockets);
        }
        public void PopToMemoryStream(MemoryStream memoryStream, int offset, int count)
        {
            if (count > Length)
            {
                throw new Exception($"bufferList length < count, {Length} {count}");
            }

            memoryStream.SetLength(offset + count);

            PopTransferred(count, (offsetIndex, processLen) =>
            {
                memoryStream.Seek(offsetIndex + offset, SeekOrigin.Begin);
                memoryStream.Write(FirstBuffer, PopPosition, processLen);
            });
        }

        public void PopToStream(Stream stream, int offset, int count)
        {
            if (count > Length)
            {
                throw new Exception($"bufferList length < count, {Length} {count}");
            }
            stream.SetLength(offset + count);
            PopTransferred(count, (offsetIndex, processLen) =>
            {
                stream.Seek(offsetIndex + offset, SeekOrigin.Begin);
                stream.Write(FirstBuffer, PopPosition, processLen);
            });
        }

        public void PopToSpan(Span<byte> data)
        {
            int dataLength = data.Length;
            int offsetIndex = 0;
            while (offsetIndex < dataLength)
            {
                int processLen = Math.Min(ChunkSize - PopPosition, dataLength - offsetIndex);
                FirstBuffer.AsSpan(PopPosition, processLen).CopyTo(data.Slice(offsetIndex));
                offsetIndex += processLen;
                PopPosition += processLen;
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
            PopTransferred(count, (offsetIndex, processLen) =>
            {
                //Array.Copy(FirstBuffer, PopPosition, bytes, len + index, processLen);
                FirstBuffer.AsSpan(PopPosition, processLen).CopyTo(bytes.AsSpan(offsetIndex + index, processLen));
            });
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
            var result = FirstBuffer[PopPosition];
            PopPosition++;
            return result;
        }
        public short PopInt16()
        {
            if (Length < 2)
            {
                throw new Exception($"bufferList length < 2, {Length}");
            }
            if (ChunkSize - PopPosition >= 2)
            {
                var result = BinaryTools.ReadInt16(FirstBuffer.AsSpan(PopPosition));
                PopPosition += 2;
                return result;
            }
            else
            {
                Span<byte> buffer = stackalloc byte[2];
                PopToSpan(buffer);
                return BinaryTools.ReadInt16(buffer);
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
                var result = BinaryTools.ReadUInt16(FirstBuffer.AsSpan(PopPosition));
                PopPosition += 2;
                return result;
            }
            else
            {
                Span<byte> buffer = stackalloc byte[2];
                PopToSpan(buffer);
                return BinaryTools.ReadUInt16(buffer);
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
                var result = BinaryTools.ReadInt32(FirstBuffer.AsSpan(PopPosition));
                PopPosition += 4;
                return result;
            }
            else
            {
                Span<byte> buffer = stackalloc byte[4];
                PopToSpan(buffer);
                return BinaryTools.ReadInt32(buffer);
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
                var result = BinaryTools.ReadUInt32(FirstBuffer.AsSpan(PopPosition));
                PopPosition += 4;
                return result;
            }
            else
            {
                Span<byte> buffer = stackalloc byte[4];
                PopToSpan(buffer);
                return BinaryTools.ReadUInt32(buffer);
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
                var result = BinaryTools.ReadInt64(FirstBuffer.AsSpan(PopPosition));
                PopPosition += 8;
                return result;
            }
            else
            {
                Span<byte> buffer = stackalloc byte[8];
                PopToSpan(buffer);
                return BinaryTools.ReadInt64(buffer);
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
                var result = BinaryTools.ReadUInt64(FirstBuffer.AsSpan(PopPosition));
                PopPosition += 8;
                return result;
            }
            else
            { 
                Span<byte> buffer = stackalloc byte[8];
                PopToSpan(buffer);
                return BinaryTools.ReadUInt64(buffer);
            }
        }
        #endregion
    }

}