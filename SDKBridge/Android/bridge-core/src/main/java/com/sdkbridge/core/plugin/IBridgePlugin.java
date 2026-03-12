package com.sdkbridge.core.plugin;

import android.app.Application;
import android.app.Activity;
import android.content.Intent;
import android.os.Bundle;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;

/**
 * 桥接插件接口 - 所有第三方SDK插件都必须实现此接口
 * <p>
 * 每个插件对应一个第三方SDK的桥接封装，通过实现此接口，
 * 将SDK的功能以统一的方式暴露给Unity侧调用。
 * </p>
 */
public interface IBridgePlugin {

    /**
     * 获取插件名称，必须全局唯一
     * Unity侧通过此名称来路由调用到具体插件
     *
     * @return 插件唯一标识名称，例如 "AdMob"、"WeChat"、"Firebase"
     */
    @NonNull
    String getPluginName();

    /**
     * 插件初始化，在插件注册到管理器后调用
     *
     * @param activity 当前Activity（Unity的主Activity）
     * @param params   初始化参数（从Unity侧传递的JSON字符串解析而来）
     */
    void onInit(@NonNull Activity activity, @Nullable Bundle params);

    /**
     * 执行插件方法 - 核心调用入口
     * Unity侧通过 pluginName + methodName + jsonParams 路由到此方法
     *
     * @param methodName 方法名，例如 "showAd"、"login"、"pay"
     * @param jsonParams JSON格式的参数字符串
     * @return JSON格式的同步返回结果，异步结果通过回调返回
     */
    @Nullable
    String execute(@NonNull String methodName, @Nullable String jsonParams);

    /**
     * Application生命周期回调
     */
    default void onApplicationCreate(@NonNull Application app) {}

    /**
     * Activity生命周期回调 - 以下方法按需覆写
     */
    default void onActivityCreate(@NonNull Activity activity, @Nullable Bundle savedInstanceState) {}

    default void onActivityResult(int requestCode, int resultCode, @Nullable Intent data) {}

    default void onResume() {}

    default void onPause() {}

    default void onDestroy() {}

    default void onNewIntent(@Nullable Intent intent) {}

    default void onRequestPermissionsResult(int requestCode,
                                            @NonNull String[] permissions,
                                            @NonNull int[] grantResults) {}
}
