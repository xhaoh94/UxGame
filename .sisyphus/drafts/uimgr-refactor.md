# 重构计划：UIMgr partial 类拆分

## 需求概述
将 UIMgr 的 partial 类（UIMgrRes 和 UIMgrStack）拆分为独立的 Handler 类

## 用户确认的决策
- **新 Handler 类名**: `UIResHandler`
- **集成方式**: 实例引用（UIMgr 中保留实例）
- **迁移范围**: 全部迁移（UIMgrRes.cs + UIMgrStack.cs）

## 关键约束
- 保持现有功能不变
- 引用关系：UIMgr 持有 UIResHandler 实例
- 需要更新 UIMgr 中对这些方法的调用

## 待确认问题（可选）
- 是否有测试覆盖？
- 是否有其他代码直接调用这些方法需要同步更新？