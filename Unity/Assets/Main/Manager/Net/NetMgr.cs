using Cysharp.Threading.Tasks;
using DCET;
using System;
using System.Collections.Generic;
using Ux.Main;

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
            clientSocket?.Connect(connectCallback);
            return clientSocket;
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
            var response = await _clientSocket.Call<TMessage>(cmd, message);
            return (TMessage)response;
        }
        public void Update()
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


        [Main.Evt(EventType.NET_DISPOSE)]
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
