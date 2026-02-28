package com.uxgame.core;

import android.app.Application;
import android.util.Log;
import com.sdkbridge.core.BridgeManager;

/**
 * 自定义 Application 入口 (中心化路由)
 *
 * 职责：仅负责拦截 Android 底层的生命周期事件，并将其转发给 BridgeManager。
 * 绝对不要在这里编写任何第三方 SDK 的初始化代码。
 * 所有的第三方 SDK 业务应该在外部的 Plugin 工程中实现。
 */
public class MyApplication extends Application {

    private static final String TAG = "MyApplication";

    @Override
    public void onCreate() {
        super.onCreate();
        Log.d(TAG, "Application onCreate 开始，向桥接层分发事件");

        // 将 Application 上下文分发给所有已通过 ContentProvider 注册的底层插件
        BridgeManager.getInstance().onApplicationCreate(this);

        Log.d(TAG, "Application onCreate 完成");
    }

    @Override
    public void onTerminate() {
        super.onTerminate();
        Log.d(TAG, "Application onTerminate");
    }

    @Override
    public void onLowMemory() {
        super.onLowMemory();
        Log.w(TAG, "Application onLowMemory");
    }
}
