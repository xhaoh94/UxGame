package com.sdkbridge.core.callback;

import android.util.Log;

import androidx.annotation.NonNull;

/**
 * Unity回调通信层
 * <p>
 * 负责Android侧向Unity侧发送消息，使用Unity的UnitySendMessage机制。
 * 所有回调数据统一为JSON格式，Unity侧有对应的接收器对象。
 * </p>
 */
public class BridgeCallback {

    private static final String TAG = "BridgeCallback";

    /**
     * Unity侧接收消息的GameObject名称
     * 必须与Unity场景中挂载 SDKBridge.cs 的 GameObject 名称一致
     */
    private static String unityReceiverObject = "SDKBridgeReceiver";

    /**
     * Unity侧接收事件的方法名
     */
    private static String unityReceiverMethod = "OnSdkCallback";

    /**
     * 设置Unity接收器的GameObject名称（可在初始化时自定义）
     */
    public static void setReceiverObjectName(@NonNull String name) {
        unityReceiverObject = name;
    }
    public static void setReceiverMethod(@NonNull String name) {
        unityReceiverMethod = name;
    }


    /**
     * 向Unity发送事件通知（无callbackId，用于主动推送事件）
     *
     * @param pluginName 插件名称
     * @param eventName  事件名称
     * @param jsonData   JSON数据
     */
    public static void sendEventToUnity(@NonNull String pluginName,
                                        @NonNull String eventName,
                                        @NonNull String jsonData) {
        String message = buildEventMessage(pluginName, eventName, jsonData);
        sendMessage(unityReceiverMethod, message);
    }

    /**
     * 通过反射调用 UnityPlayer.UnitySendMessage
     * 使用反射是为了 bridge-core 不需要在编译时强依赖 unity-classes.jar
     */
    private static void sendMessage(@NonNull String method, @NonNull String message) {
        try {
            Class<?> unityPlayerClass = Class.forName("com.unity3d.player.UnityPlayer");
            java.lang.reflect.Method sendMethod = unityPlayerClass.getMethod(
                    "UnitySendMessage", String.class, String.class, String.class);
            sendMethod.invoke(null, unityReceiverObject, method, message);
            Log.d(TAG, "发送到Unity: [" + method + "] " + message);
        } catch (Exception e) {
            // 非Unity环境下（如独立测试），打印日志但不崩溃
            Log.w(TAG, "UnitySendMessage 调用失败（可能不在Unity环境中）: " + e.getMessage());
            Log.d(TAG, "消息内容: [" + method + "] " + message);
        }
    }


    /**
     * 构建事件消息JSON
     * 格式: {"plugin":"xxx","event":"xxx","data":{...}}
     */
    private static String buildEventMessage(@NonNull String pluginName,
                                            @NonNull String eventName,
                                            @NonNull String jsonData) {
        return "{\"plugin\":\"" + pluginName
                + "\",\"eventName\":\"" + eventName
                + "\",\"data\":" + jsonData + "}";
    }
}
