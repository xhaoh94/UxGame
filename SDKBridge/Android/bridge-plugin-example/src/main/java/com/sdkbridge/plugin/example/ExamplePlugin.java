package com.sdkbridge.plugin.example;

import android.app.Application;
import android.app.Activity;
import android.os.Bundle;
import android.util.Log;
import android.widget.Toast;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;

import com.sdkbridge.core.plugin.BaseBridgePlugin;
import com.sdkbridge.core.util.JsonHelper;

/**
 * 示例插件 - 演示如何编写一个桥接插件
 * <p>
 * 这个示例展示了完整的插件结构，包括：
 * 1. 插件初始化
 * 2. 同步方法调用
 * 3. 异步方法调用（通过事件主动推送结果）
 * 实际接入第三方SDK时，照此模式替换具体SDK调用即可。
 * </p>
 */
public class ExamplePlugin extends BaseBridgePlugin {

    private static final String TAG = "ExamplePlugin";

    /** 插件名称 - Unity侧通过此名称调用 */
    private static final String PLUGIN_NAME = "Example";

    private boolean isInitialized = false;

    @NonNull
    @Override
    public String getPluginName() {
        return PLUGIN_NAME;
    }

    @Override
    public void onInit(@NonNull Activity activity, @Nullable Bundle params) {
        super.onInit(activity, params);

        // 从参数中读取配置（示例）
        String appKey = null;
        if (params != null) {
            String json = params.getString("json");
            appKey = JsonHelper.getString(json, "appKey");
        }

        Log.i(TAG, "ExamplePlugin 初始化, appKey=" + appKey);
        isInitialized = true;
    }

    @Nullable
    @Override
    public String execute(@NonNull String methodName, @Nullable String jsonParams) {
        Log.d(TAG, "执行方法: " + methodName + ", 参数: " + jsonParams);

        switch (methodName) {
            case "showToast":
                return handleShowToast(jsonParams);

            case "getDeviceInfo":
                return handleGetDeviceInfo();

            case "doAsyncWork":
                return handleAsyncWork(jsonParams);

            case "isInitialized":
                return successResult(String.valueOf(isInitialized));

            default:
                return errorResult(-1, "未知方法: " + methodName);
        }
    }

    // ==================== 方法实现示例 ====================

    /**
     * 同步方法示例 - 显示Toast
     */
    private String handleShowToast(@Nullable String jsonParams) {
        String message = JsonHelper.getString(jsonParams, "message");
        if (message == null) {
            return errorResult(1, "缺少message参数");
        }

        runOnUiThread(() -> Toast.makeText(activity, message, Toast.LENGTH_SHORT).show());
        return successResult(null);
    }

    /**
     * 同步方法示例 - 获取设备信息
     */
    private String handleGetDeviceInfo() {
        String info = "{" +
                "\"brand\":\"" + android.os.Build.BRAND + "\"," +
                "\"model\":\"" + android.os.Build.MODEL + "\"," +
                "\"sdk\":" + android.os.Build.VERSION.SDK_INT + "," +
                "\"version\":\"" + android.os.Build.VERSION.RELEASE + "\"" +
                "}";
        return successResult(info);
    }

    /**
     * 异步方法示例 - 模拟耗时操作后通过事件返回结果
     */
    private String handleAsyncWork(@Nullable String jsonParams) {
        int delayMs = JsonHelper.getInt(jsonParams, "delay", 2000);

        // 模拟异步操作
        new Thread(() -> {
            try {
                Thread.sleep(delayMs);
                // 异步完成后，通过事件推送结果到Unity
                sendEvent("onAsyncWorkDone", "{\"result\":\"异步任务完成\",\"timestamp\":" + System.currentTimeMillis() + "}");
            } catch (InterruptedException e) {
                sendEvent("onAsyncWorkDone", "{\"error\":\"任务被中断\"}");
            }
        }).start();

        // 同步返回"已接受"
        return successResult("{\"status\":\"accepted\"}");
    }

    // ==================== 生命周期 ====================

    @Override
    public void onApplicationCreate(@NonNull Application app) {
        Log.i(TAG, "ExamplePlugin onApplicationCreate called");
    }

    @Override
    public void onResume() {
        Log.d(TAG, "onResume");
        // 在这里恢复SDK状态（如广告SDK的恢复等）
    }

    @Override
    public void onPause() {
        Log.d(TAG, "onPause");
    }

    @Override
    public void onDestroy() {
        Log.d(TAG, "onDestroy");
        isInitialized = false;
        // 在这里释放SDK资源
    }
}
