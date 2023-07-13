using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace Ux
{
    public class KSocket : ClientSocket
    {
        private Socket socket;
        private Kcp kcp;
        private IPEndPoint remoteEndPoint;
        EndPoint remoteEP2 = new IPEndPoint(IPAddress.Any, 0);

        private readonly byte[] recvCache = new byte[2048];
        private readonly byte[] sendCache = new byte[2048];

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
                return IsConnecting || base.IsCheckUpdate;
            }
        }
        public KSocket(string address) : base(address)
        {
            this.startTime = TimeMgr.Ins.LocalTime.TimeStamp;
        }

        protected override void ToConnect(string address)
        {
            var ipAddress = address.Split(':');
            remoteEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress[0]), int.Parse(ipAddress[1]));
            this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            var conv = (uint)((ulong)IDGenerater.GenerateId() & uint.MaxValue);
            this.kcp = new Kcp(conv, KcpOutput);
            this.kcp.SetNoDelay(1, 10, 2, true);
            this.kcp.SetWindowSize(256, 256);
            this.kcp.SetMtu(470); // 默认1400
            this.kcp.SetMinrto(30);
            this.kcp.InitArrayPool(600, 10000);
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
            try
            {
                try
                {
                    if (count == 0)
                    {
                        Log.Error($"output 0");
                        return;
                    }
                    this.socket.SendTo(bytes, 0, count, SocketFlags.None, this.remoteEndPoint);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
                return;
            }

            return;
        }

        protected override void StartSend()
        {
            try
            {
                if (this.kcp == null)
                {
                    throw new Exception("未初始化KCP服务!");
                }
                // 检查等待发送的消息，如果超出最大等待大小，应该断开连接                    
                if (this.kcp.WaitSnd > Kcp.OuterMaxWaitSize)
                {
                    Dispose();
                    return;
                }
                this.sendBytes.ReadToKcp(this.kcp, maxSendLen);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        public override void Update()
        {
            if (this.IsDisposed)
            {
                return;
            }
            if (this.kcp == null)
            {
                return;
            }
            base.Update();
            if (IsCheckUpdate)
            {
                StartRecv();
                CheckConnect();
            }
        }

        void CheckConnect()
        {
            if (IsConnecting)
            {
                if (!this.IsConnected)
                {
                    // 一定时间后没连接上则退出连接
                    if (TimeNow > 10 * 1000)
                    {
                        this.OnSocketCode(SocketCode.ConnectionTimeout);
                        return;
                    }
                }
            }
            else
            {
                //超过心跳一定时间后，没有回应，则判断为断开连接
                if (LastRecvTime > 0 && TimeMgr.Ins.TotalTime - LastRecvTime > heartTime + 10)
                {
                    this.OnSocketCode(SocketCode.Error);
                    return;
                }

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
            }
        }


        void StartRecv()
        {
            try
            {
                if (this.IsDisposed)
                {
                    return;
                }

                if (this.socket == null)
                {
                    return;
                }

                while (socket != null && this.socket.Available > 0)
                {
                    if (this.IsDisposed)
                    {
                        return;
                    }

                    int messageLength = this.socket.ReceiveFrom(this.recvCache, ref this.remoteEP2);
                    if (messageLength == 0)
                    {
                        continue;
                    }
                    this.kcp.Input(this.recvCache.AsSpan(0, messageLength));                    
                    
                    while (true)
                    {
                        if (this.IsDisposed)
                        {
                            return;
                        }

                        int len = this.kcp.PeekSize();
                        if (len < 0)
                        {
                            break;
                        }
                        if (len == 0)
                        {
                            this.OnDispose();
                            return;
                        }
                        this.recvBytes.WriteKcp(this.kcp, len);
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
            this.socket?.Close();
            this.socket = null;
            this.kcp = null;
        }
    }
}
