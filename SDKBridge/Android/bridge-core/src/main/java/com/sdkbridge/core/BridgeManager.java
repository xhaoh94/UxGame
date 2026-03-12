package com.sdkbridge.core;

import android.app.Application;
import android.app.Activity;
import android.content.Intent;
import android.os.Bundle;
import android.util.Log;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;

import com.sdkbridge.core.callback.BridgeCallback;
import com.sdkbridge.core.plugin.IBridgePlugin;

import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;

/**
 * 桥接管理器 - 整个框架的核心入口
 * <p>
 * 职责：
 * 1. 管理所有已注册的桥接插件
 * 2. 路由Unity侧的调用到具体插件
 * 3. 分发Activity生命周期事件给各插件
 * </p>
 * <p>
 * 使用方式（在自定义Application或Unity的主Activity中初始化）：
 * <pre>
 *   BridgeManager.getInstance().init(activity);
 *   BridgeManager.getInstance().registerPlugin(new MySDKPlugin());
 * </pre>
 * </p>
 */
public class BridgeManager {

    private static final String TAG = "BridgeManager";

    private static volatile BridgeManager instance;

    /** 已注册的插件映射表：pluginName -> 插件实例 */
    private final Map<String, IBridgePlugin> pluginMap = new ConcurrentHashMap<>();

    /** 当前Activity引用 */
    private Activity activity;

    /** 当前Application引用 */
    private Application application;

    /** 是否已初始化 */
    private boolean initialized = false;

    private BridgeManager() {}

    public static BridgeManager getInstance() {
        if (instance == null) {
            synchronized (BridgeManager.class) {
                if (instance == null) {
                    instance = new BridgeManager();
                }
            }
        }
        return instance;
    }

    // ==================== 初始化 ====================

    /**
     * 初始化桥接管理器
     *
     * @param activity Unity的主Activity
     */
    public void init(@NonNull Activity activity) {
        this.activity = activity;
        this.initialized = true;
        Log.i(TAG, "BridgeManager 初始化完成");
    }

    /**
     * 设置Unity回调接收器的GameObject名称
     */
    public void setUnityReceiverName(@NonNull String name) {
        BridgeCallback.setReceiverObjectName(name);
    }

    /**
     * 设置Unity回调接收器的回调名称
     */
    public void setUnityReceiverMethod(@NonNull String name) {
        BridgeCallback.setReceiverMethod(name);
    }

    // ==================== 插件注册 ====================

    /**
     * 注册插件
     *
     * @param plugin 插件实例
     */
    public void registerPlugin(@NonNull IBridgePlugin plugin) {
        registerPlugin(plugin, null);
    }

    /**
     * 注册插件并初始化
     *
     * @param plugin 插件实例
     * @param params 初始化参数
     */
    public void registerPlugin(@NonNull IBridgePlugin plugin, @Nullable Bundle params) {
        String name = plugin.getPluginName();
        if (pluginMap.containsKey(name)) {
            Log.w(TAG, "插件已注册，将覆盖: " + name);
        }
        pluginMap.put(name, plugin);
        Log.i(TAG, "注册插件: " + name);

        if (this.application != null) {
            try {
                plugin.onApplicationCreate(this.application);
            } catch (Exception e) {
                Log.e(TAG, "插件 onApplicationCreate 异常: " + name, e);
            }
        }

        if (this.activity != null) {
            try {
                plugin.onActivityCreate(this.activity, null);
            } catch (Exception e) {
                Log.e(TAG, "插件 onActivityCreate 异常: " + name, e);
            }
        }

        if (activity != null) {
            try {
                plugin.onInit(activity, params);
                Log.i(TAG, "插件初始化完成: " + name);
            } catch (Exception e) {
                Log.e(TAG, "插件初始化失败: " + name, e);
            }
        }
    }

    /**
     * 注销插件
     */
    public void unregisterPlugin(@NonNull String pluginName) {
        IBridgePlugin removed = pluginMap.remove(pluginName);
        if (removed != null) {
            Log.i(TAG, "注销插件: " + pluginName);
        }
    }

    /**
     * 获取已注册的插件
     */
    @Nullable
    public IBridgePlugin getPlugin(@NonNull String pluginName) {
        return pluginMap.get(pluginName);
    }

    /**
     * 检查插件是否已注册
     */
    public boolean hasPlugin(@NonNull String pluginName) {
        return pluginMap.containsKey(pluginName);
    }

    // ==================== 核心调用入口（供Unity侧调用） ====================

    /**
     * Unity侧调用Android原生方法的统一入口
     * <p>
     * Unity C# 通过 AndroidJavaObject 调用此方法
     * </p>
     *
     * @param pluginName 插件名称
     * @param methodName 方法名
     * @param jsonParams JSON参数
     * @return JSON格式的返回结果
     */
    @Nullable
    public String call(@NonNull String pluginName,
                       @NonNull String methodName,
                       @Nullable String jsonParams) {
        IBridgePlugin plugin = pluginMap.get(pluginName);
        if (plugin == null) {
            String error = "{\"code\":-1,\"msg\":\"插件未找到: " + pluginName + "\"}";
            Log.e(TAG, error);
            return error;
        }

        try {
            Log.d(TAG, "调用: " + pluginName + "." + methodName + " 参数: " + jsonParams);
            String result = plugin.execute(methodName, jsonParams);
            Log.d(TAG, "返回: " + result);
            return result;
        } catch (Exception e) {
            String error = "{\"code\":-2,\"msg\":\"执行异常: " + e.getMessage() + "\"}";
            Log.e(TAG, "插件执行异常: " + pluginName + "." + methodName, e);
            return error;
        }
    }

    // ==================== 生命周期分发 ====================

    public void onApplicationCreate(@NonNull Application app) {
        this.application = app;
        for (IBridgePlugin plugin : pluginMap.values()) {
            try {
                plugin.onApplicationCreate(app);
            } catch (Exception e) {
                Log.e(TAG, "onApplicationCreate 异常: " + plugin.getPluginName(), e);
            }
        }
    }

    public void onActivityCreate(@NonNull Activity activity, @Nullable Bundle savedInstanceState) {
        this.activity = activity;
        for (IBridgePlugin plugin : pluginMap.values()) {
            try {
                plugin.onActivityCreate(activity, savedInstanceState);
            } catch (Exception e) {
                Log.e(TAG, "onActivityCreate 异常: " + plugin.getPluginName(), e);
            }
        }
    }

    public void onActivityResult(int requestCode, int resultCode, @Nullable Intent data) {
        for (IBridgePlugin plugin : pluginMap.values()) {
            try {
                plugin.onActivityResult(requestCode, resultCode, data);
            } catch (Exception e) {
                Log.e(TAG, "onActivityResult 异常: " + plugin.getPluginName(), e);
            }
        }
    }

    public void onResume() {
        for (IBridgePlugin plugin : pluginMap.values()) {
            try {
                plugin.onResume();
            } catch (Exception e) {
                Log.e(TAG, "onResume 异常: " + plugin.getPluginName(), e);
            }
        }
    }

    public void onPause() {
        for (IBridgePlugin plugin : pluginMap.values()) {
            try {
                plugin.onPause();
            } catch (Exception e) {
                Log.e(TAG, "onPause 异常: " + plugin.getPluginName(), e);
            }
        }
    }

    public void onDestroy() {
        for (IBridgePlugin plugin : pluginMap.values()) {
            try {
                plugin.onDestroy();
            } catch (Exception e) {
                Log.e(TAG, "onDestroy 异常: " + plugin.getPluginName(), e);
            }
        }
        pluginMap.clear();
        activity = null;
        initialized = false;
    }

    public void onNewIntent(@Nullable Intent intent) {
        for (IBridgePlugin plugin : pluginMap.values()) {
            try {
                plugin.onNewIntent(intent);
            } catch (Exception e) {
                Log.e(TAG, "onNewIntent 异常: " + plugin.getPluginName(), e);
            }
        }
    }

    public void onRequestPermissionsResult(int requestCode,
                                           @NonNull String[] permissions,
                                           @NonNull int[] grantResults) {
        for (IBridgePlugin plugin : pluginMap.values()) {
            try {
                plugin.onRequestPermissionsResult(requestCode, permissions, grantResults);
            } catch (Exception e) {
                Log.e(TAG, "onRequestPermissionsResult 异常: " + plugin.getPluginName(), e);
            }
        }
    }

    @NonNull
    public Activity getActivity() {
        return activity;
    }

    public boolean isInitialized() {
        return initialized;
    }
}
