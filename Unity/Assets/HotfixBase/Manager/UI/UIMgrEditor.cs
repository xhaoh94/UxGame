#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace Ux
{
    public partial class UIMgr
    {
        /// <summary>ID 到类型名的映射：只在编辑器里给错误日志和调试面板用。</summary>
        private readonly Dictionary<int, string> _idTypeName = new Dictionary<int, string>();
        private readonly Dictionary<string, IUIData> _debugAllUIData = new Dictionary<string, IUIData>();
        private readonly List<string> _debugShowedUI = new List<string>();
        private readonly List<UIStack> _debugUIStacks = new List<UIStack>();
        private readonly List<string> _debugCacheUI = new List<string>();
        private readonly List<string> _debugWaitDelUI = new List<string>();
        private readonly List<string> _debugWindows = new List<string>();

        /// <summary>
        /// 调试器事件触发函数（仅编辑器模式）
        /// 在游戏运行时触发所有调试回调，用于UI调试面板显示
        /// </summary>
        public static void __Debugger_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                ((IUIMgrDebuggerAccess)Ins).FillAllUIData(Ins._debugAllUIData);
                __Debugger_Stack_Event();
                __Debugger_Showed_Event();                
                __Debugger_Cacel_Event();
                __Debugger_WaitDel_Event();
                __Debugger_Windows_Event();
            }
        }

        /// <summary>
        /// 调试器UI事件（仅编辑器模式）
        /// 通知调试面板UI注册信息已更新
        /// </summary>
        public static void __Debugger_UI_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                var access = (IUIMgrDebuggerAccess)Ins;
                access.FillAllUIData(Ins._debugAllUIData);
                __Debugger_UI_CallBack?.Invoke(Ins._debugAllUIData);
            }
        }

        /// <summary>
        /// 调试器已显示UI事件（仅编辑器模式）
        /// 通知调试面板当前显示的UI列表
        /// </summary>
        public static void __Debugger_Showed_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                var access = (IUIMgrDebuggerAccess)Ins;
                access.FillShowedUI(Ins._debugShowedUI);
                __Debugger_Showed_CallBack?.Invoke(Ins._debugShowedUI);
            }
        }

        /// <summary>
        /// 调试器UI栈事件（仅编辑器模式）
        /// 通知调试面板UI栈信息
        /// </summary>
        public static void __Debugger_Stack_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                var access = (IUIMgrDebuggerAccess)Ins;
                access.FillUIStacks(Ins._debugUIStacks);
                __Debugger_Stack_CallBack?.Invoke(Ins._debugUIStacks);
            }
        }


        /// <summary>
        /// 调试器缓存UI事件（仅编辑器模式）
        /// 通知调试面板当前缓存的UI列表
        /// </summary>
        public static void __Debugger_Cacel_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                var access = (IUIMgrDebuggerAccess)Ins;
                access.FillCacheUI(Ins._debugCacheUI);
                __Debugger_Cacel_CallBack?.Invoke(Ins._debugCacheUI);
            }
        }

        /// <summary>
        /// 调试器等待销毁UI事件（仅编辑器模式）
        /// 通知调试面板等待销毁的UI列表
        /// </summary>
        public static void __Debugger_WaitDel_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                var access = (IUIMgrDebuggerAccess)Ins;
                access.FillWaitDelUI(Ins._debugWaitDelUI);
                __Debugger_WaitDel_CallBack?.Invoke(Ins._debugWaitDelUI);
            }
        }

        /// <summary>
        /// 调试器窗口注册表事件（仅编辑器模式）
        /// </summary>
        public static void __Debugger_Windows_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                var access = (IUIMgrDebuggerAccess)Ins;
                access.FillWindows(Ins._debugWindows);
                __Debugger_Windows_CallBack?.Invoke(Ins._debugWindows);
            }
        }

        /// <summary>
        /// UI数据调试回调（仅编辑器模式）
        /// </summary>
        public static Action<Dictionary<string, IUIData>> __Debugger_UI_CallBack;
        
        /// <summary>
        /// 已显示UI调试回调（仅编辑器模式）
        /// </summary>
        public static Action<List<string>> __Debugger_Showed_CallBack;
        
        /// <summary>
        /// UI栈调试回调（仅编辑器模式）
        /// </summary>
        public static Action<List<UIStack>> __Debugger_Stack_CallBack;
        
        /// <summary>
        /// 缓存UI调试回调（仅编辑器模式）
        /// </summary>
        public static Action<List<string>> __Debugger_Cacel_CallBack;
        
        /// <summary>
        /// 等待销毁UI调试回调（仅编辑器模式）
        /// </summary>
        public static Action<List<string>> __Debugger_WaitDel_CallBack;

        /// <summary>
        /// 窗口注册表调试回调（仅编辑器模式）
        /// </summary>
        public static Action<List<string>> __Debugger_Windows_CallBack;
        
        /// <summary>
        /// 资源包引用调试回调（仅编辑器模式）
        /// </summary>

        /// <summary>
        /// 全量校验 UI 管理器的内部索引是否自洽。
        /// 一次把所有派生索引（可见表 / 类型索引 / 根链路索引 / 栈 / 缓存 / 焦点）和主表对一遍，
        /// 出错的都是「隐藏不掉、界面错位、缓存泄漏」这类很难复现的问题，不如在编辑器里主动查。
        /// </summary>
        public void __DebugAssertAllInvariants(string context = "手动检查")
        {
            var errors = 0;
            var warnings = 0;

            void Error(string message)
            {
                errors++;
                Log.Error("[UI状态校验][{0}] {1}", context, message);
            }

            void Warn(string message)
            {
                warnings++;
                Log.Warning("[UI状态校验][{0}] {1}", context, message);
            }

            // 1. 可见表 <-> 记录表
            AssertShowedConsistency(context);

            // 2. 类型索引 <-> 记录表
            var instanceCount = 0;
            foreach (var kv in _typeInstances)
            {
                var list = kv.Value;
                if (list == null || list.Count == 0)
                {
                    Error($"类型索引 {kv.Key} 是空列表（应在没有实例时移除键）。");
                    continue;
                }

                for (int i = 0; i < list.Count; i++)
                {
                    instanceCount++;
                    if (!_records.TryGetValue(list[i], out var record) || record == null)
                    {
                        Error($"类型索引 {kv.Key} 里记录了不存在的实例 {list[i]}。");
                        continue;
                    }

                    if (record.TypeId != kv.Key)
                    {
                        Error($"实例 {list[i]} 挂在类型索引 {kv.Key} 下，但记录 TypeId 是 {record.TypeId}。");
                    }
                }
            }

            if (instanceCount != _records.Count)
            {
                Error($"类型索引实例数 {instanceCount} 与记录数 {_records.Count} 不一致。");
            }

            // 3. 根链路索引 <-> 记录表
            foreach (var kv in _rootRecordIds)
            {
                foreach (var childId in kv.Value)
                {
                    if (!_records.TryGetValue(childId, out var child) || child == null)
                    {
                        Error($"根链路索引 {kv.Key} 里记录了不存在的实例 {childId}。");
                        continue;
                    }

                    if (child.ParentRootId != kv.Key)
                    {
                        Error($"实例 {childId} 在根链路索引 {kv.Key} 下，但 ParentRootId 是 {child.ParentRootId}。");
                    }
                }
            }

            foreach (var kv in _records)
            {
                var record = kv.Value;
                if (record == null)
                {
                    Error($"记录表存在空记录，键 {kv.Key}。");
                    continue;
                }

                if (record.Id != kv.Key)
                {
                    Error($"记录表键 {kv.Key} 与记录 Id {record.Id} 不一致。");
                }

                if (record.TypeId < TypeIdBase)
                {
                    Error($"实例 {record.Id} 的 TypeId {record.TypeId} 不合法（应 >= {TypeIdBase}）。");
                }

                if (!_rootRecordIds.TryGetValue(record.ParentRootId, out var rootSet) || !rootSet.Contains(record.Id))
                {
                    Warn($"实例 {record.Id} 没有登记在根链路 {record.ParentRootId} 的反向索引里。");
                }

                if (record.Phase == UIPhase.Visible && !_showed.ContainsKey(record.Id))
                {
                    Error($"实例 {record.Id} 处于 Visible 但不在可见表里。");
                }

                if (record.Phase != UIPhase.Visible && _showed.ContainsKey(record.Id) && record.Phase != UIPhase.Showing && record.Phase != UIPhase.Hiding)
                {
                    Error($"实例 {record.Id} 在可见表里，但阶段是 {record.Phase}。");
                }

                if (record.Phase == UIPhase.Disposed && record.UI != null)
                {
                    Error($"实例 {record.Id} 已 Disposed 但仍持有 UI 引用。");
                }
            }

            // 4. 栈条目
            for (int i = 0; i < _stackEntries.Count; i++)
            {
                var entry = _stackEntries[i];
                if (!_records.ContainsKey(entry.RootId))
                {
                    Error($"栈条目 {i} 的 RootId {entry.RootId} 找不到对应记录。");
                }

                if (entry.ActiveTypeId < TypeIdBase)
                {
                    Error($"栈条目 {i} 的 ActiveTypeId {entry.ActiveTypeId} 不是类型 ID。");
                }
            }

            // 5. 焦点
            var focused = _focusHandler.GetFocusedUI();
            if (focused != null && !_showed.ContainsKey(focused.ID))
            {
                Error($"焦点停在未显示的实例 {focused.ID}（{focused.Name}）上。");
            }

            // 5.1 窗口注册表（顺序与焦点、上屏排序的一致性）
            AssertWindowInvariants(Error, Warn);

            // 6. 缓存 / 等待销毁
            var cacheIds = Pool.Get<List<int>>();
            try
            {
                _cacheHandler.FillCacheDebugIds(cacheIds);
                for (int i = 0; i < cacheIds.Count; i++)
                {
                    if (!_records.TryGetValue(cacheIds[i], out var cached) || cached == null)
                    {
                        Error($"缓存里的实例 {cacheIds[i]} 找不到对应记录。");
                    }
                }
            }
            finally
            {
                cacheIds.Clear();
                Pool.Push(cacheIds);
            }

            if (errors == 0 && warnings == 0)
            {
                Log.Info("[UI状态校验][{0}] 通过：记录 {1} 条，可见 {2} 条。", context, _records.Count, _showed.Count);
            }
            else
            {
                Log.Warning("[UI状态校验][{0}] 结束：错误 {1} 条，警告 {2} 条。", context, errors, warnings);
            }
        }

        /// <summary>
        /// 窗口不变式：注册表 ↔ 可见表 ↔ 上屏排序表，以及“焦点窗口在同 Tier 内最靠上”。
        /// 出错的都是“窗口点不上去/置顶后被打回”这类难复现问题。
        /// </summary>
        private void AssertWindowInvariants(Action<string> error, Action<string> warn)
        {
            var ids = Pool.Get<List<int>>();
            try
            {
                _windowService.FillWindowIds(ids);

                if (ids.Count > _windowService.MaxWindows)
                {
                    error($"窗口注册数 {ids.Count} 超过上限 {_windowService.MaxWindows}。");
                }

                for (int i = 0; i < ids.Count; i++)
                {
                    var id = ids[i];
                    if (!_windowService.TryGetWindow(id, out var window) || window == null)
                    {
                        error($"窗口注册表里的实例 {id} 取不到窗口对象。");
                        continue;
                    }

                    if (!_showed.ContainsKey(id))
                    {
                        error($"窗口实例 {id}（{window.GetType().Name}）已注册但不在可见表里。");
                    }

                    if (!_stageHandler.TryGetSorting(id, out _, out _))
                    {
                        warn($"窗口实例 {id}（{window.GetType().Name}）已注册但不在上屏排序表里。");
                    }
                }

                // 焦点窗口应当是所在 Tier 内 sortingOrder 最大的那个。
                var focusedId = _focusHandler.FocusedId;
                if (focusedId == 0 || !_windowService.IsRegistered(focusedId))
                    return;

                if (!_stageHandler.TryGetSorting(focusedId, out _, out var focusedOrder))
                {
                    warn($"焦点窗口 {focusedId} 不在上屏排序表里。");
                    return;
                }

                for (int i = 0; i < ids.Count; i++)
                {
                    var otherId = ids[i];
                    if (otherId == focusedId) continue;
                    if (!_stageHandler.TryGetSorting(otherId, out _, out var order)) continue;

                    var sameTier = order / UIStageHandler.TierSpan == focusedOrder / UIStageHandler.TierSpan;
                    if (sameTier && order > focusedOrder)
                    {
                        warn($"焦点窗口 {focusedId} 在同 Tier 内不是最上层（{otherId} 的 sortingOrder 更高）。");
                        break;
                    }
                }
            }
            finally
            {
                ids.Clear();
                Pool.Push(ids);
            }
        }

        /// <summary>
        /// 菜单入口：运行时手动触发一次 UI 状态自检。
        /// </summary>
        [UnityEditor.MenuItem("Tools/UI/检查 UI 状态一致性", false, 100)]
        private static void __MenuCheckUIInvariants()
        {
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                Log.Warning("[UI状态校验] 需要进入运行时才能检查（静态注册表阶段没有实例）。");
                return;
            }

            Ins.__DebugAssertAllInvariants("手动检查");
        }

        /// <summary>
        /// 菜单入口：把当前所有最小化窗口还原到 Normal。
        /// 任务栏/快捷键还没接，测试时用这个先把窗口找回来。
        /// </summary>
        [UnityEditor.MenuItem("Tools/UI/恢复所有最小化窗口", false, 104)]
        private static void __MenuRestoreMinimizedWindows()
        {
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                Log.Warning("[UI窗口] 需要进入运行时才能恢复最小化窗口。");
                return;
            }

            var count = 0;
            foreach (var kv in Ins._records)
            {
                if (kv.Value?.UI is UIWindow window && window.LayoutState == WindowState.Minimized)
                {
                    window.Restore();
                    count++;
                }
            }

            Log.Info("[UI窗口] 已恢复 {0} 个最小化窗口。", count);
        }

        private sealed class SmokeTarget
        {
            public int TypeId;
            public string Name;
            public IUIParam Param;
        }

        /// <summary>
        /// 菜单入口：冒烟测试 —— 把当前所有可见界面依次「关掉 → 用上次的参数重新打开 → 再关掉」，
        /// 每一步都跑一次全量索引自检。只验证框架状态机，不关心具体业务逻辑。
        /// </summary>
        [UnityEditor.MenuItem("Tools/UI/冒烟测试：可见界面 关->开->关", false, 102)]
        private static void __MenuRunVisibilitySmoke()
        {
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                Log.Warning("[UI冒烟] 需要进入运行时才能跑（先打开几个界面）。");
                return;
            }

            __DebugRunVisibilitySmoke().Forget();
        }

        private static async Cysharp.Threading.Tasks.UniTaskVoid __DebugRunVisibilitySmoke()
        {
            var targets = new List<SmokeTarget>();
            foreach (var kv in Ins._records)
            {
                var record = kv.Value;
                if (record == null || !record.IsVisibleCommitted || record.UI == null)
                {
                    continue;
                }

                // 只取根界面：关掉根界面会连带关掉它的页签子界面，
                // 逐个处理子界面会让序列依赖执行顺序，冒烟结果不可复现。
                if (record.ParentRootId != record.Id)
                {
                    continue;
                }

                targets.Add(new SmokeTarget
                {
                    TypeId = record.TypeId,
                    Name = record.UI.Name,
                    Param = record.LastShowParam,
                });
            }

            if (targets.Count == 0)
            {
                Log.Warning("[UI冒烟] 当前没有可见界面，先打开几个界面再跑。");
                return;
            }

            Log.Info("[UI冒烟] 开始，共 {0} 个可见界面。", targets.Count);
            Ins.__DebugAssertAllInvariants("冒烟:开始");

            var failed = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                var label = string.Format("{0}(类型{1})", target.Name, target.TypeId);

                // 1) 关掉
                Ins.Hide(target.TypeId, false);
                if (!await __WaitUntil(() => !__IsTypeVisible(target.TypeId), 3f))
                {
                    failed++;
                    Log.Error("[UI冒烟] {0} 关闭超时。", label);
                }

                Ins.__DebugAssertAllInvariants("冒烟:关闭 " + label);

                // 2) 用上次显示时的参数重新打开
                var ui = await Ins.Request(target.TypeId).SetParam(target.Param).SetAnim(false).Show().Task();
                if (ui == null || !await __WaitUntil(() => __IsTypeVisible(target.TypeId), 3f))
                {
                    failed++;
                    Log.Error("[UI冒烟] {0} 重新打开失败（Show 返回 {1}）。", label, ui == null ? "null" : "非空");
                }

                Ins.__DebugAssertAllInvariants("冒烟:打开 " + label);

                // 3) 再关掉，保证跑完不额外留下界面
                Ins.Hide(target.TypeId, false);
                if (!await __WaitUntil(() => !__IsTypeVisible(target.TypeId), 3f))
                {
                    failed++;
                    Log.Error("[UI冒烟] {0} 第二次关闭超时。", label);
                }

                Ins.__DebugAssertAllInvariants("冒烟:关闭2 " + label);
            }

            if (Ins._showed.Count > 0)
            {
                // 页签子界面被关掉后，它的根界面可能还在，这里只提示不判定失败。
                Log.Warning("[UI冒烟] 结束后仍有 {0} 个界面可见（通常是页签的根界面），要干净状态请再点一次 HideAll。",
                    Ins._showed.Count);
            }

            if (failed == 0)
            {
                Log.Info("[UI冒烟] 全部通过（{0} 个界面）。", targets.Count);
            }
            else
            {
                Log.Error("[UI冒烟] 结束，失败 {0} 项（共 {1} 个界面），详情见上方日志。", failed, targets.Count);
            }
        }

        private static bool __IsTypeVisible(int typeId)
        {
            var record = Ins.GetRecord(typeId);
            return record != null && record.IsVisibleCommitted;
        }

        private static async Cysharp.Threading.Tasks.UniTask<bool> __WaitUntil(Func<bool> condition, float timeoutSeconds)
        {
            var deadline = UnityEngine.Time.realtimeSinceStartup + timeoutSeconds;
            while (UnityEngine.Time.realtimeSinceStartup < deadline)
            {
                if (condition())
                {
                    return true;
                }

                await Cysharp.Threading.Tasks.UniTask.Yield();
            }

            return condition();
        }

        private static Action<UIRecord, UIPhase, UIPhase> __phaseLogger;
        private static bool __phaseLogEnabled;

        [UnityEditor.MenuItem("Tools/UI/记录 UI 阶段迁移", true)]
        private static bool __MenuTogglePhaseLogValidate()
        {
            return __phaseLogEnabled;
        }

        /// <summary>
        /// 菜单入口：开关「UI 阶段迁移日志」。
        /// 排查「界面卡住不显示 / 关不掉 / 闪烁」时先打开它，能直接看到每个实例的阶段序列。
        /// 默认关闭，避免正常开发时刷屏。
        /// </summary>
        [UnityEditor.MenuItem("Tools/UI/记录 UI 阶段迁移", false, 101)]
        private static void __MenuTogglePhaseLog()
        {
            __phaseLogEnabled = !__phaseLogEnabled;
            __phaseLogger ??= __LogPhaseChanged;

            if (__phaseLogEnabled)
            {
                OnPhaseChanged += __phaseLogger;
                Log.Info("[UI阶段] 已开启阶段迁移日志。");
            }
            else
            {
                OnPhaseChanged -= __phaseLogger;
                Log.Info("[UI阶段] 已关闭阶段迁移日志。");
            }
        }

        private static void __LogPhaseChanged(UIRecord record, UIPhase from, UIPhase to)
        {
            if (record == null)
            {
                return;
            }

            var name = record.UI != null ? record.UI.Name : "(未创建)";
            Log.Info("[UI阶段][{0}] {1} 实例{2}/类型{3} {4} -> {5}",
                UnityEngine.Time.frameCount, name, record.Id, record.TypeId, from, to);
        }

    }
}
#endif