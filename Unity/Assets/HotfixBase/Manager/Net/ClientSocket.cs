﻿using Cysharp.Threading.Tasks;
using Microsoft.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
namespace Ux
{
    public enum SocketCode
    {
        Success,
        Error,
        ConnectionTimeout,//连接服务器超时
    }
    public abstract class ClientSocket
    {
        enum OpType
        {
            H_B_S = 0x01,
            H_B_R,
            C_S_C,
            RPC_REQUIRE,
            RPC_RESPONSE
        }

        static uint _rpxOps = 1;
        static uint GetRpxID()
        {
            if (_rpxOps == uint.MaxValue)
            {
                _rpxOps = 0;
            }
            return _rpxOps++;
        }

        public string Address { get; private set; }
        //RPC响应超时
        const float RPC_TimeOut = 3f;

        //心跳间隔（秒）
        protected readonly int heartTime = 30;
        protected float LastRecvTime { get; private set; }
        protected float LastSendTime { get; private set; }

        private bool isNeedSend;
        private bool IsSending;

        protected bool IsConnecting { get; private set; }
        protected bool IsConnected { get; private set; }
        protected bool IsDisposed { get; private set; }
        protected virtual bool IsCheckUpdate => !IsDisposed && IsConnected;

        private readonly PacketParser parser;
        protected readonly ByteArray recvBytes = new ByteArray();
        protected readonly ByteArray sendBytes = new ByteArray();

        private MemoryStream sendStream;
        private MemoryStream recvStream;


        Dictionary<uint, AutoResetUniTaskCompletionSource<object>> rpcMethod = new Dictionary<uint, AutoResetUniTaskCompletionSource<object>>();
        Dictionary<uint, Type> rpcType = new Dictionary<uint, Type>();
        Dictionary<uint, float> rpcTime = new Dictionary<uint, float>();
        List<uint> _rpcDels = new List<uint>();
        Dictionary<uint, Type> cmdType = new Dictionary<uint, Type>();
        Dictionary<uint, FastMethodInfo> cmdMethod = new Dictionary<uint, FastMethodInfo>();

        Action _connectCallback;
        Action<object> _connect1Callback;
        object _connect1Param;

        public ClientSocket(string address)
        {
            Address = address;
            sendStream = RecyclableMemoryStreamManager.Instance.GetStream("message", ushort.MaxValue);
            recvStream = RecyclableMemoryStreamManager.Instance.GetStream("message", ushort.MaxValue);
            this.parser = new PacketParser(this.recvBytes);
            this.IsConnected = false;
            this.IsSending = false;
            ModuleMgr.ForEach(mol =>
            {
                var methods = mol.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var method in methods)
                {
                    var net = method.GetCustomAttribute<NetAttribute>();
                    if (net != null)
                    {
                        var parames = method.GetParameters();

                        if (parames.Length > 1)
                        {
                            Log.Error($"网络接口{method.Name}：参数不能超过2个");
                            continue;
                        }
                        if (parames.Length == 1)
                        {
                            var p = parames[0].ParameterType;
                            cmdType.Add(net.cmd, p);
                        }
                        cmdMethod.Add(net.cmd, new FastMethodInfo(mol, method));
                    }
                }
            });
        }

        protected void OnSocketCode(SocketCode e)
        {
            if (!_activeDisconnect)
            {
                EventMgr.Ins.Run(MainEventType.NET_SOCKET_CODE, Address, e);
            }
            Dispose();
        }
        bool _activeDisconnect;
        public void Disconnect()
        {
            if (IsConnected)
            {
                _activeDisconnect = true;
                ToDisconnect();
            }
        }
        protected virtual void ToDisconnect()
        {

        }
        public void SetConnectCallback(Action connectCallback)
        {
            _connectCallback = connectCallback;
            _connect1Callback = null;
            _connect1Param = null;
        }
        public void SetConnectCallback(Action<object> connectCallback, object param)
        {
            _connect1Callback = connectCallback;
            _connect1Param = param;
            _connectCallback = null;
        }
        public void Connect()
        {
            if (string.IsNullOrEmpty(Address))
            {
                Log.Error("Socket 连接地址为空");
                return;
            }
            if (IsConnecting)
            {
                Log.Warning("Socket 正在连接中");
                return;
            }
            if (IsConnected)
            {
                Log.Warning("Socket 已连接");
                return;
            }

            IsConnecting = true;
            ToConnect(Address);
        }
        protected abstract void ToConnect(string address);
        protected void OnConnect()
        {
            IsConnecting = false;
            IsConnected = true;
            _connectCallback?.Invoke();
            _connect1Callback?.Invoke(_connect1Param);
            EventMgr.Ins.Run(MainEventType.NET_CONNECTED, Address);
        }
        public virtual void Update()
        {
            if (IsCheckUpdate)
            {
                if (isNeedSend && !IsSending)
                {
                    try
                    {
                        OnSend();
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                }
                if (IsConnected)
                {
                    if (!isNeedSend && !IsSending)
                    {
                        SendHeartbeat();
                    }
#if !UNITY_EDITOR
                    if (rpcTime.Count > 0)
                    {
                        foreach (var kv in rpcTime)
                        {
                            if (TimeMgr.Ins.TotalTime - kv.Value >= RPC_TimeOut)
                            {
                                _rpcDels.Add(kv.Key);
                            }
                        }
                        if (_rpcDels.Count > 0)
                        {
                            foreach (var rpcId in _rpcDels)
                            {
                                rpcType.Remove(rpcId);
                                if (rpcMethod.TryGetValue(rpcId, out var method))
                                {
                                    method.TrySetException(new Exception("RPC超时"));
                                    rpcMethod.Remove(rpcId);
                                }
                                rpcTime.Remove(rpcId);
                            }
                            _rpcDels.Clear();
                        }
                    }
#endif
                }
            }
        }

        #region 解析消息包

        protected bool OnParse()
        {
            LastRecvTime = TimeMgr.Ins.TotalTime;
            // 收到消息回调
            while (true)
            {
                try
                {
                    if (!this.parser.Parse())
                    {
                        break;
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                    return false;
                }

                var packetSize = this.parser.PacketSize();
                if (packetSize == 0)
                {
                    Log.Error("空包？");
                    break;
                }
                var opcode = (OpType)recvBytes.PopByte();
                packetSize -= 1;
                object message = null;
                switch (opcode)
                {
                    case OpType.H_B_R:
                        RecvHeartbeat();
                        break;
                    case OpType.C_S_C:
                        var cmd = recvBytes.PopUInt32();
                        packetSize -= 4;
                        message = ReadToMessage(FindType(cmd), packetSize);
                        Dispatch(cmd, message);
                        break;
                    case OpType.RPC_RESPONSE:
                        cmd = recvBytes.PopUInt32();
                        packetSize -= 4;
                        var rpcId = recvBytes.PopUInt32();
                        packetSize -= 4;
#if !UNITY_EDITOR
                        rpcTime.Remove(rpcId);
#endif
                        message = ReadToMessage(FindRPCType(rpcId), packetSize);
                        DispatchRPC(rpcId, message);
                        break;
                }
            }
            return true;
        }
        object ReadToMessage(Type type, int packetSize)
        {
            if (packetSize > 0)
            {
                if (type != null)
                {
                    recvBytes.PopToMemoryStream(recvStream, 0, packetSize);
                    return recvStream.ReadToMessage(type, 0);
                }
                //就算没注册对应的类型，也需要把剩余的数据去除，不然会一直缓存着
                recvBytes.PopTransferred(packetSize);
            }
            return null;
        }
        Type FindRPCType(uint rpcId)
        {
            if (rpcType.TryGetValue(rpcId, out var type))
            {
                rpcType.Remove(rpcId);
                return type;
            }
            return null;
        }
        void DispatchRPC(uint rpcId, object message)
        {
            if (rpcMethod.TryGetValue(rpcId, out var method))
            {
                try
                {
                    method.TrySetResult(message);
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
                rpcMethod.Remove(rpcId);
            }
        }
        Type FindType(uint cmd)
        {
            if (cmdType.TryGetValue(cmd, out var type))
            {
                return type;
            }
            return null;
        }

        void Dispatch(uint cmd, object message)
        {
            if (cmdMethod.TryGetValue(cmd, out var method))
            {
                try
                {
                    method.Invoke(message);
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
            }
        }
        #endregion

        #region 心跳
        protected virtual void SendHeartbeat()
        {
            if (this.IsDisposed)
            {
                return;
            }

            if (LastSendTime == 0 || TimeMgr.Ins.TotalTime - LastSendTime > heartTime)
            {
                this.sendBytes.PushUInt16(1);
                this.sendBytes.PushByte((byte)OpType.H_B_S);
                isNeedSend = true;
            }
        }
        protected virtual void RecvHeartbeat() { }
        #endregion


        #region 发送检测
        void OnSend()
        {
            this.IsSending = true;
            LastSendTime = TimeMgr.Ins.TotalTime;
            StartSend();
        }
        protected virtual void EndSend()
        {
            IsSending = false;
            // 没有数据需要发送
            if (this.sendBytes.Length == 0)
            {
                isNeedSend = false;
            }
        }
        protected abstract void StartSend();
        #endregion

        #region 网络请求
        public void Send(uint cmd, object message)
        {
            if (this.IsDisposed)
            {
                throw new Exception("Socket已经被Dispose了");
            }
            sendStream.WriteToMessage(message, 0);
            //因为前面WriteToMessage的时候Seek(0, SeekOrigin.Begin)，            
            //所以真实长度是Position而不是Length，Length是包含缓存部分的   

            var msgLen = 1 + 4 + sendStream.Position;
            this.sendBytes.PushUInt16((ushort)msgLen);
            this.sendBytes.PushByte((byte)OpType.C_S_C);
            this.sendBytes.PushUInt32(cmd);
            this.sendBytes.PushStream(sendStream);
            isNeedSend = true;
        }
        public UniTask<object> Call<TMessage>(uint cmd, object message)
        {
            if (this.IsDisposed)
            {
                throw new Exception("Socket已经被Dispose了");
            }
            var rpxID = GetRpxID();
            rpcType.Add(rpxID, typeof(TMessage));
            var task = AutoResetUniTaskCompletionSource<object>.Create();
            rpcMethod.Add(rpxID, task);

            sendStream.WriteToMessage(message, 0);
            //因为前面WriteToMessage的时候Seek(0, SeekOrigin.Begin)，            
            //所以真实长度是Position而不是Length，Length是包含缓存部分的   
            var msgLen = 1 + 4 + 4 + sendStream.Position;
            this.sendBytes.PushUInt16((ushort)msgLen);
            this.sendBytes.PushByte((byte)OpType.RPC_REQUIRE);
            this.sendBytes.PushUInt32(cmd);
            this.sendBytes.PushUInt32(rpxID);
            this.sendBytes.PushStream(sendStream);
            isNeedSend = true;
#if !UNITY_EDITOR
            rpcTime.Add(rpxID, TimeMgr.Ins.TotalTime);
#endif
            return task.Task;
        }
        #endregion

        public void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;
            IsConnected = false;
            IsSending = false;
            isNeedSend = false;
            _activeDisconnect = false;
            foreach (var kv in rpcMethod)
            {
                kv.Value.TrySetCanceled();
            }
            rpcTime.Clear();
            _rpcDels.Clear();
            rpcMethod.Clear();
            rpcMethod = null;
            rpcType.Clear();
            rpcType = null;
            cmdType.Clear();
            cmdType = null;
            cmdMethod.Clear();
            cmdMethod = null;
            sendStream.Dispose();
            recvStream.Dispose();
            sendBytes.Dispose();
            recvBytes.Dispose();
            Address = string.Empty;
            OnDispose();
            EventMgr.Ins.Run(MainEventType.NET_DISPOSE, this);
        }
        protected virtual void OnDispose() { }
    }
}
