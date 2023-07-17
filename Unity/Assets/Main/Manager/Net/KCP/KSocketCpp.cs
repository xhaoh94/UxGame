using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace Ux
{
    #region Cpp
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int KcpOutput(IntPtr buf, int len, IntPtr kcp, IntPtr user);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void KcpLog(IntPtr buf, int len, IntPtr kcp, IntPtr user);

    public static class Kcp_cpp
    {
        public const int OneM = 1024 * 1024;
        public const int InnerMaxWaitSize = 1024 * 1024;
        public const int OuterMaxWaitSize = 1024 * 1024;


        private static KcpOutput KcpOutput;
        private static KcpLog KcpLog;

#if UNITY_IPHONE && !UNITY_EDITOR
        const string KcpDLL = "__Internal";
#else
        const string KcpDLL = "kcp";
#endif

        [DllImport(KcpDLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint ikcp_check(IntPtr kcp, uint current);
        [DllImport(KcpDLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr ikcp_create(uint conv, IntPtr user);
        [DllImport(KcpDLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ikcp_flush(IntPtr kcp);
        [DllImport(KcpDLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint ikcp_getconv(IntPtr ptr);
        [DllImport(KcpDLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int ikcp_input(IntPtr kcp, byte[] data, int offset, int size);
        [DllImport(KcpDLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int ikcp_nodelay(IntPtr kcp, int nodelay, int interval, int resend, int nc);
        [DllImport(KcpDLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int ikcp_peeksize(IntPtr kcp);
        [DllImport(KcpDLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int ikcp_recv(IntPtr kcp, byte[] buffer, int index, int len);
        [DllImport(KcpDLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ikcp_release(IntPtr kcp);
        [DllImport(KcpDLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int ikcp_send(IntPtr kcp, byte[] buffer, int offset, int len);
        [DllImport(KcpDLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ikcp_setminrto(IntPtr ptr, int minrto);
        [DllImport(KcpDLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int ikcp_setmtu(IntPtr kcp, int mtu);
        [DllImport(KcpDLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ikcp_setoutput(KcpOutput output);

        [DllImport(KcpDLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ikcp_setlog(KcpLog log);

        [DllImport(KcpDLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ikcp_update(IntPtr kcp, uint current);
        [DllImport(KcpDLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int ikcp_waitsnd(IntPtr kcp);
        [DllImport(KcpDLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int ikcp_wndsize(IntPtr kcp, int sndwnd, int rcvwnd);

        public static uint KcpCheck(IntPtr kcp, uint current)
        {
            if (kcp == IntPtr.Zero)
            {
                throw new Exception($"kcp error, kcp point is zero");
            }
            uint ret = ikcp_check(kcp, current);
            return ret;
        }

        public static IntPtr KcpCreate(uint conv, IntPtr user)
        {
            return ikcp_create(conv, user);
        }

        public static void KcpFlush(IntPtr kcp)
        {
            if (kcp == IntPtr.Zero)
            {
                throw new Exception($"kcp error, kcp point is zero");
            }
            ikcp_flush(kcp);
        }

        public static uint KcpGetconv(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                throw new Exception($"kcp error, kcp point is zero");
            }
            return ikcp_getconv(ptr);
        }

        public static int KcpInput(IntPtr kcp, byte[] buffer, int offset, int len)
        {
            if (kcp == IntPtr.Zero)
            {
                throw new Exception($"kcp error, kcp point is zero");
            }
            if (offset + len > buffer.Length)
            {
                throw new Exception($"kcp error, KcpInput {buffer.Length} {offset} {len}");
            }
            int ret = ikcp_input(kcp, buffer, offset, len);
            return ret;
        }

        public static int KcpNodelay(IntPtr kcp, int nodelay, int interval, int resend, int nc)
        {
            if (kcp == IntPtr.Zero)
            {
                throw new Exception($"kcp error, kcp point is zero");
            }
            return ikcp_nodelay(kcp, nodelay, interval, resend, nc);
        }

        public static int KcpPeeksize(IntPtr kcp)
        {
            if (kcp == IntPtr.Zero)
            {
                throw new Exception($"kcp error, kcp point is zero");
            }
            int ret = ikcp_peeksize(kcp);
            return ret;
        }

        public static int KcpRecv(IntPtr kcp, byte[] buffer, int index, int len)
        {
            if (kcp == IntPtr.Zero)
            {
                throw new Exception($"kcp error, kcp point is zero");
            }

            if (buffer.Length < index + len)
            {
                throw new Exception($"kcp error, KcpRecv error: {index} {len}");
            }

            int ret = ikcp_recv(kcp, buffer, index, len);
            return ret;
        }

        public static void KcpRelease(IntPtr kcp)
        {
            if (kcp == IntPtr.Zero)
            {
                throw new Exception($"kcp error, kcp point is zero");
            }
            ikcp_release(kcp);
        }

        public static int KcpSend(IntPtr kcp, byte[] buffer, int offset, int len)
        {
            if (kcp == IntPtr.Zero)
            {
                throw new Exception($"kcp error, kcp point is zero");
            }

            if (offset + len > buffer.Length)
            {
                throw new Exception($"kcp error, KcpSend {buffer.Length} {offset} {len}");
            }

            int ret = ikcp_send(kcp, buffer, offset, len);
            return ret;
        }

        public static void KcpSetminrto(IntPtr kcp, int minrto)
        {
            if (kcp == IntPtr.Zero)
            {
                throw new Exception($"kcp error, kcp point is zero");
            }
            ikcp_setminrto(kcp, minrto);
        }

        public static int KcpSetmtu(IntPtr kcp, int mtu)
        {
            if (kcp == IntPtr.Zero)
            {
                throw new Exception($"kcp error, kcp point is zero");
            }
            return ikcp_setmtu(kcp, mtu);
        }

        public static void KcpSetoutput(KcpOutput output)
        {
            KcpOutput = output;
            ikcp_setoutput(KcpOutput);
        }

        public static void KcpSetLog(KcpLog kcpLog)
        {
            KcpLog = kcpLog;
            ikcp_setlog(KcpLog);
        }

        public static void KcpUpdate(IntPtr kcp, uint current)
        {
            if (kcp == IntPtr.Zero)
            {
                throw new Exception($"kcp error, kcp point is zero");
            }
            ikcp_update(kcp, current);
        }

        public static int KcpWaitsnd(IntPtr kcp)
        {
            if (kcp == IntPtr.Zero)
            {
                throw new Exception($"kcp error, kcp point is zero");
            }
            int ret = ikcp_waitsnd(kcp);
            return ret;
        }

        public static int KcpWndsize(IntPtr kcp, int sndwnd, int rcvwnd)
        {
            if (kcp == IntPtr.Zero)
            {
                throw new Exception($"kcp error, kcp point is zero");
            }
            return ikcp_wndsize(kcp, sndwnd, rcvwnd);
        }
    }
#endregion

    public class KSocketCpp : ClientSocket
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
        public KSocketCpp(string address) : base(address)
        {
            this.startTime = TimeMgr.Ins.LocalTime.TimeStamp;
        }


        protected override void ToConnect(string address)
        {
            var ipAddress = address.Split(':');
            remoteEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress[0]), int.Parse(ipAddress[1]));
            this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            var conv = (uint)((ulong)IDGenerater.GenerateId() & uint.MaxValue);
            this.kcp = Kcp_cpp.KcpCreate(conv, IntPtr.Zero);
            Kcp_cpp.KcpNodelay(kcp, 1, 10, 2, 1);
            Kcp_cpp.KcpWndsize(kcp, 256, 256);
            //Kcp.KcpSetmtu(kcp, 470); // 默认1400
            Kcp_cpp.KcpSetminrto(kcp, 30);
            Kcp_cpp.KcpSetoutput(KcpOutput);
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
                    if (Kcp_cpp.KcpWaitsnd(this.kcp) > Kcp_cpp.OuterMaxWaitSize)
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
                        Kcp_cpp.KcpUpdate(this.kcp, timeNow);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                        return;
                    }
                    nextUpdateTime = Kcp_cpp.KcpCheck(this.kcp, timeNow);
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
                    Kcp_cpp.KcpInput(this.kcp, recvCache, 0, messageLength);
                    int len = 0;
                    while (!IsDisposed && (len = Kcp_cpp.KcpPeeksize(this.kcp)) > 0)
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
            if (this.kcp != IntPtr.Zero)
            {
                Kcp_cpp.KcpRelease(this.kcp);
                this.kcp = IntPtr.Zero;
            }
            this.socket?.Close();
            this.socket = null;
        }
    }

    
}
