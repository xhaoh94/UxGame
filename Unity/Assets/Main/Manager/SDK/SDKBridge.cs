using System;
using System.Collections.Generic;
using UnityEditor.UI;
using UnityEngine;
using Ux;

/// <summary>
/// Unity侧原生桥接管理器
/// 
public class SDKBridge : MonoBehaviour
{
    public static readonly string ReceiverObject = "SDKBridgeReceiver";
    public static readonly string ReceiverMethod = "OnSdkCallback";
    public static Action<string> OnSdkCallbackAction;
    private void Awake()
    {
        gameObject.name = ReceiverObject;
        DontDestroyOnLoad(gameObject);
    }


    /// <summary>
    /// 接收原生侧的事件通知（主动推送）    
    /// </summary>
    private void OnSdkCallback(string message)
    {
        OnSdkCallbackAction?.Invoke(message);        
    }


}
