# 原生 SDK 启动初始化骨架

## Context

用户需要在 Android 和 iOS 平台建立原生启动初始化入口点，用于未来接入各种第三方 SDK（如推送、统计、广告等）。当前项目 `Unity/Assets/Plugins/Android/` 已有两个 `.aar` 桥接库，`Unity/Assets/Plugins/Ios/` 已有完整的 BridgeManager 桥接插件系统，但两端均缺少应用启动时的初始化钩子。

本计划仅创建骨架模板（带 `// TODO` 占位注释），不包含任何实际 SDK 集成代码。

### 用户确认
- **包名**: `com.uxgame.core`（用户后续自行修改）
- **模板范围**: 骨架模板，仅含 `// TODO` 占位

### 已有文件（不修改）
- `Unity/Assets/Plugins/Android/bridge-core-release.aar`
- `Unity/Assets/Plugins/Android/bridge-plugin-example-release.aar`
- `Unity/Assets/Plugins/Ios/BridgeManager.h`
- `Unity/Assets/Plugins/Ios/BridgeManager.mm`
- `Unity/Assets/Plugins/Ios/ExamplePlugin.h`
- `Unity/Assets/Plugins/Ios/ExamplePlugin.m`
- `Unity/Assets/Plugins/Ios/ExampleSwiftPlugin.swift`

## Task Dependency Graph

| Task | Depends On | Reason |
|------|------------|--------|
| Task 1: 创建 MyApplication.java | None | 独立文件，无前置依赖 |
| Task 2: 创建 AndroidManifest.xml | None | 独立文件，无前置依赖（引用 `.MyApplication` 但不依赖 Task 1 完成） |
| Task 3: 创建 CustomAppController.mm | None | 独立文件，iOS 端无依赖 |
| Task 4: 验证所有文件 | Task 1, 2, 3 | 需要所有文件就位后统一验证 |

## Parallel Execution Graph

Wave 1 (Start immediately):
├── Task 1: 创建 MyApplication.java (no dependencies)
├── Task 2: 创建 AndroidManifest.xml (no dependencies)
└── Task 3: 创建 CustomAppController.mm (no dependencies)

Wave 2 (After Wave 1 completes):
└── Task 4: 验证所有文件存在且内容正确 (depends: Task 1, 2, 3)

Critical Path: Wave 1 (all parallel) → Wave 2
Estimated Parallel Speedup: All 3 creation tasks run simultaneously

## Tasks

### Task 1: 创建 Android Application 子类

**Description**: 创建 `Unity/Assets/Plugins/Android/MyApplication.java`，继承 `android.app.Application`，在 `onCreate()` 中提供 SDK 初始化的 TODO 占位。

**File**: `Unity/Assets/Plugins/Android/MyApplication.java`

**Content**:
```java
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
```

**Delegation Recommendation**:
- Category: `quick` - 单文件创建，内容已明确，无需复杂推理
- Skills: [] - 无需特殊技能

**Skills Evaluation**:
- ❌ OMITTED `dev-browser`: 非浏览器任务
- ❌ OMITTED `frontend-ui-ux`: 非前端 UI 任务
- ❌ OMITTED `git-master`: 非 git 操作
- ❌ OMITTED `playwright`: 非浏览器任务

**Depends On**: None
**Acceptance Criteria**:
- 文件 `Unity/Assets/Plugins/Android/MyApplication.java` 存在
- 文件包含 `package com.uxgame.core;`
- 文件包含 `extends Application`
- 文件包含 `onCreate` 方法
- 文件包含 `// TODO` 占位注释

---

### Task 2: 创建 Android 清单文件

**Description**: 创建 `Unity/Assets/Plugins/Android/AndroidManifest.xml`，声明 `android:name=".MyApplication"` 以在启动时加载自定义 Application 类。

**File**: `Unity/Assets/Plugins/Android/AndroidManifest.xml`

**Content**:
```xml
<?xml version="1.0" encoding="utf-8"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android"
    xmlns:tools="http://schemas.android.com/tools">

    <!--
        自定义 AndroidManifest.xml
        Unity 构建时会将此清单与主清单合并。
        android:name 指定自定义 Application 子类，在应用启动时自动加载。
    -->
    <application
        android:name=".MyApplication"
        tools:replace="android:name">

        <!-- TODO: 在此处添加需要在清单中声明的 SDK 组件 -->
        <!-- 示例：
        <meta-data
            android:name="com.example.sdk.APP_KEY"
            android:value="your-app-key" />
        -->

    </application>

</manifest>
```

**Delegation Recommendation**:
- Category: `quick` - 单文件创建，内容已明确
- Skills: [] - 无需特殊技能

**Skills Evaluation**:
- ❌ OMITTED `dev-browser`: 非浏览器任务
- ❌ OMITTED `frontend-ui-ux`: 非前端 UI 任务
- ❌ OMITTED `git-master`: 非 git 操作
- ❌ OMITTED `playwright`: 非浏览器任务

**Depends On**: None
**Acceptance Criteria**:
- 文件 `Unity/Assets/Plugins/Android/AndroidManifest.xml` 存在
- 文件包含 `android:name=".MyApplication"`
- 文件包含 `tools:replace="android:name"`
- XML 格式合法（有 `<?xml` 声明和 `<manifest>` 根元素）

---

### Task 3: 创建 iOS AppController 子类

**Description**: 创建 `Unity/Assets/Plugins/Ios/CustomAppController.mm`，使用 Unity 提供的 `IMPL_APP_CONTROLLER_SUBCLASS` 宏来注册自定义 AppDelegate 子类，在应用生命周期钩子中提供 SDK 初始化的 TODO 占位。

**File**: `Unity/Assets/Plugins/Ios/CustomAppController.mm`

**Content**:
```objc
#import "UnityAppController.h"

/// 自定义 AppController
///
/// 通过 IMPL_APP_CONTROLLER_SUBCLASS 宏注册为 Unity 的 AppController 子类，
/// 在应用启动及生命周期事件中提供原生 SDK 初始化入口。
///
/// 使用方法：
///   在对应的生命周期方法中按需添加各 SDK 的初始化 / 回调代码。
@interface CustomAppController : UnityAppController
@end

@implementation CustomAppController

- (BOOL)application:(UIApplication *)application
    didFinishLaunchingWithOptions:(NSDictionary *)launchOptions {

    NSLog(@"[CustomAppController] didFinishLaunchingWithOptions 开始");

    // TODO: 在调用 super 之前初始化需要尽早启动的 SDK
    // 示例：
    // [ThirdPartySDK initWithAppKey:@"your-app-key"];

    BOOL result = [super application:application didFinishLaunchingWithOptions:launchOptions];

    // TODO: 在调用 super 之后初始化依赖 Unity 引擎的 SDK
    // 示例：
    // [AnalyticsSDK startWithConfiguration:config];

    NSLog(@"[CustomAppController] didFinishLaunchingWithOptions 完成");
    return result;
}

- (void)applicationDidBecomeActive:(UIApplication *)application {
    [super applicationDidBecomeActive:application];
    NSLog(@"[CustomAppController] applicationDidBecomeActive");

    // TODO: 在此处处理应用激活回调（如有需要）
}

- (void)applicationWillResignActive:(UIApplication *)application {
    [super applicationWillResignActive:application];
    NSLog(@"[CustomAppController] applicationWillResignActive");

    // TODO: 在此处处理应用即将进入后台回调（如有需要）
}

- (void)applicationDidEnterBackground:(UIApplication *)application {
    [super applicationDidEnterBackground:application];
    NSLog(@"[CustomAppController] applicationDidEnterBackground");

    // TODO: 在此处处理应用进入后台回调（如有需要）
}

- (void)applicationWillEnterForeground:(UIApplication *)application {
    [super applicationWillEnterForeground:application];
    NSLog(@"[CustomAppController] applicationWillEnterForeground");

    // TODO: 在此处处理应用即将回到前台回调（如有需要）
}

- (void)applicationWillTerminate:(UIApplication *)application {
    [super applicationWillTerminate:application];
    NSLog(@"[CustomAppController] applicationWillTerminate");

    // TODO: 在此处执行 SDK 清理工作（如有需要）
}

- (void)application:(UIApplication *)application
    didRegisterForRemoteNotificationsWithDeviceToken:(NSData *)deviceToken {
    [super application:application didRegisterForRemoteNotificationsWithDeviceToken:deviceToken];
    NSLog(@"[CustomAppController] didRegisterForRemoteNotificationsWithDeviceToken");

    // TODO: 在此处将 deviceToken 传递给推送 SDK（如有需要）
}

- (void)application:(UIApplication *)application
    didFailToRegisterForRemoteNotificationsWithError:(NSError *)error {
    [super application:application didFailToRegisterForRemoteNotificationsWithError:error];
    NSLog(@"[CustomAppController] didFailToRegisterForRemoteNotificationsWithError: %@", error);

    // TODO: 在此处处理推送注册失败（如有需要）
}

@end

IMPL_APP_CONTROLLER_SUBCLASS(CustomAppController)
```

**Delegation Recommendation**:
- Category: `quick` - 单文件创建，内容已明确
- Skills: [] - 无需特殊技能

**Skills Evaluation**:
- ❌ OMITTED `dev-browser`: 非浏览器任务
- ❌ OMITTED `frontend-ui-ux`: 非前端 UI 任务
- ❌ OMITTED `git-master`: 非 git 操作
- ❌ OMITTED `playwright`: 非浏览器任务

**Depends On**: None
**Acceptance Criteria**:
- 文件 `Unity/Assets/Plugins/Ios/CustomAppController.mm` 存在
- 文件包含 `#import "UnityAppController.h"`
- 文件包含 `@interface CustomAppController : UnityAppController`
- 文件包含 `IMPL_APP_CONTROLLER_SUBCLASS(CustomAppController)`
- 文件包含 `didFinishLaunchingWithOptions` 方法
- 文件包含 `// TODO` 占位注释
- 文件注释使用中文

---

### Task 4: 验证所有文件

**Description**: 验证 Wave 1 创建的所有文件均存在且内容正确。

**Delegation Recommendation**:
- Category: `quick` - 简单文件检查
- Skills: [] - 无需特殊技能

**Depends On**: Task 1, Task 2, Task 3
**Acceptance Criteria**:
```bash
# 验证 Android 文件存在
ls "Unity/Assets/Plugins/Android/MyApplication.java"
ls "Unity/Assets/Plugins/Android/AndroidManifest.xml"

# 验证 iOS 文件存在
ls "Unity/Assets/Plugins/Ios/CustomAppController.mm"

# 验证 Android Application 关键内容
grep -q "package com.uxgame.core" "Unity/Assets/Plugins/Android/MyApplication.java"
grep -q "extends Application" "Unity/Assets/Plugins/Android/MyApplication.java"
grep -q "onCreate" "Unity/Assets/Plugins/Android/MyApplication.java"

# 验证 AndroidManifest 关键内容
grep -q 'android:name=".MyApplication"' "Unity/Assets/Plugins/Android/AndroidManifest.xml"
grep -q 'tools:replace="android:name"' "Unity/Assets/Plugins/Android/AndroidManifest.xml"

# 验证 iOS AppController 关键内容
grep -q "IMPL_APP_CONTROLLER_SUBCLASS(CustomAppController)" "Unity/Assets/Plugins/Ios/CustomAppController.mm"
grep -q "UnityAppController" "Unity/Assets/Plugins/Ios/CustomAppController.mm"
grep -q "didFinishLaunchingWithOptions" "Unity/Assets/Plugins/Ios/CustomAppController.mm"
```

## Must NOT Have（禁止事项）

- ❌ 不得包含任何实际的第三方 SDK 初始化代码（仅 `// TODO` 占位）
- ❌ 不得修改已有文件（`.aar` 文件、BridgeManager、ExamplePlugin 等）
- ❌ 不得添加任何第三方 SDK 依赖或 import
- ❌ 不得创建 `.meta` 文件（Unity Editor 会自动生成）
- ❌ 不得在 AndroidManifest.xml 中声明 Activity（Unity 主 Activity 由引擎管理）

## Commit Strategy

所有 3 个文件在同一个原子提交中提交：
```
feat: 添加 Android/iOS 原生启动初始化骨架

- Android: MyApplication.java (Application 子类，onCreate 入口)
- Android: AndroidManifest.xml (声明自定义 Application)
- iOS: CustomAppController.mm (IMPL_APP_CONTROLLER_SUBCLASS 生命周期钩子)

所有 SDK 初始化位置均为 TODO 占位，待后续按需接入。
```

## Success Criteria

1. `Unity/Assets/Plugins/Android/MyApplication.java` 存在，包含 `com.uxgame.core` 包名和 `extends Application`
2. `Unity/Assets/Plugins/Android/AndroidManifest.xml` 存在，正确引用 `.MyApplication`，使用 `tools:replace`
3. `Unity/Assets/Plugins/Ios/CustomAppController.mm` 存在，使用 `IMPL_APP_CONTROLLER_SUBCLASS` 宏注册
4. 所有文件仅包含骨架代码和 `// TODO` 占位，无实际 SDK 代码
5. 不修改任何已有文件

## Execution Instructions

1. **Wave 1**: Fire these 3 tasks IN PARALLEL (no dependencies)
   ```
   task(category="quick", load_skills=[], prompt="创建文件 Unity/Assets/Plugins/Android/MyApplication.java，内容如下：[Task 1 Content]")
   task(category="quick", load_skills=[], prompt="创建文件 Unity/Assets/Plugins/Android/AndroidManifest.xml，内容如下：[Task 2 Content]")
   task(category="quick", load_skills=[], prompt="创建文件 Unity/Assets/Plugins/Ios/CustomAppController.mm，内容如下：[Task 3 Content]")
   ```

2. **Wave 2**: After Wave 1 completes, verify
   ```
   task(category="quick", load_skills=[], prompt="验证以下 3 个文件存在且内容正确：[Task 4 验证命令]")
   ```

3. **Commit**: After all waves pass, commit with the message in Commit Strategy section