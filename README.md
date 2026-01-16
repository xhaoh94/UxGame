# UxGame 框架

一个集成了 HybridCLR、YooAsset 和 FairyGUI 的综合 Unity 游戏开发框架，为高效游戏开发提供强大工具。

## 目录

- [功能特性](#功能特性)
- [前置要求](#前置要求)
- [安装说明](#安装说明)
- [设置指南](#设置指南)
- [使用示例](#使用示例)
  - [事件系统](#事件系统)
  - [网络模块](#网络模块)
  - [资源管理](#资源管理)
  - [时间系统](#时间系统)
  - [UI系统](#ui系统)
  - [公式解析器](#公式解析器)
- [架构设计](#架构设计)
- [故障排除](#故障排除)
- [贡献指南](#贡献指南)
- [许可证](#许可证)

## 功能特性

### HybridCLR + YooAsset 集成
- 使用 HybridCLR 实现 C# 热更新
- 使用 YooAsset 实现资源热更新
- 一键构建到指定目录
- 资源差异分析
- 本地资源服务器 (DHFS) 支持编辑器热更新测试

### 事件系统
- 独立的事件系统，包含单例默认事件系统
- 基于特性的注册，减少重复代码
- 事件队列带每帧触发上限，防止掉帧
- 支持同步和异步事件调用
- 按对象标签一键取消所有事件监听
- 可视化事件监控工具

### 网络模块 (Net)
- 集成 TCP、KCP 和 WebSocket 协议
- 自定义 RPC 流程，实现简洁的请求-响应代码
- 可配置数据序列化的大小端

### 资源管理 (Res)
- 封装 YooAssets 加载接口
- 自动脚本挂载和资源句柄回收
- 支持基于标签的资源懒加载管理
- 可视化 UI 包引用计数工具

### 时间系统
- 多种定时器类型，采用优化的排序算法
  - 间隔定时器（秒）
  - 帧定时器
  - 时间戳定时器
  - Cron 表达式定时器
- 可视化定时器监控工具

### UI 系统
- FairyGUI 包装器，增强功能
- 支持异步加载和懒加载
- UI 栈管理
- UI 动画和模糊效果
- 界面嵌套支持
- 可视化代码生成和事件绑定工具
- 多种 UI 组件：
  - UIView（常规界面）
  - UIWindow（弹窗界面）
  - UITabView（嵌套界面）
  - UITip（提示语）
  - UIMessageBox（确认对话框）
  - UIModel/RTModel（2D 中显示 3D 模型）
  - UIList（可自定义循环列表）

### 公式解析器 (Eval)
- 使用 AST 和对象池实现高性能字符串公式解析
- 支持自定义函数和变量
- 内置数学函数

### 开发中功能
- Timeline 战斗编辑工具
- 条件系统（需根据游戏定制）
- Tag 红点系统

## 前置要求

- Unity 2021.3 LTS 或更新版本
- .NET Framework 4.7.1 或更新版本
- HybridCLR
- YooAsset
- FairyGUI
- Visual Studio 2019 或更新版本（用于开发）

## 安装说明

1. 克隆仓库：
   ```bash
   git clone https://github.com/xhaoh94/UxGame.git
   ```

2. 在 Unity 中打开项目：
   - 启动 Unity Hub
   - 点击 "添加" 并选择克隆的项目文件夹
   - 使用 Unity 2021.3 LTS 或更新版本打开项目

3. 安装依赖：
   - HybridCLR 会在项目打开时自动配置
   - YooAsset 和 FairyGUI 包已包含在项目中

## 设置指南

### 1. 配置热更新

1. 打开 HybridCLR 窗口：`Tools > HybridCLR > Settings`
2. 根据需要配置热更新程序集设置
3. 构建热更新程序集：`Tools > HybridCLR > Build > Build Hot Update DLL`

### 2. 配置资源系统

1. 打开 YooAsset 设置：`YooAsset > Settings`
2. 配置资源包设置
3. 构建资源包：`YooAsset > Build > Build AssetBundles`

### 3. 配置 UI 系统

1. 通过 FairyGUI 编辑器导入 UI 包
2. 使用可视化工具生成代码绑定：`Tools > UI > Code Generator`
3. 在资源系统中配置 UI 包引用

### 4. 配置网络设置

1. 在网络管理器中设置服务器地址和端口
2. 配置协议类型（TCP/KCP/WebSocket）
3. 如有需要，设置数据序列化的大小端

## 使用示例

### 事件系统

```csharp
// 使用特性注册事件
[Event(MainEventType.PLAYER_LOGIN)]
private void OnPlayerLogin(object data)
{
    // 处理玩家登录事件
}

// 手动注册事件
EventMgr.Ins.On(MainEventType.PLAYER_LOGIN, this, (data) =>
{
    // 处理玩家登录事件
});

// 触发事件
EventMgr.Ins.Run(MainEventType.PLAYER_LOGIN, playerData);

// 移除对象的所有事件
EventMgr.Ins.OffTag(this);
```

### 网络模块

```csharp
// 连接服务器
var socket = NetMgr.Ins.Connect(NetType.TCP, "127.0.0.1:8080", OnConnect);

// 发送消息
var loginReq = new LoginRequest { Username = "user", Password = "pass" };
NetMgr.Ins.Send(CmdID.Login, loginReq);

// RPC 调用
var response = await NetMgr.Ins.Call<LoginRequest, LoginResponse>(CmdID.Login, loginReq);
```

### 资源管理

```csharp
// 加载资源
var handle = ResMgr.Ins.LoadAssetAsync<GameObject>("Assets/Resources/Player.prefab");
var playerPrefab = handle.AssetObject as GameObject;

// 实例化并自动管理资源
var player = ResMgr.Ins.Instantiate("Assets/Resources/Player.prefab", transform);
```

### 时间系统

```csharp
// 间隔定时器（延迟1秒后开始，每2秒触发1次，共触发5次）
TimeMgr.Ins.Timer(2.0f, this, OnTimerTrigger)
    .FirstDelay(1.0f)  // 初始延迟时间
    .Repeat(5)         // 重复次数
    .OnComplete(OnTimerComplete);  // 完成回调

// 帧定时器（立即开始，每60帧触发1次，共触发10次）
TimeMgr.Ins.Frame(60, this, OnFrameTimerTrigger)
    .FirstDelay(0)     // 初始延迟帧数
    .Repeat(10);       // 重复次数

// Cron 定时器（每天中午12点触发）
TimeMgr.Ins.Cron("0 0 12 * * ?", this, OnDailyTrigger);

// 定时器回调
private void OnTimerTrigger()
{
    Log.Debug("定时器触发");
}

private void OnFrameTimerTrigger()
{
    Log.Debug("帧定时器触发");
}

private void OnDailyTrigger()
{
    Log.Debug("每天触发一次的定时器");
}

private void OnTimerComplete()
{
    Log.Debug("定时器完成");
}
```

### UI 系统

```csharp
// 显示 UI
UIMgr.Ins.Show<UIMain>();

// 带参数显示 UI
var param = new UIParam { A = "value1", B = 123 };
UIMgr.Ins.Show<UISettings>(param);

// 隐藏 UI
UIMgr.Ins.Hide<UIMain>();

// 显示消息框
UIMgr.MessageBox.DoubleBtn(
    "确认", 
    "确定要退出吗？", 
    "是", () => Application.Quit(), 
    "否", null);
```

### 公式解析器

```csharp
// 基本计算
var result1 = EvalMgr.Ins.Parse("10 + 5 * 2"); // 返回 20

// 自定义变量和函数
EvalMgr.Ins.AddVariable("hp", 100);
EvalMgr.Ins.AddVariable("atk", 20);
EvalMgr.Ins.AddFunction("damage", (args) => args[0] * (1 + args[1] / 100.0));

var result2 = EvalMgr.Ins.Parse("damage(atk, 50)"); // 返回 30
```

## 架构设计

框架采用模块化架构，各模块职责清晰：

```
┌─────────────────────────────────────────────────┐
│                    UxGame                       │
├───────────┬───────────┬───────────┬────────────┤
│   Event   │    Net    │    Res    │    Time    │
├───────────┼───────────┼───────────┼────────────┤
│    UI     │    Eval   │   Hybrid  │   YooAsset │
│           │           │   CLR     │            │
└───────────┴───────────┴───────────┴────────────┘
```


## 许可证

MIT 许可证

版权所有 (c) 2023 xhaoh94

特此免费授予任何获得本软件及相关文档文件（"软件"）副本的人不受限制地处置该软件的权利，包括不受限制地使用、复制、修改、合并、发布、分发、转授许可和/或出售该软件副本，以及再授权被配发了本软件的人如上的权利，须在下列条件下：

上述版权声明和本许可声明应包含在该软件的所有副本或实质成分中。

本软件按"原样"提供，不附带任何形式的明示或暗示的保证，包括但不限于对适销性、特定用途的适用性和不侵权的保证。在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论这些追责来自合同、侵权或其它行为中，还是产生于、源于或有关于本软件以及本软件的使用或其它处置。

## 致谢

- [Unity](https://unity.com/)
- [HybridCLR](https://github.com/focus-creative-games/hybridclr)
- [YooAsset](https://github.com/tuyoogame/YooAsset)
- [FairyGUI](https://www.fairygui.com/)

## 联系方式

如有问题或疑问，请在 GitHub 仓库创建 issue。

