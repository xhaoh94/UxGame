# UxGame Agent 指南

本文档概述了 UxGame 项目的构建流程、代码风格和架构模式。
项目为 Unity 客户端游戏，配合 Go 语言服务器（提供二进制文件），共享 Protobuf/Config 定义。

## 项目结构

```
UxGame/
├── Unity/               # Unity 项目（客户端）
│   └── Assets/
│       ├── Main/        # 核心框架（不可热更）
│       ├── HotfixBase/  # 热更基础类（框架层）
│       ├── Hotfix/      # 业务逻辑（可热更）
│       │   └── CodeGen/ # 生成代码（禁止编辑）
│       ├── Editor/      # 编辑器工具
│       ├── ThirdParty/  # 第三方库
│       └── StreamingAssets/ # 运行时资源
├── Proto/               # Protobuf 定义和生成脚本
├── DataTables/          # 游戏配置（Luban）和生成脚本
└── Server/              # 服务器可执行文件和配置
```

## 构建与生成命令

### 1. 代码生成（修改定义后需执行）

**Protobuf（网络消息）：**
```batch
运行 Proto\gen_csharp.bat
输入：Proto/protofiles/*.proto
输出：Unity/Assets/Hotfix/CodeGen/Proto/
```

**游戏配置（Excel/Data）：**
```batch
运行 DataTables\gen.bat
输入：DataTables/Datas/*.xlsx 或 *.json
输出：C#代码和JSON数据，需复制到：
  - Unity/Assets/Hotfix/CodeGen/Config/
  - Unity/Assets/StreamingAssets/
```

### 2. Unity 客户端构建

**无 CLI 构建命令**，需使用 Unity Editor：
- **构建游戏**：File > Build Settings
- **构建 AssetBundles**：Window > YooAsset > AssetBundle Collector/Builder

### 3. 测试

**Unity Test Runner（NUnit 框架）：**
- **运行全部测试**：Window > General > Test Runner > "Run All"
- **运行单个测试**：
  - 方法一：在 Test Runner 窗口中右键测试类/方法 → "Run"
  - 方法二：在 Project 视图中右键测试文件 → "Run"
- **运行选中的测试**：选中一个或多个测试后点击 "Run Selected"

## 代码风格与约定（C# / Unity）

### 通用原则

- **异步编程**：统一使用 `UniTask` 和 `UniTask<T>`，禁止使用 Unity Coroutines
- **热更机制**：业务逻辑在 `Assets/Hotfix`，核心框架在 `Assets/Main`
- **生成代码**：禁止编辑 `CodeGen/` 目录下的文件，修改源文件后重新生成

### 命名规范

| 类型 | 命名风格 | 示例 |
|------|----------|------|
| 类/结构体/枚举 | PascalCase | `UIManager`, `LoginRequest` |
| 接口 | IPascalCase | `IWindow`, `IService` |
| 方法/属性 | PascalCase | `ConnectAsync`, `IsReady` |
| 公有字段 | PascalCase（推荐用属性） | `public int Count;` |
| 私有/受保护字段 | _camelCase | `_networkClient`, `_isInitialized` |
| 常量 | PascalCase 或 UPPER_SNAKE_CASE | `MaxCount`, `MAX_CONNECTIONS` |
| 局部变量 | camelCase | `playerData`, `itemList` |

### 格式与语法

- **缩进**：4 空格
- **大括号**：K&R 风格（开括号与语句同行）
- **导入顺序**：
  ```csharp
  using System;
  using System.Collections.Generic;
  using UnityEngine;
  using Cysharp.Threading.Tasks;  // 第三方库
  using FairyGUI;
  using Ux;                       // 项目命名空间
  ```
- **空值检查**：使用 `?.` 和 `??` 运算符
- **字符串格式化**：使用 `$"Key: {value}"` 而非 `string.Format`
- **注释**：使用 XML 文档注释 `///` 描述公共 API

### 错误处理

- **异常**：谨慎使用，逻辑失败优先返回 `UniTask<bool>` 或结果对象
- **日志**：使用严格封装的日志接口：
  ```csharp
  Log.Debug("连接已启动");                          // 调试信息
  Log.Info("玩家登录成功");                          // 一般信息
  Log.Warning($"重试中... 第{count}次");             // 警告
  Log.Error($"连接失败: {error}");                   // 错误
  Log.Error(ex);                                   // 异常对象
  Log.Fatal("严重错误，程序即将退出");                 // 致命错误
  ```

## 架构模式

### 1. 管理器（单例模式）

继承 `Singleton<T>` 处理全局状态：
```csharp
public class BattleMgr : Singleton<BattleMgr>
{
    protected override void OnCreated()
    {
        // 初始化逻辑
    }
    
    public void StartBattle() { }
}

// 使用方式
BattleMgr.Ins.StartBattle();
```

### 2. 模块系统

继承 `ModuleBase<T>` 实现模块化：
```csharp
public class LoginModule : ModuleBase<LoginModule>
{
    protected override void OnInit() { }
    protected override void OnRelease() { }
}
```

### 3. 事件系统

使用属性驱动的事件系统实现解耦：
```csharp
// 事件定义
public enum GameEvent 
{ 
    PlayerDied,
    LevelComplete 
}

// 事件监听（使用属性标记）
[MainEvt(GameEvent.PlayerDied)]
private void OnPlayerDied(object args) { }

// 注册监听
EventMgr.Ins.RegisterFastMethod(this);

// 触发事件
EventMgr.Ins.Run(GameEvent.PlayerDied, playerId);

// 带参数触发
EventMgr.Ins.Run(GameEvent.LevelComplete, levelId, score);
```

### 4. UI 系统（FairyGUI）

- 继承 `UIWindow`（模态窗口）或 `UIView`（普通视图）
- 使用 Binder 类（自动生成）访问组件
- 生命周期回调：`OnInit` → `OnShow` → `OnHide`

```csharp
[UI]  // 标记为 UI 类
public class TestView : UIView
{
    protected override UILayer Layer => UILayer.View;
    public override UIType Type => UIType.Stack;
    
    protected override void OnInit()
    {
        // 初始化组件
        testList.SetItemRenderer<TestItem>();
    }
    
    protected override void OnShow()
    {
        // 显示逻辑
    }
    
    // 按钮点击事件（partial 方法）
    partial void OnBtnTestClick(EventContext e)
    {
        // 点击处理
    }
}

// 显示/隐藏 UI
UIMgr.Ins.Show<TestView>();
UIMgr.Ins.Hide<TestView>();
```

### 5. 资源加载（YooAsset）

统一通过 `ResMgr` 异步加载资源：
```csharp
// 异步加载并实例化
GameObject obj = await ResMgr.Ins.LoadAssetAsync<GameObject>(
    string.Format(PathHelper.Res.Prefab, "Hero_ZS"));

// 同步加载（谨慎使用）
var asset = ResMgr.Ins.LoadAsset<Sprite>("Assets/Icons/icon.png");

// 资源路径常量
PathHelper.Res.UI       // UI 资源路径模板
PathHelper.Res.Prefab   // 预制体路径模板
PathHelper.Res.Config   // 配置路径模板
```

### 6. 网络通信

```csharp
// 发送消息
NetMgr.Ins.Send(Cmd.LoginRequest, request);

// RPC 调用
var response = await NetMgr.Ins.Call<LoginRequest, LoginResponse>(
    Cmd.LoginRequest, request);

// 网络事件监听
[Net(Cmd.S2C_PlayerJoin)]
private void OnPlayerJoin(PlayerJoinMsg msg) { }
```

## Agent 工作流程

1. **检查需求**：任务是否涉及修改 Proto 或 Config？
2. **修改源文件**：先编辑 `.proto` 或 Excel 文件
3. **重新生成**：运行对应的 `.bat` 脚本
4. **实现逻辑**：在 `Assets/Hotfix` 中编写 C# 代码
5. **验证结果**：使用 Unity Editor 检查编译错误

## 常用编辑器工具路径

| 工具 | 菜单路径 |
|------|----------|
| UI 代码生成 | UxGame > 工具 > UI > 代码生成 |
| Proto 生成 | UxGame > 工具 > Proto |
| 配置生成 | UxGame > 工具 > Config |
| 版本构建 | UxGame > 构建 > 版本 |
| 调试器 | Window > UxDebugger > Event/Time/UI/Res |
