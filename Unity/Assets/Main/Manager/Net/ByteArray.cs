using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using SJ;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Ux
{
    public class ByteArray
    {
        public const int ChunkSize = 8192;

        readonly Queue<byte[]> _buffers = new Queue<byte[]>();

        readonly Queue<byte[]> _caches = new Queue<byte[]>();

        readonly Dictionary<int, byte[]> _bytesDict = new Dictionary<int, byte[]>();

        int _PushPosition;
        int PushPosition
        {
            get => _PushPosition;
            set
            {
                _PushPosition = value;
                if (_PushPosition == ChunkSize)
                {
                    Add();
                }
            }
        }
        int _PopPosition;
        int PopPosition
        {
            get => _PopPosition;
            set
            {
                _PopPosition = value;
                if (_PopPosition == ChunkSize)
                {
                    Remove();
                }
            }
        }
        byte[] LastBuffer { get; set; }
        byte[] FirstBuffer => _buffers.Peek();

        public ByteArray()
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
            var buffer = _caches.Count > 0 ? _caches.Dequeue() : new byte[ChunkSize];
            _buffers.Enqueue(buffer);
            LastBuffer = buffer;
            _PushPosition = 0;
        }
        void Remove()
        {
            _caches.Enqueue(_buffers.Dequeue());
            _PopPosition = 0;
        }

        public void Dispose()
        {
            _buffers.Clear();
            _caches.Clear();
            _PopPosition = 0;
            _PopPosition = 0;
        }

        #region Push
        public void PushTransferred(int count, Action<int, int> action = null)
        {
            int len = 0;
            while (len < count)
            {
                int temLen = count - len;
                if (PushPosition + temLen > ChunkSize)
                {
                    temLen = ChunkSize - PushPosition;
                }
                action?.Invoke(len, temLen);
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
            PushTransferred(count, (len, temLen) =>
            {
                kcp.Receive(LastBuffer.AsSpan(PushPosition, temLen));
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
        public void PushStream(Stream stream)
        {
            //因为前面序列化的时候Seek(0, SeekOrigin.Begin)，            
            //所以真实长度是Position而不是Length，Length是包含之前部分的            
            int count = (int)stream.Position;
            stream.Seek(0, SeekOrigin.Begin);
            PushTransferred(count, (len, temLen) =>
            {
                stream.Read(LastBuffer, PushPosition, temLen);
            });
        }
        public void PushBytes(byte[] bytes, int index, int count)
        {
            PushTransferred(count, (len, temLen) =>
            {
                Array.Copy(bytes, len + index, LastBuffer, PushPosition, temLen);
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

        public void PopTransferred(int count, Action<int, int> action = null)
        {
            int len = 0;
            while (len < count)
            {
                int temLen = count - len;
                if (PopPosition + count > ChunkSize)
                {
                    temLen = ChunkSize - PopPosition;
                }
                action?.Invoke(len, temLen);
                PopPosition += temLen;
                len += temLen;
            }
        }

        public bool PopToSocket(Socket socket, SocketAsyncEventArgs eventArgs)
        {
            var count = ChunkSize - PopPosition;
            if (count > Length)
            {
                count = (int)Length;
            }

            try
            {
                //var str = "[";
                //for (var i = PopPosition; i < PopPosition + count; i++)
                //{
                //    if (i > PopPosition)
                //    {
                //        str += ", ";
                //    }
                //    str += FirstBuffer[i].ToString();
                //}
                //str += "]";
                //Log.Debug(str);
                eventArgs.SetBuffer(FirstBuffer, PopPosition, count);
            }
            catch (Exception e)
            {
                throw new Exception($"socket set buffer error: {Length}, {PopPosition}, {count}", e);
            }
            //返回True则挂起走异步OnSendComplete。
            //返回False则是同步，不会触发OnSendComplete。
            return socket.SendAsync(eventArgs);
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

            PopTransferred(count, (len, temLen) =>
            {
                kcp.Send(FirstBuffer.AsSpan(PopPosition, temLen));
            });
        }

        List<UniTask> _websockets;
        public async UniTask PopToWebSocketAsync(WebSocket webSocket, int maxSendLen, CancellationToken token)
        {
            var count = (int)Length;
            if (count > maxSendLen)
            {
                count = maxSendLen;
            }
            if (_websockets == null)
            {
                _websockets = new List<UniTask>();
            }
            _websockets.Clear();
            PopTransferred(count, (len, temLen) =>
            {
                var task = webSocket.SendAsync(new ArraySegment<byte>(FirstBuffer, PopPosition, temLen),
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

            PopTransferred(count, (len, temLen) =>
            {
                memoryStream.Seek(len + offset, SeekOrigin.Begin);
                memoryStream.Write(FirstBuffer, PopPosition, temLen);
            });
        }

        public void PopToStream(Stream stream, int offset, int count)
        {
            if (count > Length)
            {
                throw new Exception($"bufferList length < count, {Length} {count}");
            }
            stream.SetLength(offset + count);
            PopTransferred(count, (len, temLen) =>
            {
                stream.Seek(len + offset, SeekOrigin.Begin);
                stream.Write(FirstBuffer, PopPosition, temLen);
            });
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
            PopTransferred(count, (len, temLen) =>
            {
                Array.Copy(FirstBuffer, PopPosition, bytes, len + index, temLen);
            });
        }

        public byte[] PopBytes(int count)
        {
            if (!_bytesDict.TryGetValue(count, out var bytes))
            {
                bytes = new byte[count];
                _bytesDict.Add(count, bytes);
            }
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
                var result = BitConverter.ToInt16(FirstBuffer, PopPosition);
                PopPosition += 2;
                return result;
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
                var result = BitConverter.ToUInt16(FirstBuffer, PopPosition);
                PopPosition += 2;
                return result;
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
                var result = BitConverter.ToInt32(FirstBuffer, PopPosition);
                PopPosition += 4;
                return result;
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
                var result = BitConverter.ToUInt32(FirstBuffer, PopPosition);
                PopPosition += 4;
                return result;
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
                var result = BitConverter.ToInt64(FirstBuffer, PopPosition);
                PopPosition += 8;
                return result;
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
                var result = BitConverter.ToUInt64(FirstBuffer, PopPosition);
                PopPosition += 8;
                return result;
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