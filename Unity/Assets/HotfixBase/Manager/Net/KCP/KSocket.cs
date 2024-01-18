using SJ;
using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Ux
{
    public class KSocket : ClientSocket
    {
        private Socket socket;
        private Kcp kcp;
        private IPEndPoint remoteEndPoint;
        EndPoint remoteEP2 = new IPEndPoint(IPAddress.Any, 0);

        private readonly byte[] recvCache = new byte[2048];

        private const int maxSendLen = 2048;

        // KSocket创建的时间
        readonly long startTime;

        // 当前时间 - KSocket创建的时间
        public uint TimeNow => (uint)(TimeMgr.Ins.LocalTime.TimeStamp - startTime);
        uint nextUpdateTime;
        protected override bool IsCheckUpdate
        {
            get
            {
                return !IsDisposed && (IsConnecting || IsConnected);
            }
        }
        public KSocket(string address) : base(address)
        {
            this.startTime = TimeMgr.Ins.LocalTime.TimeStamp;
        }

        protected override void ToDisconnect()
        {
            base.ToDisconnect();
            socket.Shutdown(SocketShutdown.Both);
            socket.Disconnect(false);
        }
        protected override void ToConnect(string address)
        {
            var ipAddress = address.Split(':');
            remoteEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress[0]), int.Parse(ipAddress[1]));
            this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            var conv = (uint)((ulong)IDGenerater.GenerateId() & uint.MaxValue);
            this.kcp = new Kcp(conv, KcpOutput);
            kcp.SetNoDelay(1, 10, 2, true);
            kcp.SetWindowSize(256, 256);
            //kcp.SetMtu(470); // 默认1400
            kcp.SetMinrto(30);
            SendHeartbeat();//发送心跳验证是否可以进行网络链接
        }

        protected override void RecvHeartbeat()
        {
            if (IsConnecting)
            {
                OnConnect();
            }
        }

        private void KcpOutput(byte[] bytes, int count)
        {
            if (this.IsDisposed)
            {
                return;
            }

            if (kcp == null)
            {
                return;
            }
            try
            {
                if (count == 0)
                {
                    Log.Error($"output 0");
                    return;
                }
                this.socket.SendTo(bytes, 0, count, SocketFlags.None, this.remoteEndPoint);
                EndSend();
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        protected override void StartSend()
        {
            try
            {
                if (this.kcp != null)
                {
                    // 检查等待发送的消息，如果超出最大等待大小，应该断开连接                    
                    if (this.kcp.WaitSnd > Kcp.OuterMaxWaitSize)
                    {
                        Dispose();
                        return;
                    }
                }
                this.sendBytes.PopToKcp(this.kcp, maxSendLen);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        public override void Update()
        {
            if (this.kcp == null)
            {
                return;
            }
            base.Update();
            if (IsCheckUpdate)
            {
                StartRecv();
                var timeNow = TimeNow;
                if (nextUpdateTime <= timeNow)
                {
                    try
                    {
                        this.kcp.Update(timeNow);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                        return;
                    }
                    nextUpdateTime = this.kcp.Check(timeNow);
                }
                CheckConnect();
            }
        }

        void CheckConnect()
        {
            if (IsConnected)
            {
                //超过一定时间后，没有回应，则判断为断开连接
                if (LastRecvTime > 0 && TimeMgr.Ins.TotalTime - LastRecvTime > heartTime + 5)
                {
                    this.OnSocketCode(SocketCode.Error);
                }
            }
            else
            {
                // 一定时间后没连接上则退出连接
                if (TimeNow > 8 * 1000)
                {
                    this.OnSocketCode(SocketCode.ConnectionTimeout);
                }
            }
        }


        void StartRecv()
        {
            try
            {
                while (!IsDisposed && socket != null && this.socket.Available > 0)
                {
                    int messageLength = this.socket.ReceiveFrom(this.recvCache, ref this.remoteEP2);
                    if (messageLength == 0)
                    {
                        continue;
                    }
                    this.kcp.Input(recvCache.AsSpan(0, messageLength));
                    int len = 0;

                    while (!IsDisposed && (len = this.kcp.PeekSize()) > 0)
                    {
                        this.recvBytes.PushByKcp(this.kcp, len);
                        if (!this.OnParse())
                        {
                            return;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        protected override void OnDispose()
        {
            if (this.kcp != null)
            {
                this.kcp = null;
            }
            this.socket?.Close();
            this.socket = null;
        }
    }
}
