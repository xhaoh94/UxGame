# UI 框架可读性与低 GC 重构计划

## Summary
- 目标是在不移除现有功能的前提下，做一次框架级整理：`UIMgr` 按职责拆分，保留现有对外调用方式，并修复已确认的高置信问题。
- 范围包括 UI 管理、显示/隐藏流程、事件生命周期、Dialog/Tip 辅助工厂、Blur 处理和 Lazyload 下载检查。
- 不改变 UI 注册 ID、栈行为、缓存策略、FairyGUI 包引用计数和现有 `Show/Hide/GetUI/Dialog/Tip` 对外入口。

## Key Changes
- 将 `UIMgr` 拆成 partial 职责文件：核心初始化/注册、Show/Create、Hide、Lazyload/Download、DebuggerAccess；保留 `Ux.UIMgr` 类型和现有 public 方法。
- 优化 `UIMgr` 热点路径：`GetUI` 改为单次 `TryGetValue`；`HashSet` 用 `Add/Remove` 替代 `Contains + Add/Remove`；`HideAll` 去掉 `Func<int,bool>` 委托，使用复用的 ID 缓冲列表避免遍历 `_showed` 时同步移除导致异常。
- 修复事件生命周期：`UIEvent.AddEvent` 注册后必须记录 listener，`RemoveEvents/Release` 能真正清理普通点击事件，避免 UI 反复显示后回调叠加。
- 优化 `UIObject`：组件遍历直接检查 `_components`，避免无组件 UI 首次显示时因 `Components` 属性创建空 `List`；清理错误的 `CancellationTokenSource` 池化，改为取消后释放，避免“复用已取消 token”的假优化和资源滞留。
- 修复明确问题：`DialogData.WithBtn2` 正确写入 `Btn2`；仓库内 Dialog 示例调用改成链式或接收返回值。按你的选择，`DialogData` 保持当前不可变链式 API，不改成可变 builder。
- 降低小额 GC：`UIBlurHandler` 用反向 for 替代 `FindLastIndex` lambda，用位运算替代 `Enum.HasFlag`；`ResLazyload.GetDownloaderByTags` 去掉 LINQ `Where().ToArray()`，仅在确实需要下载时分配 tags 数组。
- 整理层级获取：用明确字段/switch 表达 Root/Bottom/Tip/Top，保持 `Normal/View` 回落到 `GRoot.inst` 的现有行为。

## Public Surface
- 保留现有 `UIMgr.Ins.Show/Hide/HideNotStack/HideAll/GetUI/GetLayer/Release/AddUIData/RemoveUIData` 签名。
- 可新增 `HideAll()` 无参重载，用来消除两个可选参数重载导致的潜在无参调用歧义；原 `HideAll(IList<int>)` 和 `HideAll(IList<Type>)` 继续可用。
- `DialogData` 继续是链式返回值语义：调用方必须使用 `.WithTitle(...).WithContent(...).Show()` 或接收返回值。
- 不新增运行时依赖，不改 UI 配置数据结构和 attribute 用法。

## Test Plan
- 编译验证：运行 Unity/C# 编译，确保 `Unity.HotfixBase`、`Unity.Hotfix`、Editor debugger 相关代码都通过。
- 显示/隐藏场景：连续 `Show/Hide` 同一个 UI、带 Tab 子界面的 UI、Stack UI 返回流程、`HideAll()`、`HideAll(ignoreIds)`、`HideAll(ignoreTypes)`。
- 事件场景：同一按钮反复打开关闭 UI 后点击一次只触发一次回调；多击、长按、列表 item click 仍正常。
- Dialog/Tip 场景：链式 Dialog 标题/内容/Btn1/Btn2/CheckBox 显示和回调正确；Tip 多个显示时位置调整仍正常。
- 资源场景：需要 Lazyload 时弹下载确认；下载成功后继续 Show；失败后不残留 `_idDownloader`；已加载 tags 不产生额外 LINQ 分配。
- 性能检查：用 Unity Profiler 对反复 Show/Hide、HideAll、Blur 切换做 GC Alloc 对比，确认重构后没有新增运行时分配热点。

## Assumptions
- 项目使用 Unity `6000.4.3f1` 和 C# 9，允许 partial 文件拆分和当前语法。
- 新增 Unity `.cs` 文件时一并提交 `.meta`，避免资源 GUID 缺失。
- 当前工作树里 `docs/uimgr-ui-framework-review.md` 已删除，视为用户既有改动，本次不恢复、不触碰。
