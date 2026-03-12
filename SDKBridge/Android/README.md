# SDKBridge - Unity 第三方SDK桥接框架

一个灵活的 Android 桥接工程，用于在 Unity 项目中快速接入各种第三方 Android SDK。每个 SDK 封装为独立插件模块，最终以 **aar** 方式集成到 Unity 工程中。

## 架构概览

```
SDKBridge/
├── bridge-core/                    # 核心桥接模块（必须）
│   ├── src/main/java/com/sdkbridge/core/
│   │   ├── BridgeManager.java      # 插件管理器（注册、路由、生命周期分发）
│   │   ├── UnityBridge.java        # Unity调用的静态入口
│   │   ├── plugin/
│   │   │   ├── IBridgePlugin.java  # 插件接口
│   │   │   └── BaseBridgePlugin.java # 插件基类（推荐继承）
│   │   ├── callback/
│   │   │   └── BridgeCallback.java # Android -> Unity 回调通信
│   │   └── util/
│   │       └── JsonHelper.java     # JSON工具类
│   └── libs/                       # 放置 unity-classes.jar
│
├── bridge-plugin-example/          # 示例插件（可作为模板）
│   └── src/main/java/.../ExamplePlugin.java
│
├── app/                            # 独立测试App（不参与aar打包）
│
├── unity/                          # Unity侧脚本
│   ├── NativeBridge.cs             # C#桥接管理器
│   └── NativeBridgeExample.cs      # 使用示例
│
└── gradle 配置文件...
```

## 快速开始

### 1. 环境准备

- Android Studio Arctic Fox 或更高版本
- JDK 8+
- Gradle 8.5

### 2. 放置 Unity classes.jar

从 Unity 安装目录复制 `classes.jar`：

```
{Unity安装路径}/Editor/Data/PlaybackEngines/AndroidPlayer/Variations/mono/Release/Classes/classes.jar
```

重命名为 `unity-classes.jar` 放入 `bridge-core/libs/` 目录。

### 3. 构建 aar

```bash
# 构建 bridge-core aar
./gradlew :bridge-core:assembleRelease

# 构建示例插件 aar
./gradlew :bridge-plugin-example:assembleRelease
```

产物位置：
- `bridge-core/build/outputs/aar/bridge-core-release.aar`
- `bridge-plugin-example/build/outputs/aar/bridge-plugin-example-release.aar`

### 4. 集成到 Unity

1. 将所有 aar 文件复制到 Unity 工程的 `Assets/Plugins/Android/` 目录
2. 将 `unity/NativeBridge.cs` 复制到 Unity 的 `Assets/Scripts/` 目录
3. 在场景中创建空 GameObject，命名为 `SDKBridgeReceiver`，挂载 `NativeBridge` 脚本

---

## 开发新插件（接入新的第三方SDK）

### 步骤一：创建插件模块

1. 在项目根目录创建新模块，例如 `bridge-plugin-wechat`
2. 在 `settings.gradle.kts` 中添加：
   ```kotlin
   include(":bridge-plugin-wechat")
   ```

### 步骤二：配置 build.gradle.kts

```kotlin
plugins {
    id("com.android.library")
}

android {
    namespace = "com.sdkbridge.plugin.wechat"
    compileSdk = property("COMPILE_SDK").toString().toInt()

    defaultConfig {
        minSdk = property("MIN_SDK").toString().toInt()
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_1_8
        targetCompatibility = JavaVersion.VERSION_1_8
    }
}

dependencies {
    implementation(project(":bridge-core"))
    // 添加微信SDK依赖
    implementation("com.tencent.mm.opensdk:wechat-sdk-android:6.8.0")
}
```

### 步骤三：实现插件类

```java
package com.sdkbridge.plugin.wechat;

import android.app.Activity;
import android.os.Bundle;
import com.sdkbridge.core.plugin.BaseBridgePlugin;

public class WeChatPlugin extends BaseBridgePlugin {

    @Override
    public String getPluginName() {
        return "WeChat";   // Unity侧通过此名称调用
    }

    @Override
    public void onInit(Activity activity, Bundle params) {
        super.onInit(activity, params);
        // 初始化微信SDK
    }

    @Override
    public String execute(String methodName, String jsonParams) {
        switch (methodName) {
            case "login":
                return handleLogin(jsonParams);
            case "share":
                return handleShare(jsonParams);
            default:
                return errorResult(-1, "未知方法: " + methodName);
        }
    }

    private String handleLogin(String jsonParams) {
        // 调用微信SDK的登录方法
        // ...
        return successResult(null);
    }

    private String handleShare(String jsonParams) {
        // 调用微信SDK的分享方法
        // ...
        return successResult(null);
    }
}
```

### 步骤四：Unity侧调用

```csharp
// 初始化
NativeBridge.Init();
NativeBridge.RegisterPlugin("com.sdkbridge.plugin.wechat.WeChatPlugin");

// 同步调用
string result = NativeBridge.Call("WeChat", "login", "{\"scope\":\"snsapi_userinfo\"}");

// 异步调用
NativeBridge.CallAsync("WeChat", "login", "{\"scope\":\"snsapi_userinfo\"}", (data) => {
    Debug.Log("微信登录结果: " + data);
});

// 事件监听
NativeBridge.AddEventListener("WeChat", "onPayResult", (data) => {
    Debug.Log("支付结果: " + data);
});
```

---

## 通信机制说明

### Unity → Android（同步调用）

```
Unity C#                        Android Java
───────                         ────────────
NativeBridge.Call()
    │
    ├── AndroidJavaClass("com.sdkbridge.core.UnityBridge")
    │       .CallStatic("call", pluginName, methodName, jsonParams)
    │
    └──────────────────────────► UnityBridge.call()
                                    │
                                    ├── BridgeManager.call()
                                    │       │
                                    │       └── plugin.execute(methodName, jsonParams)
                                    │               │
                                    └───────────────┘ return JSON result
```

### Android → Unity（异步回调）

```
Android Java                    Unity C#
────────────                    ───────
plugin.sendCallback(callbackId, data)
    │
    ├── BridgeCallback.sendToUnity()
    │       │
    │       └── UnityPlayer.UnitySendMessage(
    │               "SDKBridgeReceiver",
    │               "OnNativeCallback",
    │               jsonMessage
    │           )
    │
    └──────────────────────────► NativeBridge.OnNativeCallback()
                                    │
                                    └── 查找 callbackId 对应的 Action 并执行
```

### Android → Unity（事件推送）

```
Android Java                    Unity C#
────────────                    ───────
plugin.sendEvent(eventName, data)
    │
    └── BridgeCallback.sendEventToUnity()
            │
            └── UnitySendMessage → NativeBridge.OnNativeEvent()
                                        │
                                        └── 触发所有注册的事件监听器
```

## 数据格式约定

### 请求参数
所有参数统一使用 JSON 字符串：
```json
{"key1": "value1", "key2": 123}
```

### 返回结果
```json
// 成功
{"code": 0, "msg": "success", "data": {...}}

// 失败
{"code": -1, "msg": "错误描述"}
```

### 异步回调消息
```json
{"plugin": "PluginName", "callbackId": "cb_1_xxx", "data": {...}}
```

### 事件消息
```json
{"plugin": "PluginName", "event": "eventName", "data": {...}}
```

---

## 注意事项

1. **线程安全**：SDK 调用如果涉及 UI 操作，需要通过 `runOnUiThread()` 切到主线程
2. **混淆规则**：每个插件模块的 `consumer-rules.pro` 要保留插件类不被混淆
3. **生命周期**：如果第三方 SDK 需要感知 Activity 生命周期，覆写对应的生命周期方法即可
4. **unity-classes.jar**：仅编译时依赖（compileOnly），不会打包进 aar
