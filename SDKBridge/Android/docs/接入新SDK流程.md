# 接入新SDK详细流程

以接入一个名为 "SuperAd" 的广告SDK为例，完整演示每一步操作。

---

## 第一步：创建插件模块

### 方式一：Android Studio 界面创建（推荐）

1. `File → New → New Module`
2. 左侧选 **Android Library**
3. 填写信息：

| 配置项 | 填写 |
|--------|------|
| Module name | `bridge-plugin-superad` |
| Package name | `com.sdkbridge.plugin.superad` |
| Language | Java |
| Minimum SDK | API 21 |
| Build Configuration Language | Kotlin DSL |

4. 点击完成

### 方式二：手动创建

创建目录结构：
```
bridge-plugin-superad/
├── libs/                          ← 第三方SDK的aar/jar放这里
├── src/main/
│   ├── AndroidManifest.xml
│   └── java/com/sdkbridge/plugin/superad/
│       └── SuperAdPlugin.java
├── build.gradle.kts
├── consumer-rules.pro
└── proguard-rules.pro
```

然后在 `settings.gradle.kts` 中添加：
```kotlin
include(":bridge-plugin-superad")
```

---

## 第二步：放置第三方SDK文件

### 情况一：SDK提供的是 aar 文件

将 aar 文件放入模块的 `libs/` 目录：

```
bridge-plugin-superad/
└── libs/
    └── superad-sdk-2.0.1.aar     ← 放这里
```

### 情况二：SDK提供的是 jar 文件

同样放入 `libs/` 目录：

```
bridge-plugin-superad/
└── libs/
    └── superad-sdk-2.0.1.jar     ← 放这里
```

### 情况三：SDK通过Maven仓库提供

不需要放文件，直接在 build.gradle.kts 中用远程依赖（见下一步）。

---

## 第三步：配置 build.gradle.kts

```kotlin
plugins {
    id("com.android.library")
}

android {
    namespace = "com.sdkbridge.plugin.superad"
    compileSdk = property("COMPILE_SDK").toString().toInt()

    defaultConfig {
        minSdk = property("MIN_SDK").toString().toInt()
        consumerProguardFiles("consumer-rules.pro")
    }

    buildTypes {
        release {
            isMinifyEnabled = false
            proguardFiles(
                getDefaultProguardFile("proguard-android-optimize.txt"),
                "proguard-rules.pro"
            )
        }
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_1_8
        targetCompatibility = JavaVersion.VERSION_1_8
    }
}

dependencies {
    // ======== 必须：依赖核心桥接模块 ========
    implementation(project(":bridge-core"))

    // ======== 引用第三方SDK（三种情况选一种） ========

    // 情况一：libs目录下的 aar 文件
    implementation(files("libs/superad-sdk-2.0.1.aar"))

    // 情况二：libs目录下的 jar 文件
    // implementation(files("libs/superad-sdk-2.0.1.jar"))

    // 情况三：Maven远程依赖（SDK文档会提供坐标）
    // implementation("com.superad:sdk:2.0.1")

    // 情况四：libs目录下有多个jar/aar，一次性全部引入
    // implementation(fileTree(mapOf("dir" to "libs", "include" to listOf("*.jar", "*.aar"))))
}
```

### 如果SDK的Maven仓库不是标准的

有些SDK需要额外添加仓库地址，在根目录 `settings.gradle.kts` 中加：

```kotlin
dependencyResolutionManagement {
    repositoriesMode.set(RepositoriesMode.FAIL_ON_PROJECT_REPOS)
    repositories {
        google()
        mavenCentral()
        // 第三方SDK的私有仓库（按SDK文档填写）
        maven { url = uri("https://repo.superad.com/maven") }
    }
}
```

---

## 第四步：编写插件类

```java
package com.sdkbridge.plugin.superad;

import android.app.Activity;
import android.os.Bundle;
import android.util.Log;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;

import com.sdkbridge.core.plugin.BaseBridgePlugin;
import com.sdkbridge.core.util.JsonHelper;

public class SuperAdPlugin extends BaseBridgePlugin {

    private static final String TAG = "SuperAdPlugin";

    @NonNull
    @Override
    public String getPluginName() {
        return "SuperAd";  // Unity侧通过此名称调用
    }

    @Override
    public void onInit(@NonNull Activity activity, @Nullable Bundle params) {
        super.onInit(activity, params);

        // 从参数中读取SDK配置
        String json = params != null ? params.getString("json") : null;
        String appId = JsonHelper.getString(json, "appId");
        String appKey = JsonHelper.getString(json, "appKey");

        // 调用第三方SDK的初始化方法
        // SuperAdSDK.init(activity, appId, appKey);
        Log.i(TAG, "SuperAd SDK 初始化完成");
    }

    @Nullable
    @Override
    public String execute(@NonNull String methodName, @Nullable String jsonParams) {
        switch (methodName) {
            case "loadAd":
                return handleLoadAd(jsonParams);
            case "showAd":
                return handleShowAd(jsonParams);
            case "isAdReady":
                return handleIsAdReady(jsonParams);
            default:
                return errorResult(-1, "未知方法: " + methodName);
        }
    }

    private String handleLoadAd(@Nullable String jsonParams) {
        String adType = JsonHelper.getString(jsonParams, "adType");
        String callbackId = JsonHelper.getString(jsonParams, "callbackId");

        // 调用SDK加载广告
        // SuperAdSDK.loadAd(adType, new AdListener() {
        //     @Override
        //     public void onLoaded() {
        //         sendCallback(callbackId, "{\"status\":\"loaded\"}");
        //     }
        //     @Override
        //     public void onError(String error) {
        //         sendCallback(callbackId, "{\"status\":\"error\",\"msg\":\"" + error + "\"}");
        //     }
        // });

        return successResult(null);
    }

    private String handleShowAd(@Nullable String jsonParams) {
        String adType = JsonHelper.getString(jsonParams, "adType");

        // 必须在主线程显示广告
        runOnUiThread(() -> {
            // SuperAdSDK.showAd(activity, adType);
        });

        return successResult(null);
    }

    private String handleIsAdReady(@Nullable String jsonParams) {
        // boolean ready = SuperAdSDK.isAdReady();
        boolean ready = false;
        return successResult("{\"ready\":" + ready + "}");
    }

    // ======== 生命周期（按SDK要求覆写） ========

    @Override
    public void onResume() {
        // SuperAdSDK.onResume();
    }

    @Override
    public void onPause() {
        // SuperAdSDK.onPause();
    }

    @Override
    public void onDestroy() {
        // SuperAdSDK.destroy();
    }
}
```

---

## 第五步：配置混淆规则

在 `consumer-rules.pro` 中添加：

```proguard
# 保留本插件类
-keep class com.sdkbridge.plugin.superad.** { *; }

# 保留第三方SDK的类（从SDK文档复制，每个SDK不同）
# -keep class com.superad.sdk.** { *; }
# -dontwarn com.superad.sdk.**
```

---

## 第六步：如果SDK需要权限或组件声明

在模块的 `AndroidManifest.xml` 中添加（会自动合并到最终APK）：

```xml
<?xml version="1.0" encoding="utf-8"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android">

    <!-- SDK需要的权限 -->
    <uses-permission android:name="android.permission.INTERNET" />
    <uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />

    <application>
        <!-- SDK需要注册的组件（Activity/Service/Receiver等） -->
        <!-- <activity android:name="com.superad.sdk.AdActivity" /> -->
    </application>

</manifest>
```

---

## 第七步：构建aar

```bash
./gradlew :bridge-plugin-superad:assembleRelease
```

产物位置：
```
bridge-plugin-superad/build/outputs/aar/bridge-plugin-superad-release.aar
```

---

## 第八步：集成到Unity

### 复制文件到Unity工程

```
Unity工程/
└── Assets/
    └── Plugins/
        └── Android/
            ├── bridge-core-release.aar              ← 核心模块（必须）
            ├── bridge-plugin-superad-release.aar     ← 插件模块
            └── superad-sdk-2.0.1.aar                ← 第三方SDK原始aar（如果有的话）
```

> **注意**：如果第三方SDK是aar格式且用 `implementation(files(...))` 引入的，
> 它不会被打包进插件的aar里。需要把SDK原始aar也一起复制到Unity工程中。
> 如果是Maven远程依赖，同样需要手动下载aar放入Unity。

### Unity C# 调用

```csharp
void Start()
{
    // 初始化桥接框架
    NativeBridge.Init();

    // 注册插件（传入Java完整类名）
    NativeBridge.RegisterPlugin(
        "com.sdkbridge.plugin.superad.SuperAdPlugin",
        "{\"appId\":\"your_app_id\",\"appKey\":\"your_app_key\"}"
    );

    // 监听广告事件
    NativeBridge.AddEventListener("SuperAd", "onAdClosed", OnAdClosed);
}

// 加载广告（异步）
public void LoadAd()
{
    NativeBridge.CallAsync("SuperAd", "loadAd",
        "{\"adType\":\"rewarded\"}",
        (data) => { Debug.Log("广告加载结果: " + data); }
    );
}

// 显示广告（同步）
public void ShowAd()
{
    NativeBridge.Call("SuperAd", "showAd", "{\"adType\":\"rewarded\"}");
}

void OnAdClosed(string data)
{
    Debug.Log("广告关闭: " + data);
}
```

---

## 总结清单

| 步骤 | 操作 | 关键文件 |
|------|------|----------|
| 1 | 创建 Android Library 模块 | `settings.gradle.kts` |
| 2 | 放置SDK的aar/jar到 `libs/` | `libs/xxx.aar` |
| 3 | 配置依赖 | `build.gradle.kts` |
| 4 | 继承 BaseBridgePlugin 实现插件 | `XxxPlugin.java` |
| 5 | 配置混淆规则 | `consumer-rules.pro` |
| 6 | 声明权限和组件（如需要） | `AndroidManifest.xml` |
| 7 | 构建aar | `gradlew assembleRelease` |
| 8 | 复制aar到Unity + C#调用 | `Assets/Plugins/Android/` |
