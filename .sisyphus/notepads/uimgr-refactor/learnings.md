# UIMgr 重构学习笔记

## 完成时间
2026-04-20

## 重构内容总结

### 创建的新文件
1. **UIResHandler.cs** - UI资源处理器
   - 包含 UIPkgRef 类
   - 包含 UI 包加载、引用计数、纹理加载逻辑

2. **UIStackHandler.cs** - UI栈处理器
   - 包含 UI 栈管理逻辑
   - 持有 UIMgr 引用以解决反向依赖

### 修改的主要文件
1. **UIMgr.cs**
   - 去除 partial 关键词
   - 实例化 UIResHandler 和 UIStackHandler
   - 添加供 Handler 调用的公共方法
   - 合并 UIData 和 UIMgrBlur 逻辑

2. **UIMgrEditor.cs**
   - 更新引用路径：_pkgToRef → ResHandler.PkgToRef
   - 更新引用路径：_uiStacks → StackHandler.UiStacks

### 保留 partial 的文件（必要）
- **UIMgrDefine.cs** - 包含嵌套类型定义（CallBackData, UIStack, BlurStack 等）
  - 被外部代码引用（如 UIDebuggerWindow.cs）
  - 这些类型需要通过 UIMgr.XXX 访问

- **UIMgrEditor.cs** - 部分调试方法

### 技术要点

#### 1. UIStackHandler 反向依赖解决方案
UIStackHandler 需要调用 UIMgr 的私有方法：
- `_ShowAsync<T>` → 改为公共方法 `ShowAsync()`
- `_showing.Contains()` → 改为公共方法 `IsShowing()`
- `_showed.TryGetValue()` → 改为公共方法 `TryGetShowed()`

#### 2. 编辑器调试事件连接
通过事件回调连接：
```csharp
ResHandler.__Debugger_Pkg_Event += __Debugger_Pkg_Event;
StackHandler.__Debugger_Stack_Event += __Debugger_Stack_Event;
```

### 验证状态
- [x] UIMgr.cs 不再是 partial class
- [x] UIResHandler.cs 存在
- [x] UIStackHandler.cs 存在
- [x] LSP 诊断无错误
- [ ] Unity 编译验证（待用户验证）

## 待处理
- 验证编译通过后，可能需要重新生成 HybridCLR 的 AOTGenericReferences.cs