package com.sdkbridge.core;

import android.app.Application;
import android.app.Activity;
import android.content.Intent;
import android.os.Bundle;
import android.util.Log;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;

import com.sdkbridge.core.plugin.IBridgePlugin;

/**
 * Unity侧直接调用的静态入口类
 * <p>
 * Unity C# 通过 AndroidJavaClass 调用这些静态方法，
 * 避免Unity侧需要持有Java对象的引用。
 * </p>
 * <p>
 * Unity C# 调用示例：
 * <pre>
 *   AndroidJavaClass bridge = new AndroidJavaClass("com.sdkbridge.core.UnityBridge");
 *   string result = bridge.CallStatic&lt;string&gt;("call", "PluginName", "methodName", jsonParams);
 * </pre>
 * </p>
 */
public class UnityBridge {

    private static final String TAG = "UnityBridge";

    /**
     * 初始化桥接框架（Unity侧在启动时调用）
     *
     * @param activityObj Unity传入的当前Activity对象
     */
    public static void init(Object activityObj) {
        if (activityObj instanceof Activity) {
            BridgeManager.getInstance().init((Activity) activityObj);
            Log.i(TAG, "UnityBridge 初始化完成");
        } else {
            Log.e(TAG, "init 参数不是 Activity 类型");
        }
    }

    /**
     * 初始化桥接框架，并指定Unity回调接收器名称
     */
    public static void init(Object activityObj, String receiverName, String receiverMethod) {
        init(activityObj);
        if (receiverName != null && !receiverName.isEmpty()) {
            BridgeManager.getInstance().setUnityReceiverName(receiverName);
        }
        if (receiverMethod != null && !receiverMethod.isEmpty()) {
            BridgeManager.getInstance().setUnityReceiverMethod(receiverMethod);
        }
    }

    /**
     * 调用插件方法 - Unity侧的统一调用入口
     *
     * @param pluginName 插件名称
     * @param methodName 方法名
     * @param jsonParams JSON参数字符串
     * @return JSON格式的返回结果
     */
    public static String call(String pluginName, String methodName, String jsonParams) {
        return BridgeManager.getInstance().call(pluginName, methodName, jsonParams);
    }

    /**
     * 注册插件（通过完整类名反射实例化）
     * 供Unity侧动态注册插件使用
     *
     * @param pluginClassName 插件完整类名，例如 "com.sdkbridge.plugin.example.ExamplePlugin"
     * @return 是否注册成功
     */
    public static boolean registerPlugin(String pluginClassName) {
        return registerPlugin(pluginClassName, null);
    }

    /**
     * 注册插件（通过完整类名反射实例化，带初始化参数）
     *
     * @param pluginClassName 插件完整类名
     * @param jsonParams      JSON格式的初始化参数
     * @return 是否注册成功
     */
    public static boolean registerPlugin(String pluginClassName, String jsonParams) {
        try {
            Class<?> clazz = Class.forName(pluginClassName);
            Object obj = clazz.newInstance();
            if (obj instanceof IBridgePlugin) {
                Bundle params = null;
                if (jsonParams != null && !jsonParams.isEmpty()) {
                    params = new Bundle();
                    params.putString("json", jsonParams);
                }
                BridgeManager.getInstance().registerPlugin((IBridgePlugin) obj, params);
                return true;
            } else {
                Log.e(TAG, pluginClassName + " 未实现 IBridgePlugin 接口");
                return false;
            }
        } catch (ClassNotFoundException e) {
            Log.e(TAG, "插件类未找到: " + pluginClassName, e);
            return false;
        } catch (Exception e) {
            Log.e(TAG, "注册插件失败: " + pluginClassName, e);
            return false;
        }
    }

    /**
     * 检查插件是否已注册
     */
    public static boolean hasPlugin(String pluginName) {
        return BridgeManager.getInstance().hasPlugin(pluginName);
    }

    /**
     * 注销插件
     */
    public static void unregisterPlugin(String pluginName) {
        BridgeManager.getInstance().unregisterPlugin(pluginName);
    }

    // ==================== 生命周期转发（在自定义Activity中调用） ====================

    public static void onApplicationCreate(Application app) {
        BridgeManager.getInstance().onApplicationCreate(app);
    }

    public static void onActivityCreate(Activity activity, Bundle savedInstanceState) {
        BridgeManager.getInstance().onActivityCreate(activity, savedInstanceState);
    }

    public static void onActivityResult(int requestCode, int resultCode, Intent data) {
        BridgeManager.getInstance().onActivityResult(requestCode, resultCode, data);
    }

    public static void onResume() {
        BridgeManager.getInstance().onResume();
    }

    public static void onPause() {
        BridgeManager.getInstance().onPause();
    }

    public static void onDestroy() {
        BridgeManager.getInstance().onDestroy();
    }

    public static void onNewIntent(Intent intent) {
        BridgeManager.getInstance().onNewIntent(intent);
    }

    public static void onRequestPermissionsResult(int requestCode,
                                                  String[] permissions,
                                                  int[] grantResults) {
        BridgeManager.getInstance().onRequestPermissionsResult(requestCode, permissions, grantResults);
    }
}
