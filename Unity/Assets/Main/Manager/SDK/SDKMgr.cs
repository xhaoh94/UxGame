using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    public class SDKMgr : Singleton<SDKMgr>
    {

        /// <summary>
        /// 事件监听字典：pluginName.eventName -> 事件Action列表
        /// </summary>
        private readonly Dictionary<string, List<Action<string>>> _eventListeners = new Dictionary<string, List<Action<string>>>();
        private readonly HashSet<string> _startupPlugins = new HashSet<string>();
        private readonly Dictionary<string, Queue<NativeEventMessage>> _cacel = new Dictionary<string, Queue<NativeEventMessage>>();
        public ISDK SDK { get; private set; }
        public void Init()
        {
            SDKBridge.OnSdkCallbackAction = OnSdkCallback;
#if UNITY_IOS && !UNITY_EDITOR
            SDK = new SDKIOS();
#elif UNITY_ANDROID && !UNITY_EDITOR
            SDK = new SDKAndroid();
#else
            SDK = new SDKUx();
#endif

            SDK.Init();
        }

        public void Startup(string pluginName)
        {
            if (_startupPlugins.Contains(pluginName))
            {
                return;
            }
            _startupPlugins.Add(pluginName);
            if (_cacel.TryGetValue(pluginName, out var queue))
            {
                while (queue.Count > 0)
                {
                    OnMessage(queue.Dequeue());
                }
            }
        }
        // ==================== 事件监听 ====================

        /// <summary>
        /// 注册事件监听器
        /// </summary>
        /// <param name="pluginName">插件名称</param>
        /// <param name="eventName">事件名称</param>
        /// <param name="listener">监听回调</param>
        public void AddEventListener(string pluginName, string eventName, Action<string> listener)
        {
            string key = $"{pluginName}.{eventName}";
            if (!_eventListeners.ContainsKey(key))
            {
                _eventListeners[key] = new List<Action<string>>();
            }
            _eventListeners[key].Add(listener);
        }

        /// <summary>
        /// 移除事件监听器
        /// </summary>
        public void RemoveEventListener(string pluginName, string eventName, Action<string> listener)
        {
            string key = $"{pluginName}.{eventName}";
            if (_eventListeners.ContainsKey(key))
            {
                _eventListeners[key].Remove(listener);
            }
        }

        /// <summary>
        /// 接收原生侧的事件通知（主动推送）    
        /// </summary>
        private void OnSdkCallback(string message)
        {
            Log.Debug($"[SDKBridge] 收到事件: {message}");
            try
            {
                var json = JsonUtility.FromJson<NativeEventMessage>(message);
                OnMessage(json);
            }
            catch (Exception e)
            {
                Log.Error($"[SDKBridge] 解析事件失败: {e.Message}\n原始消息: {message}");
            }
        }
        void OnMessage(NativeEventMessage json)
        {
            if (json != null)
            {
                if (!_startupPlugins.Contains(json.plugin))
                {
                    if (!_cacel.TryGetValue(json.plugin, out var queue))
                    {
                        queue = new Queue<NativeEventMessage>();
                    }
                    queue.Enqueue(json);
                    return;
                }

                string key = $"{json.plugin}.{json.eventName}";
                if (_eventListeners.TryGetValue(key, out List<Action<string>> listeners))
                {
                    foreach (var listener in listeners)
                    {
                        listener?.Invoke(json.data);
                    }
                }
            }
        }

        // ==================== 数据结构 ====================
        [Serializable]
        private class NativeEventMessage
        {
            public string plugin;
            public string eventName;
            public string data;
        }

    }
}