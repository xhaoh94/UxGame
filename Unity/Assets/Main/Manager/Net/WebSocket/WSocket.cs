using Cysharp.Threading.Tasks;
using Ux;
using System;
using System.Net.WebSockets;
using System.Threading;

namespace Ux
{
    public class WSocket : ClientSocket
    {
        public HttpListenerWebSocketContext WebSocketContext { get; }

        private WebSocket webSocket;
        private const int maxSendLen = 2048;

        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        public WSocket(string address) : base(address) { }

        protected override void ToDisconnect()
        {
            base.ToDisconnect();            
            webSocket.Abort();
        }
        protected override void ToConnect(string address)
        {
            this.webSocket = new ClientWebSocket();
            this.ConnectAsync(address).Forget();
        }

        async UniTask ConnectAsync(string address)
        {
            try
            {
                await ((ClientWebSocket)this.webSocket).ConnectAsync(new Uri(address), cancellationTokenSource.Token);
                this.StartRecv().Forget();
                OnConnect();
            }
            catch (Exception e)
            {
                Log.Error(e);
                this.OnSocketCode(SocketCode.ConnectionTimeout);
            }
        }

        protected override void StartSend()
        {
            _SendSync().Forget();
        }
        
        async UniTaskVoid _SendSync()
        {
            try
            {
                await this.sendBytes.PopToWebSocketAsync(this.webSocket, maxSendLen, cancellationTokenSource.Token);
                EndSend();
            }
            catch (Exception e)
            {
                Log.Error(e);
                this.OnSocketCode(SocketCode.Error);
            }
        }

        public async UniTaskVoid StartRecv()
        {
            if (this.IsDisposed)
            {
                return;
            }

            try
            {
                while (true)
                {
                    WebSocketReceiveResult receiveResult;
                    do
                    {
                        receiveResult = await this.recvBytes.PushByWebSocketAsync(webSocket, cancellationTokenSource.Token);
                        if (this.IsDisposed)
                        {
                            return;
                        }
                        this.recvBytes.PushTransferred(receiveResult.Count);
                    }
                    while (!receiveResult.EndOfMessage);

                    if (receiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        return;
                    }

                    if (receiveResult.Count > ushort.MaxValue)
                    {
                        await this.webSocket.CloseAsync(WebSocketCloseStatus.MessageTooBig, $"message too big: {receiveResult.Count}",
                            cancellationTokenSource.Token);
                        return;
                    }

                    if (!this.OnParse())
                    {
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
                this.OnSocketCode(SocketCode.Error);
            }
        }

        protected override void OnDispose()
        {
            this.cancellationTokenSource.Cancel();
            this.cancellationTokenSource.Dispose();
            this.cancellationTokenSource = null;
            this.webSocket.Dispose();
            this.webSocket = null;
        }
    }
}