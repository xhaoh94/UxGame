using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    public class SDKAndroid : SDKBase
    {
        private const string BRIDGE_CLASS = "com.sdkbridge.core.UnityBridge";

        /// <summary>
        /// 事件监听字典：pluginName.eventName -> 事件Action列表
        /// </summary>
        private static readonly Dictionary<string, List<Action<string>>> _eventListeners = new Dictionary<string, List<Action<string>>>();


        private static AndroidJavaClass _bridgeClass;
        private static AndroidJavaClass GetBridgeClass()
        {
            if (_bridgeClass == null)
            {
                _bridgeClass = new AndroidJavaClass(BRIDGE_CLASS);
            }
            return _bridgeClass;
        }
        public override void Init()
        {
            // Android SDK 初始化
            new GameObject().AddComponent<SDKBridge>();

            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                GetBridgeClass().CallStatic("init", activity, SDKBridge.ReceiverObject,SDKBridge.ReceiverMethod);
            }
            Debug.Log("[SDKAndroid] 初始化完成");
        }
        /// <summary>
        /// 通过类名注册插件
        /// </summary>
        /// <param name="pluginClassName">插件完整Java类名，例如 "com.sdkbridge.plugin.example.ExamplePlugin"</param>
        /// <param name="jsonParams">初始化参数JSON（可选）</param>
        /// <returns>是否注册成功</returns>
        public override bool RegisterPlugin(string pluginClassName, string jsonParams = null)
        {
            bool result = GetBridgeClass().CallStatic<bool>("registerPlugin", pluginClassName, jsonParams);
            Debug.Log($"[SDKAndroid] 注册插件 {pluginClassName}: {(result ? "成功" : "失败")}");
            return result;
        }

        public override bool UnRegisterPlugin(string pluginClassName)
        {
            bool result = GetBridgeClass().CallStatic<bool>("unregisterPlugin", pluginClassName);
            Debug.Log($"[SDKAndroid] 注销插件 {pluginClassName}: {(result ? "成功" : "失败")}");
            return result;
        }
        /// <summary>
        /// 检查插件是否已注册
        /// </summary>
        public override bool HasPlugin(string pluginName)
        {
            return GetBridgeClass().CallStatic<bool>("hasPlugin", pluginName);
        }        

        /// <summary>
        /// 调用原生插件方法（同步）
        /// </summary>
        /// <param name="pluginName">插件名称</param>
        /// <param name="methodName">方法名</param>
        /// <param name="jsonParams">JSON参数（可选）</param>
        /// <returns>JSON格式的返回结果</returns>
        public override string Call(string pluginName, string methodName, string jsonParams = null)
        {
            string result = GetBridgeClass().CallStatic<string>("call", pluginName, methodName, jsonParams);
            Debug.Log($"[SDKAndroid] {pluginName}.{methodName} => {result}");
            return result;
        }         
    }
}