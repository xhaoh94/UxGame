using System;
using UnityEngine;
namespace Ux
{
    /// <summary>
    /// 全局低频回调入口。
    ///
    /// 帧驱动（Update/LateUpdate/FixedUpdate/Quit）已经迁到 <see cref="FrameDriver"/>：
    /// 想参与每帧驱动必须实现 IFrameUpdate / IFrameLateUpdate / IFrameFixedUpdate / IFrameQuit 接口，
    /// 不再提供「任意委托订阅全局帧事件」的入口。
    /// </summary>
    public static class GameMethod
    {
        /// <summary>低内存回调（系统内存告警，低频，允许各模块订阅）。</summary>
        public static event Action LowMemory;

        public static void TickLowMemory() => LowMemory?.Invoke();
    }

    public class Global
    {
        public static int DownloadingMaxNum => 10;
        public static int FailedTryAgain => 3;
        public static string GetHostServerURL()
        {
            string hostServerIP = "http://127.0.0.1:0709";
            string runtimePlatform = GetRuntimePlatform();

            return $"{hostServerIP}/{runtimePlatform}";
        }
        public static string GetFallbackHostServerURL()
        {
            string hostServerIP = "http://127.0.0.1:0709";
            string runtimePlatform = GetRuntimePlatform();

            return $"{hostServerIP}/{runtimePlatform}";
        }

        static string GetRuntimePlatform()
        {
            string runtimePlatform = "StandaloneWindows64";
#if UNITY_EDITOR
            runtimePlatform = UnityEditor.EditorUserBuildSettings.activeBuildTarget.ToString();
#else
        switch (Application.platform)
        {            
            case RuntimePlatform.Android:
                runtimePlatform = "Android";
                break;
            case RuntimePlatform.IPhonePlayer:
                runtimePlatform = "iOS";
                break;
        }
#endif
            return runtimePlatform;
        }

    }
}

