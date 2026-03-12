package com.sdkbridge.core.plugin;

import android.app.Activity;
import android.os.Bundle;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;

import com.sdkbridge.core.callback.BridgeCallback;
import com.sdkbridge.core.BridgeManager;

/**
 * 插件基类 - 提供常用辅助方法，推荐所有插件继承此类
 * <p>
 * 相比直接实现 IBridgePlugin，继承此基类可以获得：
 * 1. Activity 引用的自动保存
 * 2. 向Unity发送事件的便捷方法
 * 3. 日志辅助方法
 * </p>
 */
public abstract class BaseBridgePlugin implements IBridgePlugin {

    protected Activity activity;

    @Override
    public void onActivityCreate(@NonNull Activity activity, @Nullable Bundle savedInstanceState) {
        this.activity = activity;
    }

    @Override
    public void onInit(@NonNull Activity activity, @Nullable Bundle params) {
        this.activity = activity;
    }


    /**
     * 向Unity发送事件通知（无需callbackId，用于主动推送）
     *
     * @param eventName 事件名称
     * @param jsonData  JSON格式的事件数据
     */
    protected void sendEvent(@NonNull String eventName, @NonNull String jsonData) {
        BridgeCallback.sendEventToUnity(getPluginName(), eventName, jsonData);
    }

    /**
     * 在主线程上执行操作
     */
    protected void runOnUiThread(@NonNull Runnable runnable) {
        if (activity != null) {
            activity.runOnUiThread(runnable);
        }
    }

    /**
     * 构建成功响应JSON
     */
    protected String successResult(@Nullable String data) {
        if (data == null) {
            return "{\"code\":0,\"msg\":\"success\"}";
        }
        return "{\"code\":0,\"msg\":\"success\",\"data\":" + data + "}";
    }

    /**
     * 构建失败响应JSON
     */
    protected String errorResult(int code, @NonNull String msg) {
        return "{\"code\":" + code + ",\"msg\":\"" + msg + "\"}";
    }
}
