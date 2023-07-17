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
        private IntPtr kcp;
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
                return !IsDisposed && (IsConnecting || IsConnected);
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
            this.kcp = Kcp.KcpCreate(conv, IntPtr.Zero);
            Kcp.KcpNodelay(kcp, 1, 10, 2, 1);
            Kcp.KcpWndsize(kcp, 256, 256);
            //Kcp.KcpSetmtu(kcp, 470); // 默认1400
            Kcp.KcpSetminrto(kcp, 30);
            Kcp.KcpSetoutput(KcpOutput);
            SendHeartbeat();//发送心跳验证是否可以进行网络链接
        }

        protected override void RecvHeartbeat()
        {
            if (IsConnecting)
            {
                OnConnect();
            }
        }


#if ENABLE_IL2CPP
		[AOT.MonoPInvokeCallback(typeof(KcpOutput))]
#endif
        private int KcpOutput(IntPtr bytes, int count, IntPtr kcp, IntPtr user)
        {
            if (this.IsDisposed)
            {
                return 0;
            }

            if (kcp == IntPtr.Zero)
            {
                return 0;
            }
            try
            {
                if (count == 0)
                {
                    Log.Error($"output 0");
                    return 0;
                }
                Marshal.Copy(bytes, sendCache, 0, count);
                this.socket.SendTo(sendCache, 0, count, SocketFlags.None, this.remoteEndPoint);
            }
            catch (Exception e)
            {
                Log.Error(e);                
            }

            return count;
        }

        protected override void StartSend()
        {
            try
            {
                if (this.kcp != IntPtr.Zero)
                {
                    // 检查等待发送的消息，如果超出最大等待大小，应该断开连接                    
                    if (Kcp.KcpWaitsnd(this.kcp) > Kcp.OuterMaxWaitSize)
                    {
                        Dispose();
                        return;
                    }
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
            if (this.kcp == IntPtr.Zero)
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
                        Kcp.KcpUpdate(this.kcp, timeNow);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                        return;
                    }
                    nextUpdateTime = Kcp.KcpCheck(this.kcp, timeNow);
                }
                CheckConnect();
            }
        }

        void CheckConnect()
        {
            if (IsConnected)
            {
                //超过一定时间后，没有回应，则判断为断开连接
                if (LastRecvTime > 0 && TimeMgr.Ins.TotalTime - LastRecvTime > 8)
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
                    Kcp.KcpInput(this.kcp, recvCache, 0, messageLength);
                    int len = 0;
                    while (!IsDisposed && (len = Kcp.KcpPeeksize(this.kcp)) > 0)
                    {
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
            if (this.kcp != IntPtr.Zero)
            {
                Kcp.KcpRelease(this.kcp);
                this.kcp = IntPtr.Zero;
            }
            this.socket?.Close();
            this.socket = null;
        }
    }
}
