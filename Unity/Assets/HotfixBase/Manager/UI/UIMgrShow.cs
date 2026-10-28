using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Ux
{
    public partial class UIMgr
    {
        /// <summary>
        /// 显示会话类，用于封装一次显示请求的所有相关数据
        /// </summary>
        private sealed class ShowSession
        {
            /// <summary>
            /// 请求显示的UI ID（类型 ID：调用方传进来的那个）
            /// </summary>
            public int RequestedId { get; private set; }
            
            /// <summary>
            /// 显示参数
            /// </summary>
            public IUIParam Param { get; private set; }
            
            /// <summary>
            /// 是否播放动画
            /// </summary>
            public bool IsAnim { get; private set; }
            
            /// <summary>
            /// 是否检查栈
            /// </summary>
            public bool CheckStack { get; private set; }

            /// <summary>
            /// 同类型已有可见实例时的处理策略
            /// </summary>
            public UIShowMode Mode { get; private set; }
            
            /// <summary>
            /// 显示链，包含从根界面到目标界面的所有UI ID
            /// </summary>
            public readonly List<int> Chain = new ();
            
            /// <summary>
            /// 已激活的UI记录列表
            /// </summary>
            internal readonly List<UIRecord> Activated = new ();

            /// <summary>
            /// 已激活记录对应的请求版本
            /// </summary>
            internal readonly List<int> ActivatedVersions = new ();

            internal int PreviousMountedChildId { get; private set; }

            /// <summary>
            /// 根界面记录 ID（实例 ID）。显示链准备阶段解析出来后填入。
            /// </summary>
            public int RootRecordId { get; set; }

            /// <summary>
            /// 目标界面记录 ID（实例 ID）。显示链准备阶段解析出来后填入。
            /// </summary>
            public int TargetRecordId { get; set; }
            
            /// <summary>
            /// 重置会话数据
            /// </summary>
            public void Reset(int requestedId, IUIParam param, bool isAnim, bool checkStack, UIShowMode mode)
            {
                RequestedId = requestedId;
                Param = param;
                IsAnim = isAnim;
                CheckStack = checkStack;
                Mode = mode;
                PreviousMountedChildId = 0;
                RootRecordId = 0;
                TargetRecordId = 0;
                Chain.Clear();
                Activated.Clear();
                ActivatedVersions.Clear();
            }

            public void CapturePreviousMountedChild(int id)
            {
                PreviousMountedChildId = id;
            }

            /// <summary>
            /// 释放会话资源
            /// </summary>
            public void Release()
            {
                RequestedId = 0;
                Param = null;
                IsAnim = true;
                CheckStack = true;
                Mode = UIShowMode.Reuse;
                PreviousMountedChildId = 0;
                RootRecordId = 0;
                TargetRecordId = 0;
                Chain.Clear();
                Activated.Clear();
                ActivatedVersions.Clear();
                Pool.Push(this);
            }
        }

        /// <summary>
        /// 创建UI显示构建器
        /// </summary>
        /// <param name="id">UI ID，默认为0</param>
        /// <returns>UI显示构建器实例</returns>
        public UIShowBuilder Create(int id = 0)
        {
            var builder = Pool.Get<UIShowBuilder>();
            builder.SetId(id);
            return builder;
        }

        /// <summary>
        /// 异步显示UI
        /// </summary>
        private async UniTask<T> _ShowAsync<T>(int id, IUIParam param, bool isAnim, bool checkStack, UIShowMode mode = UIShowMode.Reuse) where T : IUI
        {
            EnsureInitialized();
            var data = GetUIData(id);
            if (data == null)
            {
                return default;
            }

            // 获取目标ID（可能是Tab页的子界面）
            var targetId = data.GetChildID();
            
            // 检查是否需要下载资源
            if (_CheckDownload(targetId, param, isAnim))
            {
                return default;
            }

            // 同类型已有可见实例时，按调用方声明的策略处理，避免「静默覆盖」。
            if (mode == UIShowMode.SkipIfShown)
            {
                var shownRecord = GetRecord(targetId);
                if (shownRecord?.UI != null && (shownRecord.IsVisibleCommitted || shownRecord.IsShowingLike))
                {
                    return shownRecord.UI is T shown ? shown : default;
                }
            }
            else if (mode == UIShowMode.Replace)
            {
                var replaceRecord = GetRecord(targetId);
                if (replaceRecord?.UI != null && (replaceRecord.IsVisibleCommitted || replaceRecord.IsShowingLike))
                {
                    // 先等旧实例彻底关掉，再走一遍完整显示流程。
                    // 这里不检查栈：否则关掉的瞬间栈会尝试把「上一个界面」弹回来，形成自我复读。
                    await _Hide(targetId, false, false);
                }
            }
            // NewInstance 由显示链准备阶段处理（见 PrepareShowChain 的 forceNew）：
            // 只有「没有父级的根界面」（弹窗/飘字）才会真的新建实例；
            // 页签子界面挂在同一个根下，同一类型在一条根链路里只可能有一个活动实例，按 Reuse 处理。

            var session = Pool.Get<ShowSession>();
            session.Reset(id, param, isAnim, checkStack, mode);
            try
            {
                // 构建显示链
                BuildShowChain(targetId, session.Chain);
                var success = await RunShowSession(session);
                if (!success)
                {
                    return default;
                }

                var record = GetRecord(targetId);
                return record?.UI is T ui ? ui : default;
            }
            catch (Exception ex)
            {
                // 显示链路里的异常（创建实例失败、界面脚本自身抛错等）必须回滚：
                // 否则记录会永久停留在 Creating，IsShowingLike 一直为 true，之后既显示不了也隐藏不掉。
                Log.Error("显示UI异常，已回滚。Id[{0}] {1}", id, ex);
                await AbortShowSession(session, session.Activated.Count);
                return default;
            }
            finally
            {
                session.Release();
            }
        }

        /// <summary>
        /// 构建显示链，从根界面到目标界面的ID列表
        /// </summary>
        private void BuildShowChain(int targetId, List<int> chain)
        {
            chain.Clear();
            var data = GetUIData(targetId);
            if (data == null)
            {
                return;
            }

            // 获取父界面ID列表并反转（从根到目标）
            var parents = data.GetParentIDs();
            if (parents != null)
            {
                for (int i = parents.Count - 1; i >= 0; i--)
                {
                    chain.Add(parents[i]);
                }
            }
            chain.Add(targetId);
        }

        /// <summary>
        /// 运行显示会话，显示流程分为多个清晰的阶段
        /// 这样请求失效只会在少数几个关键点处理
        /// </summary>
        private async UniTask<bool> RunShowSession(ShowSession session)
        {
            session.CapturePreviousMountedChild(GetMountedRootChildId(session.RootRecordId));

            // 1. 确保父子链中的每个节点都有可用的实例
            if (!await PrepareShowChain(session))
            {
                return false;
            }

            if (!await EnsureShowSessionCurrent(session))
            {
                return false;
            }

            // 2. 如果目标UI需要模糊截图背景，在显示前截图
            var targetRecord = GetRecord(session.TargetRecordId);
            if (targetRecord?.UI != null)
            {
                await _blurHandler.PrepareBeforeShowAsync(targetRecord.UI);
            }

            if (!await EnsureShowSessionCurrent(session))
            {
                return false;
            }

            // 这里如果是页签界面。则先隐藏上一个页签
            HidePreviousRootChildBeforeShow(session);

            // 3. 同时启动每个节点的显示，不需要等待父动画完成
            if (!await StartShowChain(session))
            {
                return false;
            }

            // 4. 当请求的目标变为可见时，认为请求完成
            if (!await WaitForTargetVisible(session))
            {
                await AbortShowSession(session, session.Activated.Count);
                return false;
            }

            // 5. 在目标赢得竞态后，丢弃过期的完成状态
            if (!ValidateShowChain(session))
            {
                return false;
            }

            // 记录当前为哪个子页签
            CommitRootChildBinding(session);
            return true;
        }

        private void RestorePreviousRootChild(ShowSession session)
        {
            if (!CanRestorePreviousRootChild(session))
            {
                return;
            }

            var previousId = session.PreviousMountedChildId;
            if (previousId != 0)
            {
                var previousRecord = GetRecord(previousId);
                if (previousRecord != null && (previousRecord.IsVisibleCommitted || previousRecord.IsShowingLike))
                {
                    RegisterRecordToRoot(previousRecord, session.RootRecordId);
                    SetRootChildRequest(session.RootRecordId, previousId);
                    return;
                }
            }

            SetRootChildRequest(session.RootRecordId, session.RootRecordId);
        }

        private bool CanRestorePreviousRootChild(ShowSession session)
        {
            if (session == null)
            {
                return false;
            }

            var rootRecord = GetRecord(session.RootRecordId);
            if (rootRecord == null)
            {
                return false;
            }

            if (GetRequestedRootChildId(rootRecord) != session.TargetRecordId)
            {
                return false;
            }

            var count = Math.Min(session.Activated.Count, session.ActivatedVersions.Count);
            for (int i = 0; i < count; i++)
            {
                var record = session.Activated[i];
                if (record?.Id == session.RootRecordId)
                {
                    return IsRequestCurrent(record, session.ActivatedVersions[i]);
                }
            }

            return true;
        }

        private void HidePreviousRootChildBeforeShow(ShowSession session)
        {
            if (!IsShowSessionCurrent(session))
            {
                return;
            }

            var previousId = session.PreviousMountedChildId;
            if (previousId == 0 || previousId == session.RootRecordId || previousId == session.TargetRecordId)
            {
                return;
            }

            var previous = GetRecord(previousId);
            if (previous == null || previous.ParentRootId != session.RootRecordId || previous.UI == null)
            {
                return;
            }

            if (!previous.IsVisibleCommitted && !previous.IsShowingLike)
            {
                return;
            }

            BeginHideRecord(previous, false);
        }

        private async UniTask<bool> EnsureShowSessionCurrent(ShowSession session)
        {
            if (IsShowSessionCurrent(session))
            {
                return true;
            }

            await AbortShowSession(session, session.Activated.Count);
            return false;
        }

        private bool IsShowSessionCurrent(ShowSession session)
        {
            if (session == null)
            {
                return false;
            }

            if (session.Activated.Count != session.ActivatedVersions.Count)
            {
                return false;
            }

            for (int i = 0; i < session.Activated.Count; i++)
            {
                var record = session.Activated[i];
                var version = session.ActivatedVersions[i];
                if (!IsRequestCurrent(record, version))
                {
                    return false;
                }
            }

            return IsRootChildRequested(session.RootRecordId, session.TargetRecordId);
        }

        /// <summary>
        /// 在任何显示回调改变可见状态之前，构建或复用目标链所需的所有记录
        /// </summary>
        private async UniTask<bool> PrepareShowChain(ShowSession session)
        {
            var prepared = Pool.Get<List<UIRecord>>();
            try
            {
                // 第一遍：按「根 -> 目标」顺序准备记录并接好父链。
                // 必须先全部建好再绑根链路，否则绑定时还不知道目标记录 Id。
                for (int i = 0; i < session.Chain.Count; i++)
                {
                    var nodeTypeId = session.Chain[i];
                    var forceNew = session.Mode == UIShowMode.NewInstance && i == session.Chain.Count - 1 && session.Chain.Count == 1;
                    var record = GetOrCreateRecord(nodeTypeId, forceNew);

                    if (i == 0)
                    {
                        // 根界面指向自己
                        record.ParentRootId = record.Id;
                        session.RootRecordId = record.Id;
                    }
                    else
                    {
                        record.ParentRootId = prepared[i - 1].Id;
                    }

                    prepared.Add(record);
                    if (i == session.Chain.Count - 1)
                    {
                        session.TargetRecordId = record.Id;
                    }
                }

                // 第二遍：开始显示事务、绑定根链路请求、准备实例。
                for (int i = 0; i < prepared.Count; i++)
                {
                    var record = prepared[i];
                    var version = BeginShowTransition(record);
                    BindRecordToRootRequest(record, session.RootRecordId, session.TargetRecordId);
                    record.LastShowParam = record.TypeId == session.RequestedId ? session.Param : null;
                    if (!await PrepareRecordForShow(record, version))
                    {
                        CompletePendingShow(record, false);
                        RestorePreviousRootChild(session);
                        await AbortShowSession(session, i);
                        return false;
                    }

                    session.Activated.Add(record);
                    session.ActivatedVersions.Add(version);
                }

                return true;
            }
            finally
            {
                prepared.Clear();
                Pool.Push(prepared);
            }
        }

        /// <summary>
        /// 父子界面显示是连续启动的，只等待目标界面变为可见
        /// </summary>
        private async UniTask<bool> StartShowChain(ShowSession session)
        {
            for (int i = 0; i < session.Activated.Count; i++)
            {
                var record = session.Activated[i];
                if (!IsShowSessionCurrent(session))
                {
                    RestorePreviousRootChild(session);
                    await AbortShowSession(session, session.Activated.Count);
                    return false;
                }

                // 已经在显示（或正在播放显示动画）的节点只是被父链重新带上，按「重新激活」处理：
                // 不重发 UI_SHOW、不重放动画，也不把它的阶段降级成 Showing。
                var reason = record.IsVisibleCommitted || record.Phase == UIPhase.Showing
                    ? UIShowReason.Reactivate
                    : UIShowReason.FirstShow;

                if (reason == UIShowReason.FirstShow)
                {
                    SetPhase(record, UIPhase.Showing);
                    record.LastShowStartTime = Time.unscaledTime;
                }

                if (i == session.Activated.Count - 1 && session.RootRecordId != 0)
                {
                    MarkRootChildMounted(session.RootRecordId, record.Id);
                }

                await record.UI.DoShow(session.IsAnim, session.RequestedId,
                    record.TypeId == session.RequestedId ? session.Param : null, session.CheckStack, reason);

                if (reason == UIShowReason.Reactivate && record.IsVisibleCommitted)
                {
                    // 已经提交可见的节点不会再走 OnShow 回调，这里补上等待句柄，避免留下悬空的完成源。
                    // 仍在播显示动画的节点不补：它的 OnShow 回调马上会到，补了会让调用方在
                    // 界面真正上屏之前就拿到「显示成功」，后面的 WaitForTargetVisible 会误判失败。
                    CompletePendingShow(record, true);
                }

                if (!IsShowSessionCurrent(session))
                {
                    RestorePreviousRootChild(session);
                    await AbortShowSession(session, session.Activated.Count);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 等待目标界面变为可见
        /// </summary>
        private async UniTask<bool> WaitForTargetVisible(ShowSession session)
        {
            if (session.Activated.Count == 0)
            {
                return false;
            }

            var targetRecord = session.Activated[session.Activated.Count - 1];
            if (targetRecord.IsVisibleCommitted)
            {
                return IsShowSessionCurrent(session);
            }

            if (targetRecord.PendingShow != null)
            {
                await targetRecord.PendingShow.Task;
            }
            if (!IsShowSessionCurrent(session))
            {
                RestorePreviousRootChild(session);
                return false;
            }

            // 只有等待对象被提前完成、或者 DoShow 被界面内部排队丢弃时才会走到这里而界面并未上屏。
            // 不能把「没有等待对象」当成「已经显示成功」，否则调用方会拿到一个还没可见的界面，
            // 根链的子页签绑定也会被提前提交。
            if (!targetRecord.IsVisibleCommitted)
            {
                RestorePreviousRootChild(session);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 一旦目标赢得竞态，链中的每个记录仍然必须属于同一请求版本
        /// </summary>
        private bool ValidateShowChain(ShowSession session)
        {
            if (IsShowSessionCurrent(session))
            {
                return true;
            }

            RestorePreviousRootChild(session);
            AbortShowSession(session, session.Activated.Count).Forget();
            return false;
        }

        /// <summary>
        /// 准备用于显示的UI记录
        /// </summary>
        private async UniTask<bool> PrepareRecordForShow(UIRecord record, int version)
        {
            // 如果UI已存在且可见，直接返回
            if (record.UI != null && (record.IsVisibleCommitted || record.IsShowingLike))
            {
                return true;
            }

            // 等待之前的隐藏完成
            if (record.PendingHide != null)
            {
                // 后续的显示可以在之前的隐藏完成后合法地重用同一记录
                await record.PendingHide.Task;
            }

            if (!IsRequestCurrent(record, version))
            {
                return false;
            }

            // 尝试从缓存中获取
            if (_cacheHandler.TryTakeCached(record.Id, out var cached))
            {
                record.UI = cached;
                SetPhase(record, UIPhase.Hidden);
                record.WaitDel = null;
                record.CacheState = CacheState.None;
                return IsRequestCurrent(record, version);
            }

            if (record.UI != null)
            {
                return IsRequestCurrent(record, version);
            }

            // 创建新的UI实例
            if (!TryGetUIData(record.TypeId, out var uiData) || uiData.CType == null)
            {
                Log.Error("创建UI失败，找不到对应的UIData。TypeId[{0}] InstanceId[{1}]", record.TypeId, record.Id);
                SetPhase(record, UIPhase.Idle);
                return false;
            }

            // 提前缓存资源包名：动态 UIData 可能在销毁前就被移除，
            // 销毁时再靠 UIData 反查会拿不到包名，资源包引用就永远减不掉。
            record.Pkgs ??= uiData.Pkgs;
            SetPhase(record, UIPhase.Creating);
            var created = await CreateUI(uiData, record.Id);
            if (!IsRequestCurrent(record, version))
            {
                if (created != null)
                {
                    _cacheHandler.TrackHidden(created, record.Id);
                }
                return false;
            }

            if (created == null)
            {
                SetPhase(record, UIPhase.Idle);
                return false;
            }

            record.UI = created;
            SetPhase(record, UIPhase.Hidden);
            return true;
        }

        /// <summary>
        /// 中止显示会话，回滚部分显示的UI
        /// </summary>
        private async UniTask AbortShowSession(ShowSession session, int activatedCount)
        {
            var count = Math.Min(activatedCount, Math.Min(session.Activated.Count, session.ActivatedVersions.Count));
            for (int i = count - 1; i >= 0; i--)
            {
                var record = session.Activated[i];
                var version = session.ActivatedVersions[i];
                if (record == null)
                {
                    continue;
                }

                if (!IsRequestCurrent(record, version))
                {
                    continue;
                }

                if (record.IsVisibleCommitted)
                {
                    continue;
                }

                if (record.IsShowingLike)
                {
                    BeginHideRecord(record, false);
                    continue;
                }

                if (record.UI == null)
                {
                    SetPhase(record, UIPhase.Hidden);
                    CompletePendingShow(record, false);
                    continue;
                }

                SetPhase(record, UIPhase.Hidden);
                _cacheHandler.TrackHidden(record);
                CompletePendingShow(record, false);
            }

            await UniTask.CompletedTask;
        }

        /// <summary>
        /// 记录当前为哪个子页签
        /// </summary>
        private void CommitRootChildBinding(ShowSession session)
        {
            for (int i = 0; i < session.Activated.Count; i++)
            {
                var record = session.Activated[i];
                BindRecordToRootRequest(record, session.RootRecordId, session.TargetRecordId);
            }

#if UNITY_EDITOR
            ValidateSingleRootMountedChild(session.RootRecordId, session.TargetRecordId);
#endif
        }

#if UNITY_EDITOR
        private void ValidateSingleRootMountedChild(int rootId, int expectedChildId)
        {
            var count = 0;
            var firstId = 0;
            foreach (var kv in _records)
            {
                var record = kv.Value;
                if (record == null || record.Id == rootId || record.ParentRootId != rootId)
                {
                    continue;
                }

                if (record.UI == null)
                {
                    continue;
                }

                if (!record.IsVisibleCommitted && record.Phase != UIPhase.Showing)
                {
                    continue;
                }

                count++;
                if (firstId == 0)
                {
                    firstId = record.Id;
                }
            }

            if (count > 1)
            {
                Log.Error("同一UI根节点下存在多个活动页签 RootId[{0}] Expected[{1}] First[{2}] Count[{3}]",
                    rootId, expectedChildId, firstId, count);
            }
        }
#endif

        /// <summary>
        /// Tab页从其根容器继承栈类型，而不是独立决定
        /// </summary>
        private UIType ResolveStackType(IUI ui, int rootId)
        {
            if (ui is not UITabView)
            {
                return ui.Type;
            }

            var rootRecord = GetRecord(rootId);
            return rootRecord?.UI?.Type ?? ui.Type;
        }

        /// <summary>
        /// 创建UI实例
        /// </summary>
        private async UniTask<IUI> CreateUI(IUIData data, int instanceId)
        {
            if (data == null || data.CType == null)
            {
                Log.Error("创建UI失败，UIData 或界面类型为空。");
                return null;
            }

            var loaded = false;
            try
            {
                // 加载UI资源包
                if (data.Pkgs is { Length: > 0 })
                {
                    if (!await ResMgr.Ins.LoadUIPackage(data.Pkgs))
                    {
                        Log.Error("[{0}]资源包加载失败", data.Name);
                        return null;
                    }

                    loaded = true;
                }

                // 创建UI实例
                var ui = (IUI)Activator.CreateInstance(data.CType);
                if (ui == null)
                {
                    Log.Error("[{0}]创建UI实例失败", data.Name);
                    return null;
                }

                ui.InitData(data, _initData, instanceId);
                return ui;
            }
            catch (Exception ex)
            {
                // 让异常在这里收敛为 null，由显示会话按失败流程回滚，
                // 避免异常穿透导致记录停在 Creating 状态。
                Log.Error("[{0}]创建UI异常 {1}", data.Name, ex);
                if (loaded)
                {
                    // 实例没建出来，把刚才加上的包引用还回去，否则这个包永远不会被卸载。
                    ResMgr.Ins.RemoveUIPackage(data.Pkgs);
                }

                return null;
            }
        }

        /// <summary>
        /// UI显示完成回调
        /// </summary>
        void _OnUIShown(IUI ui, IUIParam param, bool checkStack)
        {
            var record = GetRecord(ui.ID);
            if (record == null)
            {
                return;
            }

            if (!ReferenceEquals(record.UI, ui))
            {
                ui.DoHide(false, false);
                return;
            }

            var rootId = record.ParentRootId == 0 ? record.Id : record.ParentRootId;
            var activeId = GetActiveRootChildId(record);
            if (IsObsoleteShowCompletion(rootId, activeId))
            {
                RejectObsoleteShowCompletion(record);
                return;
            }

            // 附加到可见记录
            AttachVisibleRecord(record);
            record.LastVisibleTime = Time.unscaledTime;
            
            // 如果需要检查栈，将界面加入栈管理
            if (checkStack)
            {
                _stackHandler.CommitVisible(rootId, activeId, record.TypeId,
                    param, ResolveStackType(ui, rootId));
#if UNITY_EDITOR
                ValidateCurrentChild(rootId, activeId);
#endif
            }
            
            // 通知模糊效果处理器
            _blurHandler.OnShowed(ui);
            // 通知聚焦处理器
            _focusHandler.OnShowed(ui);
            CompletePendingShow(record, true);
        }

        private bool IsObsoleteShowCompletion(int rootId, int activeId)
        {
            if (rootId == 0 || activeId == 0)
            {
                return false;
            }

            if (!_records.ContainsKey(rootId))
            {
                return false;
            }

            return !IsRootChildRequested(rootId, activeId);
        }

        private void RejectObsoleteShowCompletion(UIRecord record)
        {
            if (record == null)
            {
                return;
            }

            BeginHideRecord(record, false);
        }
    }
}
