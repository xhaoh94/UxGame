using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace Ux
{
    public enum NetType
    {
        KCP,
        TCP,
        WebSocket,
    }
    public class NetMgr : Singleton<NetMgr>
    {
        readonly List<ClientSocket> _clientSockets = new List<ClientSocket>();
        ClientSocket _clientSocket;

        protected override void OnCreated()
        {
            GameMain.Ins.AddFixedUpdate(_Update);
            EventMgr.Ins.On<ClientSocket>(MainEventType.NET_DISPOSE, this, OnSocketDispose);
        }
        /// <summary>
        /// 创建远程服务
        /// </summary>
        public ClientSocket Create(NetType netType, string address)
        {
            var clientSocket = Get(address);
            if (clientSocket != null)
            {
                Log.Error($"重复创建Socket->Address:[{address}]");
                return null;
            }

            clientSocket = netType switch
            {
                NetType.KCP => new KSocket(address),
                NetType.TCP => new TSocket(address),
                NetType.WebSocket => new WSocket(address),
                _ => null
            };
            _clientSockets.Add(clientSocket);
            if (_clientSocket == null)
            {
                SetDefaultClient(clientSocket);
            }
            return clientSocket;
        }
        public ClientSocket Get(string address)
        {
            if (_clientSocket != null && _clientSocket.Address == address) return _clientSocket;
            return _clientSockets.Find(x => x.Address == address);
        }

        /// <summary>
        /// 连接远程服务
        /// </summary>
        public ClientSocket Connect(NetType netType, string address, Action connectCallback)
        {
            var clientSocket = Create(netType, address);
            clientSocket.SetConnectCallback(connectCallback);
            clientSocket?.Connect();
            return clientSocket;
        }
        /// <summary>
        /// 连接远程服务
        /// </summary>
        public ClientSocket Connect(NetType netType, string address, Action<object> connectCallback, object param)
        {
            var clientSocket = Create(netType, address);
            clientSocket.SetConnectCallback(connectCallback, param);
            clientSocket?.Connect();
            return clientSocket;
        }
        public void Disconnect(ClientSocket socket = null)
        {
            if (socket == null) socket = _clientSocket;
            if (socket == null) return;
            socket.Disconnect();
            if (socket == _clientSocket)
            {
                _clientSocket = null;
            }
        }
        public void SetDefaultClient(ClientSocket clientSocket)
        {
            _clientSocket = clientSocket;
        }
        public void Send(uint cmd, object message)
        {
            _clientSocket?.Send(cmd, message);
        }
        public async UniTask<TMessage> Call<TMessage>(uint cmd, object message)
        {
            try
            {
                var response = await _clientSocket.Call<TMessage>(cmd, message);
                return (TMessage)response;
            }
            catch (Exception ex)
            {
                if (!(ex is OperationCanceledException))
                {
                    Log.Error(ex);
                }
                return default(TMessage);
            }
        }
        void _Update()
        {
            foreach (var client in _clientSockets)
            {
                client.Update();
            }
        }

        public void Release()
        {
            _clientSocket = null;
            for (int i = _clientSockets.Count - 1; i >= 0; i--)
            {
                _clientSockets[i].Dispose();
            }
            _clientSockets.Clear();
        }


        void OnSocketDispose(ClientSocket clientSocket)
        {
            _clientSockets.Remove(clientSocket);
            if (_clientSocket == clientSocket)
            {
                _clientSocket = null;
            }
        }
    }
}
