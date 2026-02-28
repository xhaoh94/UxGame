package com.uxgame.core;

import android.app.Application;
import android.util.Log;

/**
 * 自定义 Application 入口
 *
 * 用于在应用启动时执行原生 SDK 的初始化工作。
 * Unity 会通过 AndroidManifest.xml 中的 android:name 属性加载此类。
 *
 * 使用方法：
 *   在 onCreate() 中按需添加各 SDK 的初始化代码。
 */
public class MyApplication extends Application {

    private static final String TAG = "MyApplication";

    @Override
    public void onCreate() {
        super.onCreate();
        Log.d(TAG, "Application onCreate 开始");

        // TODO: 在此处初始化第三方 SDK
        // 示例：
        // ThirdPartySDK.init(this, "your-app-key");

        Log.d(TAG, "Application onCreate 完成");
    }

    @Override
    public void onTerminate() {
        super.onTerminate();
        Log.d(TAG, "Application onTerminate");

        // TODO: 在此处执行 SDK 清理工作（如有需要）
    }

    @Override
    public void onLowMemory() {
        super.onLowMemory();
        Log.w(TAG, "Application onLowMemory");

        // TODO: 在此处处理低内存回调（如有需要）
    }
}
