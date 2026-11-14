# UxGame 框架

一个基于 Unity 6 的客户端游戏开发框架：集成 HybridCLR 热更新、YooAsset 资源管理、Unity UGUI 界面体系（含高性能虚拟列表），并提供 Luban 配置生成与 Obfuz 代码加固工具链。

## 目录

- [功能特性](#功能特性)
- [前置要求](#前置要求)
- [安装说明](#安装说明)
- [设置指南](#设置指南)
- [使用示例](#使用示例)
  - [事件系统示例](#事件系统示例)
  - [网络模块示例](#网络模块示例)
  - [资源管理示例](#资源管理示例)
  - [时间系统示例](#时间系统示例)
  - [UI系统示例](#ui系统示例)
  - [虚拟列表示例](#虚拟列表示例)
  - [公式解析器示例](#公式解析器示例)
- [架构设计](#架构设计)
- [项目文档](#项目文档)
- [许可证](#许可证)

## 功能特性

### HybridCLR + YooAsset 集成
- 使用 HybridCLR 实现 C# 热更新
- 使用 YooAsset 实现资源热更新
- 一键构建到指定目录
- 资源差异分析
- 本地资源服务器 (DHFS) 支持编辑器热更新测试
- 集成 Luban 配置表生成流程（`DataTables/luban.conf` + `gen.bat` / `gen.sh`）
- 可选 Obfuz 代码加固（`manifest.json` 已接入，按需开启）

### 事件系统
- 独立的事件系统，包含默认系统与可创建的多事件系统实例（用于限流隔离）
- 事件队列带每帧触发上限，防止掉帧
- 支持同步调用与异步等待（`Call` 返回 UniTask）
- 按对象标签一键取消所有事件监听
- 可视化事件监控工具

### 网络模块
- 集成 TCP、KCP 和 WebSocket 协议
- 自定义 RPC 流程，实现简洁的请求-响应代码
- 可配置数据序列化的大小端

### 资源管理
- 封装 YooAsset 加载接口（同步/异步、原生文件、场景）
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

### UI系统
- 基于 Unity UGUI（项目已从 FairyGUI 迁移到 UGUI，第三方 FairyGUI 已移除）
- 支持异步加载和懒加载
- UI 栈管理
- UI 动画和模糊效果
- 界面嵌套支持
- 可视化代码生成和事件绑定工具（选中 Prefab 后执行 `Assets > UxGame > 生成 UI 绑定代码`）
- UIRelation 关联布局：边对距离 / 宽高比例跟随，支持 Inspector 配置与运行时 API
- 多种 UI 组件：
  - UIView（常规界面）
  - UIWindow（弹窗界面）
  - UITabView（嵌套界面）
  - UITip（提示语）
  - UIDialog（通用对话框，支持标题、双按钮、不再提示）
  - UIModel / RTModel（2D 中显示 3D 模型）
  - UIList（列表封装，内部使用虚拟列表）

### 虚拟列表
- 纯 C# 核心 `UIVirtualList` + 可选组件 `UIVirtualListComponent`（Add Component → `Ux/UI/UI Virtual List`）
- 数据类型：普通列表 / 树形列表 / 分页列表（页号、游标）/ 分组列表（支持单级吸顶组头）
- 尺寸模式：固定 / 可变（内容驱动高度），支持单列/多列、竖向/横向
- 无限循环列表（周期无缝滚动与吸附）
- 增量数据接口：`AddData` / `InsertDatas` / `RemoveRange` / `UpdateData`；另有保持滚动位置的 `UpdateDatas`
- 性能机制：已测尺寸复用、分批创建（帧预算）、测量收敛限流、对象池复用、增量数据不重建可见项
- 交互能力：单选/多选（可按业务 Id 保持选中）、平滑滚动导航、轮播吸附、绘制顺序与单项提权
- 模板支持：场景对象 / Prefab / 运行时异步加载（`UIVirtualListTemplateCache` + YooAsset）
- 详细文档见 [虚拟列表 README](Unity/Assets/HotfixBase/Manager/UI/Common/VirtualList/README.md)

### 公式解析器
- 使用 AST 和对象池实现高性能字符串公式解析
- 支持自定义函数和变量
- 内置数学函数

### 开发中功能
- Timeline 战斗编辑工具
- 条件系统（需根据游戏定制）
- Tag 红点系统

## 前置要求

- Unity 6.3（6000.3.28f1）或更新版本（URP 渲染管线）
- HybridCLR 9.1.0、UGUI 2.0.0
- YooAsset 3.0.7（以嵌入包形式随仓库提供）
- 可选：Luban（配置表）、Obfuz 4.0.0（代码加固）
- Visual Studio 2022 / JetBrains Rider（用于开发）

## 安装说明

1. 克隆仓库：
   ```bash
   git clone https://github.com/xhaoh94/UxGame.git
   ```

2. 在 Unity 中打开项目：
   - 启动 Unity Hub
   - 点击 "添加" 并选择克隆项目下的 `Unity` 文件夹
   - 使用 Unity 6.3 或更新版本打开项目

3. 依赖说明：
   - HybridCLR、Luban、Obfuz 通过 `Unity/Packages/manifest.json` 中的 Git 依赖自动拉取
   - YooAsset 3.0.7 为嵌入包（`Unity/Packages/com.tuyoogame.yooasset`），随仓库提供，无需额外安装
   - 首次打开项目时 Unity 会自动解析并导入全部包

## 设置指南

### 1. 配置热更新

1. 打开 HybridCLR 设置窗口：`HybridCLR > Settings`
2. 根据需要配置热更新程序集设置
3. 构建热更新程序集：`HybridCLR > Build > Build Hot Update DLL`

### 2. 配置资源系统

1. 打开 YooAsset 设置：`YooAsset > Settings`
2. 配置资源包与采集规则
3. 构建资源包：`YooAsset > Build > Build AssetBundles`

### 3. 配置 UI 系统

1. 使用 Unity UGUI 制作界面 Prefab，根节点按框架约定挂载 `UIView` / `UIWindow` / `UITabView` 等脚本
2. 选中 Prefab 执行 `Assets > UxGame > 生成 UI 绑定代码`，生成字段绑定与事件绑定代码（生成规则可在 `UxGame > 工具 > UI > 代码生成规则` 中配置）
3. 列表类界面使用 `UIVirtualListComponent`（Add Component → `Ux/UI/UI Virtual List`），按 Inspector 配置 ScrollRect、模板与回调；详细说明见 [虚拟列表 README](Unity/Assets/HotfixBase/Manager/UI/Common/VirtualList/README.md)
4. 对话框与提示的默认类型在启动时注册：`UIMgr.Dialog.SetDefaultType<CommonDialog>()`、`UIMgr.Tip.SetDefaultType<CommonTip>()`

### 4. 配置网络设置

1. 在网络管理器中设置服务器地址和端口
2. 配置协议类型（TCP/KCP/WebSocket）
3. 如有需要，设置数据序列化的大小端

## 使用示例

### 事件系统示例

```csharp
// 注册事件（MainEventType 为框架内置事件；业务事件可扩展 EventType）
EventMgr.Ins.On<object>(MainEventType.UI_SHOW, this, data =>
{
    // 处理事件
});

// 触发事件（带一个参数）
EventMgr.Ins.Run(MainEventType.UI_SHOW, ui);

// 取消某个对象注册的全部事件
EventMgr.Ins.OffTag(this);
```

### 网络模块示例

```csharp
// 连接服务器
var socket = NetMgr.Ins.Connect(NetType.TCP, "127.0.0.1:8080", OnConnect);

// 发送消息（协议号以项目协议定义为准，这里用常量示例）
var loginReq = new LoginRequest { Username = "user", Password = "pass" };
NetMgr.Ins.Send(10001, loginReq);

// RPC 调用
var response = await NetMgr.Ins.Call<LoginRequest, LoginResponse>(10001, loginReq);
```

### 资源管理示例

```csharp
// 同步加载（YooAsset 定位地址由采集规则生成，例如“组名_文件名”）
var prefab = ResMgr.Ins.LoadAsset<GameObject>("Prefab_Role_Dummy");

// 异步加载（返回 UniTask）
var prefabAsync = await ResMgr.Ins.LoadAssetAsync<GameObject>("Prefab_Role_Dummy");

// 实例化与资源句柄由框架统一管理
var go = Instantiate(prefabAsync, transform);
```

### 时间系统示例

```csharp
// 间隔定时器（延迟 1 秒后开始，每 2 秒触发 1 次，共触发 5 次）
TimeMgr.Ins.Timer(2.0d, this, OnTimerTrigger)
    .FirstDelay(1.0d)  // 初始延迟时间
    .Repeat(5)         // 重复次数
    .OnComplete(OnTimerComplete)  // 完成回调
    .Build();          // 构建并启动

// 帧定时器（每 60 帧触发 1 次，共触发 10 次）
TimeMgr.Ins.Frame(60d, this, OnFrameTimerTrigger)
    .Repeat(10)
    .Build();

// 循环定时器
TimeMgr.Ins.Timer(1.0d, this, OnLoopTrigger).Loop().Build();

// Cron 定时器（每天中午 12 点触发）
TimeMgr.Ins.Cron("0 0 12 * * ?", this, OnDailyTrigger).Build();

private void OnTimerTrigger() => Log.Debug("定时器触发");
private void OnFrameTimerTrigger() => Log.Debug("帧定时器触发");
private void OnLoopTrigger() => Log.Debug("循环定时器触发");
private void OnDailyTrigger() => Log.Debug("Cron 定时器触发");
private void OnTimerComplete() => Log.Debug("定时器完成");
```

### UI系统示例

```csharp
// 显示 UI
UIMgr.Ins.Show<UIMain>();

// 带参数显示 UI（IUIParam 支持 1~3 个参数）
UIMgr.Ins.Show<UISettings>(IUIParam.Create("value1", 123));

// 隐藏 UI
UIMgr.Ins.Hide<UIMain>();

// 确认对话框（默认对话框类型在启动时通过 Dialog.SetDefaultType 注册）
UIMgr.Dialog.Create()
    .SetTitle("提示")
    .SetContent("确定要退出吗？")
    .SetBtn1Title("是").SetBtn1(() => Application.Quit())
    .SetBtn2Title("否")
    .Show(null, null);

// 轻提示
UIMgr.Tip.Show("保存成功");
```

### 虚拟列表示例

```csharp
using Ux;
using UnityEngine;
using UnityEngine.UI;

public sealed class MailPanel : MonoBehaviour
{
    [SerializeField] private UIVirtualListComponent _list;

    private void Start()
    {
        // 绑定可见项（每次绑定可能是新数据或复用旧对象，需完整覆盖显示状态）
        _list.ItemRenderer = (index, data, item) =>
        {
            item.transform.Find("Text").GetComponent<Text>().text = (string)data;
        };
        _list.ItemClicked += (index, data) => Log.Debug($"点击 {index}");

        // 全量替换（回到顶部）；UpdateDatas 则保持当前滚动位置
        _list.SetDatas(mails);

        // 增量追加一条，不重建现有可见项
        _list.AddData(newMail);
    }

    private void JumpTo(int index)
    {
        // 即时定位；ScrollTo(index, alignment, padding, durationSeconds) 支持平滑滚动
        _list.ScrollToView(index, UIVirtualListAlignment.Center);
    }
}
```

> 树形、分页、分组、循环、吸顶、选中、轮播吸附等完整用法见 [虚拟列表 README](Unity/Assets/HotfixBase/Manager/UI/Common/VirtualList/README.md)。

### 公式解析器示例

```csharp
// 基本计算
var result1 = EvalMgr.Ins.Parse("10 + 5 * 2"); // 返回 20

// 自定义变量和函数
EvalMgr.Ins.AddVariable("hp", 100);
EvalMgr.Ins.AddVariable("atk", 20);
EvalMgr.Ins.AddFunction("damage", args => args[0] * (1 + args[1] / 100.0));

var result2 = EvalMgr.Ins.Parse("damage(atk, 50)"); // 返回 30
```

## 架构设计

框架采用模块化架构，各模块职责清晰：

| 模块 | 职责 |
|---|---|
| Event | 事件系统（默认系统 + 多实例、每帧限流、同步/异步调用） |
| Net | TCP / KCP / WebSocket 与 RPC |
| Res | YooAsset 加载封装、资源句柄与懒加载 |
| Time | 间隔 / 帧 / 时间戳 / Cron 定时器 |
| UI | UGUI 界面栈、加载、动画、嵌套、代码生成 |
| VirtualList | 虚拟列表核心、布局、树 / 分页 / 分组 / 循环适配、选中与导航 |
| Eval | AST + 对象池公式解析 |
| HybridCLR | C# 热更新 |
| YooAsset | 资源热更新 |
| Luban / Obfuz | 配置表生成 / 代码加固（可选工具链） |

程序集分层（单向依赖，Hotfix 与 HotfixBase 逻辑均可热更）：

```
Assembly-CSharp（Assets/Hotfix，业务与热更逻辑）
        ↓
Unity.HotfixBase（Assets/HotfixBase，事件/网络/资源/时间/UI/虚拟列表等基础模块）
        ↓
Unity.Main（Assets/Main，启动、补丁、热更与基础设施）
```

## 项目文档

- [虚拟列表完整文档](Unity/Assets/HotfixBase/Manager/UI/Common/VirtualList/README.md)：组件配置、增量接口、树形/分页/分组、无限循环、滚动导航、吸顶、选中、异步模板与测量行为
- 虚拟列表演示：Play 前执行 `UxGame > 工具 > UI > 虚拟列表` 下的 `Create UI VirtualList Demo` / `Create UI Multi Column Demo` / `Create UI Tree Demo` / `Create UI Paging Demo` / `Create UI Grouped Demo` / `Create UI Loop Demo`
- 编辑器工具入口：`UxGame > 工具 > UI`（UI 打包规则、散图打包规则、代码生成规则、虚拟列表）

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
- [Luban](https://github.com/focus-creative-games/luban)
- [Obfuz](https://github.com/focus-creative-games/obfuz)
- [UniTask](https://github.com/Cysharp/UniTask)

## 联系方式

如有问题或疑问，请在 GitHub 仓库创建 issue。
