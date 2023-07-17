using System;
using System.Net.Sockets;
using System.Net;

namespace Ux
{
    internal class TSocket : ClientSocket
    {
        private Socket socket;
        private SocketAsyncEventArgs innArgs = new SocketAsyncEventArgs();
        private SocketAsyncEventArgs outArgs = new SocketAsyncEventArgs();

        public TSocket(string address) : base(address)
        {
            this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.socket.NoDelay = true;
            this.innArgs.Completed += IO_Complete;
            this.outArgs.Completed += IO_Complete;
        }
        private void IO_Complete(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Connect:
                    OneThreadSynchronizationContext.Instance.Post(this.OnConnectComplete, e);
                    break;
                case SocketAsyncOperation.Receive:
                    OneThreadSynchronizationContext.Instance.Post(this.OnRecvComplete, e);
                    break;
                case SocketAsyncOperation.Send:
                    OneThreadSynchronizationContext.Instance.Post(this.OnSendComplete, e);
                    break;
                case SocketAsyncOperation.Disconnect:
                    OneThreadSynchronizationContext.Instance.Post(this.OnDisconnectComplete, e);
                    break;
                default:
                    throw new Exception($"socket error: {e.LastOperation}");
            }
        }

        protected override void ToConnect(string address)
        {
            var ipAddress = address.Split(':');
            var remoteIpEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress[0]), int.Parse(ipAddress[1]));
            this.outArgs.RemoteEndPoint = remoteIpEndPoint;
            if (this.socket.ConnectAsync(this.outArgs))
            {
                return;
            }
            OnConnectComplete(this.outArgs);
        }

        private void OnConnectComplete(object o)
        {
            if (this.socket == null)
            {
                return;
            }
            SocketAsyncEventArgs args = (SocketAsyncEventArgs)o;

            if (args.SocketError != SocketError.Success)
            {
                this.OnError(args.SocketError);
                return;
            }

            args.RemoteEndPoint = null;
            this.StartRecv();
            OnConnect();
        }

        private void OnDisconnectComplete(object o)
        {
            SocketAsyncEventArgs args = (SocketAsyncEventArgs)o;
            this.OnError(args.SocketError);
        }

        public void StartRecv()
        {
            if (!this.recvBytes.PushBySocket(socket, innArgs))
            {
                return;
            }
            OnRecvComplete(this.innArgs);
        }

        private void OnRecvComplete(object o)
        {
            if (this.socket == null)
            {
                return;
            }
            SocketAsyncEventArgs args = (SocketAsyncEventArgs)o;

            if (args.SocketError != SocketError.Success)
            {
                this.OnError(args.SocketError);
                return;
            }

            if (args.BytesTransferred == 0)
            {
                return;
            }

            this.recvBytes.PushTransferred(args.BytesTransferred);

            if (!this.OnParse())
            {
                return;
            }

            if (this.socket == null)
            {
                return;
            }

            this.StartRecv();
        }

        protected override void StartSend()
        {
            try
            {
                if (!this.sendBytes.PopToSocket(this.socket, this.outArgs))
                {
                    return;
                }
                OnSendComplete(this.outArgs);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private void OnSendComplete(object o)
        {
            if (this.socket == null)
            {
                return;
            }
            SocketAsyncEventArgs args = (SocketAsyncEventArgs)o;

            if (args.SocketError != SocketError.Success)
            {
                this.OnError(args.SocketError);
                return;
            }

            if (args.BytesTransferred == 0)
            {
                return;
            }

            this.sendBytes.PopTransferred(args.BytesTransferred);
        }

        protected override void OnDispose()
        {            
            this.socket?.Close();
            this.innArgs.Dispose();
            this.outArgs.Dispose();
            this.innArgs = null;
            this.outArgs = null;
            this.socket = null;
        }

        private void OnError(SocketError error)
        {
            Log.Debug("socketerror:" + error);
            switch (error)
            {
                case SocketError.ConnectionRefused:
                    OnSocketCode(SocketCode.ConnectionTimeout);
                    break;
                default:
                    OnSocketCode(SocketCode.Error);
                    break;
            }
        }
    }
}
