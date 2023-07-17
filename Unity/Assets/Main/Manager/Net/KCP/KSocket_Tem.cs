//using System;
//using System.Buffers;
//using System.Net;
//using System.Net.Sockets;

//namespace Main
//{
//    public class KSocket : ClientSocket, IKcpCallback
//    {
//        public PoolSegManager.Kcp kcp { get; }
//        //public UnSafeSegManager.Kcp kcp { get; }
//        private readonly int maxSendLen = 2048;
//        UdpClient client;
//        IPEndPoint remoteIpEndPoint;

//        public KSocket() : base()
//        {
//            kcp = new PoolSegManager.Kcp(2001, this);
//            //kcp = new UnSafeSegManager.Kcp(2001, this);
//            kcp.NoDelay(1, 10, 2, 1);
//            kcp.WndSize(256, 256);
//            //kcp.SetMtu(512);
//        }
//        public override void Connect(string address)
//        {
//            var ipAddress = address.Split(':');
//            remoteIpEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress[0]), int.Parse(ipAddress[1]));
//            client = new UdpClient();
//            OnConnect();
//            StartRecv();
//        }

//        public override void Update()
//        {
//            base.Update();
//            if (IsConnected)
//            {
//                kcp.Update(DateTimeOffset.UtcNow);
//                int len;
//                while ((len = kcp.PeekSize()) > 0)
//                {
//                    var buffer = new byte[len];
//                    if (kcp.Recv(buffer) >= 0)
//                    {
//                        this.recvBuffer.WriteBytes(buffer, 0, len);
//                        if (!this.OnParse())
//                        {
//                            return;
//                        }
//                    }
//                }
//            }
//        }

//        public async void StartRecv()
//        {
//            if (this.IsDisposed)
//            {
//                return;
//            }

//            try
//            {
//                var result = await client.ReceiveAsync();
//                remoteIpEndPoint = result.RemoteEndPoint;
//                kcp.Input(result.Buffer);
//                StartRecv();
//            }
//            catch (Exception e)
//            {
//                Log.Error(e);
//            }
//        }

//        protected override void StartSend()
//        {
//            if (!this.IsConnected)
//            {
//                return;
//            }

//            // 没有数据需要发送
//            if (this.sendBuffer.Length == 0)
//            {
//                this.isSending = false;
//                return;
//            }

//            this.isSending = true;

//            while (true)
//            {
//                var sendLen = (int)this.sendBuffer.Length;
//                if (sendLen == 0)
//                {
//                    this.isSending = false;
//                    return;
//                }
//                if (sendLen > maxSendLen)
//                {
//                    sendLen = maxSendLen;
//                }

//                byte[] bytes = this.sendBuffer.ReadBytes(sendLen);
//                try
//                {
//                    kcp.Send(bytes);
//                }
//                catch (Exception e)
//                {
//                    Log.Error(e);
//                    return;
//                }
//            }
//        }

//        public void Output(IMemoryOwner<byte> buffer, int avalidLength)
//        {
//            using (buffer)
//            {
//                var s = buffer.Memory.Span.Slice(0, avalidLength).ToArray();
//                client.SendAsync(s, s.Length, remoteIpEndPoint);
//            }
//        }
//    }
//}
