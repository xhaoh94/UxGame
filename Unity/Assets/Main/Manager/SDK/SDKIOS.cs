using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Ux
{
    public class SDKIOS : SDKBase
    {
        [DllImport("__Internal")]
        private static extern void OC_Init(string receiverName, string receiverMethod);
        [DllImport("__Internal")]
        private static extern bool OC_RegisterPlugin(string pluginClassName, string jsonParams);
        [DllImport("__Internal")]
        private static extern bool OC_UnRegisterPlugin(string pluginClassName);
        [DllImport("__Internal")]
        private static extern bool OC_HasPlugin(string pluginName);
        [DllImport("__Internal")]
        private static extern IntPtr OC_Call(string pluginName, string methodName, string jsonParams);

        [DllImport("__Internal")]
        private static extern void OC_FreeString(IntPtr ptr);

        public override void Init()
        {
           new GameObject().AddComponent<SDKBridge>();
            OC_Init(SDKBridge.ReceiverObject, SDKBridge.ReceiverMethod);
            Debug.Log("[SDKIOS] 初始化完成");
        }
        public override string Call(string pluginName, string methodName, string jsonParams = null)
        {
            IntPtr ptr = OC_Call(pluginName, methodName, jsonParams);
            if (ptr == IntPtr.Zero)
            {
                return string.Empty;
            }

            // 在 iOS/Unix 平台，PtrToStringAnsi 会按 UTF-8 解码原生字符串
            string result = Marshal.PtrToStringAnsi(ptr);
            
            // 释放原生层 malloc 的内存，防止内存泄漏
            OC_FreeString(ptr);
            
            return result;
        }

        public override bool HasPlugin(string pluginName)
        {
            return OC_HasPlugin(pluginName);
        }

        public override bool RegisterPlugin(string pluginClassName, string jsonParams = null)
        {
            bool result = OC_RegisterPlugin(pluginClassName, jsonParams);
            Debug.Log($"[SDKIOS] 注册插件 {pluginClassName}: {(result ? "成功" : "失败")}");
            return result;
        }
        public override bool UnRegisterPlugin(string pluginClassName)
        {
            bool result = OC_UnRegisterPlugin(pluginClassName);
            Debug.Log($"[SDKIOS] 注销插件 {pluginClassName}: {(result ? "成功" : "失败")}");
            return result;
        }
    }
}