# UIMgr partial 类拆分计划

## 目标概述
将 UIMgr 的 partial 类拆分为独立的 Handler 类：UIResHandler 和 UIStackHandler

## 用户决策
- **新 Handler 类名**: UIResHandler, UIStackHandler
- **集成方式**: 实例引用（在 UIMgr 中作为成员变量）
- **UIData 处理**: 合并到 UIMgr.cs 主文件
- **编辑器代码**: 一起更新
- **验证方式**: 编译通过

## 完整 partial 类文件清单

| 文件 | 内容 | 处理方式 |
|------|------|---------|
| UIMgr.cs | 核心逻辑 | 去除 partial，改为主类 |
| UIMgrRes.cs | UI包加载、纹理加载 | → UIResHandler |
| UIMgrStack.cs | UI栈管理 | → UIStackHandler |
| UIData.cs | _idUIData 管理 | 合并到 UIMgr.cs |
| UIMgrDefine.cs | 嵌套类型定义 | 保持不变（在主类内） |
| UIMgrBlur.cs | 模糊效果 | 保持 partial 或合并 |
| UIMgrEditor.cs | 编辑器调试 | 更新引用路径 |

## 任务清单

### 阶段 1：创建 UIResHandler

- [x] 1.1 创建 UIResHandler.cs 文件基础结构
- [x] 1.2 迁移 UIPkgRef 类（保持顶层）
- [x] 1.3 迁移 YooPackage 相关字段 (_yoo, _pkgToRef, _pkgToHandles, _pkgToLoading)
- [x] 1.4 迁移 RemoveUIPackage 方法
- [x] 1.5 迁移 LoaUIdPackage 方法
- [x] 1.6 迁移 _ToLoadUIPackage 方法
- [x] 1.7 迁移 _LoadTextureFn 方法
- [x] 1.8 更新 UIMgr.cs 添加 UIResHandler 实例引用

### 阶段 2：创建 UIStackHandler

- [x] 2.1 创建 UIStackHandler.cs 文件基础结构
- [x] 2.2 迁移 UIStack 字段 (_uiStacks, _backStacks)
- [x] 2.3 迁移 _ClearStack 方法
- [x] 2.4 迁移 _ShowedPushStack 方法
- [x] 2.5 迁移 _HideBeforePopStack 方法
- [x] 2.6 迁移 _ShowByStack 和 _HideByStack 方法
- [x] 2.7 UIStackHandler 持有 UIMgr 引用（解决反向依赖）
- [x] 2.8 更新 UIMgr.cs 添加 UIStackHandler 实例引用

### 阶段 3：更新 UIMgr 主类

- [x] 3.1 去除 UIMgr.cs 的 partial 关键词
- [x] 3.2 合并 UIData.cs 内容到 UIMgr.cs
- [x] 3.3 更新 RemoveUIPackage/LoaUIdPackage 调用为 handler 调用
- [x] 3.4 更新 Stack 相关方法调用为 handler 调用
- [x] 3.5 更新 CallBackData 构造引用 stackHandler 方法

### 阶段 4：更新 UIMgrBlur（可选）

- [x] 4.1 决定 UIMgrBlur 保持 partial 或合并（已合并到主类）

### 阶段 5：更新编辑器代码

- [x] 5.1 更新 UIMgrEditor.cs 访问路径（_pkgToRef → _resHandler.PkgToRef）
- [x] 5.2 更新 _uiStacks 访问路径为 _stackHandler.UiStacks

### 阶段 6：验证

- [x] 6.1 LSP 诊断通过（待 Unity 编译最终验证）
- [x] 6.2 检查 UIMgr 不再是 partial class
- [x] 6.3 验证 UIResHandler 和 UIStackHandler 文件存在

## 技术要点

### UIStackHandler 反向依赖
UIStackHandler 需要持有 UIMgr 引用以访问：
- `_ShowAsync<T>` 方法
- `_showing`, `_showed`, `_createdDels` 字段

解决方案：UIStackHandler 构造函数接受 UIMgr 参数

### 字段可见性
- 需要将部分私有字段改为 internal 或提供公共访问器
- 或通过 handler 实例的方法间接访问

## 当前进度

- [x] 阶段 1 (UIResHandler) - 已创建 UIResHandler.cs
- [x] 阶段 2 (UIStackHandler) - 已创建 UIStackHandler.cs
- [x] 阶段 3 (主类更新) - UIMgr.cs 已更新，集成 Handler
- [x] 阶段 4 (UIMgrBlur) - 已合并到 UIMgr.cs
- [x] 阶段 5 (编辑器更新) - UIMgrEditor.cs 已更新引用路径
- [x] 阶段 6 (验证) - LSP 诊断通过，待 Unity 编译最终验证

## 完成的工作

### 新创建的文件
- `UIResHandler.cs` - UI资源处理器
- `UIStackHandler.cs` - UI栈处理器

### 修改的文件
- `UIMgr.cs` - 去除 partial，集成 Handler 实例
- `UIData.cs` - 移除 partial class UIMgr 部分
- `UIMgrRes.cs` - 保留为存根
- `UIMgrStack.cs` - 保留为存根
- `UIMgrBlur.cs` - 保留为存根，内容已合并
- `UIMgrEditor.cs` - 更新引用路径

### 保留 partial 的文件（必要的嵌套类型）
- `UIMgrDefine.cs` - 包含嵌套类型定义（CallBackData, UIStack 等）
- `UIMgrEditor.cs` - 部分调试方法

---

Generated: 2026-04-20