using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Ux
{
    public partial class UIMgr
    {
        /// <summary>
        /// 隐藏所有UI时的ID缓冲列表
        /// </summary>
        private readonly List<int> _hideAllIdBuffer = new List<int>();

        /// <summary>
        /// 隐藏会话类，用于封装一次隐藏请求的所有相关数据
        /// </summary>
        private sealed class HideSession
        {
            /// <summary>
            /// 请求隐藏的UI ID
            /// </summary>
            public int RequestedId { get; private set; }
            
            /// <summary>
            /// 根界面ID
            /// </summary>
            public int RootId { get; private set; }
            
            /// <summary>
            /// 是否播放动画
            /// </summary>
            public bool IsAnim { get; private set; }
            
            /// <summary>
            /// 是否检查栈
            /// </summary>
            public bool CheckStack { get; private set; }

            /// <summary>
            /// 隐藏目标列表
            /// </summary>
            internal readonly List<UIRecord> Targets = new List<UIRecord>();
            internal readonly List<int> TargetVersions = new List<int>();

            /// <summary>
            /// 重置会话数据
            /// </summary>
            public void Reset(int requestedId, int rootId, bool isAnim, bool checkStack)
            {
                RequestedId = requestedId;
                RootId = rootId;
                IsAnim = isAnim;
                CheckStack = checkStack;
                Targets.Clear();
                TargetVersions.Clear();
            }

            /// <summary>
            /// 释放会话资源
            /// </summary>
            public void Release()
            {
                RequestedId = 0;
                RootId = 0;
                IsAnim = true;
                CheckStack = true;
                Targets.Clear();
                TargetVersions.Clear();
                Pool.Push(this);
            }
        }

        /// <summary>
        /// 隐藏所有UI（内部方法）
        /// </summary>
        /// <param name="ignoreSet">需要忽略的UI ID集合</param>
        void HideAllInternal(HashSet<int> ignoreSet)
        {
            // 清空UI栈
            _stackHandler.Clear();
            _hideAllIdBuffer.Clear();

            // 先收集有 record 的可见/显示中 UI。
            foreach (var kv in _records)
            {
                var record = kv.Value;
                if (record.IsVisibleCommitted || record.IsShowingLike)
                {
                    _hideAllIdBuffer.Add(kv.Key);
                }
            }

            // 再补收只有 showed 但没有 record 的异常残留 UI，避免 HideAll 漏关。
            foreach (var kv in _showed)
            {
                if (_records.ContainsKey(kv.Key))
                {
                    continue;
                }

                var exists = false;
                for (int i = 0; i < _hideAllIdBuffer.Count; i++)
                {
                    if (_hideAllIdBuffer[i] == kv.Key)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    _hideAllIdBuffer.Add(kv.Key);
                }
            }

            // 隐藏收集到的UI
            for (int i = 0; i < _hideAllIdBuffer.Count; i++)
            {
                var id = _hideAllIdBuffer[i];
                if (ignoreSet != null)
                {
                    // 忽略集合里放的是「类型ID」（HideAllExceptTypes 的语义是按类型排除），
                    // 而缓冲里是「实例ID」，两种都要认。
                    var ignored = ignoreSet.Contains(id);
                    if (!ignored && _records.TryGetValue(id, out var buffered) && buffered != null)
                    {
                        ignored = ignoreSet.Contains(buffered.TypeId);
                    }

                    if (ignored)
                    {
                        continue;
                    }
                }

                // 有记录的走正常隐藏流程（不再依赖 UIData 是否存在），
                // 只有 _showed 中残留（没有记录）的才走兜底关闭。
                if (_records.ContainsKey(id))
                {
                    Hide(id, false);
                    continue;
                }

                ForceHideShowedOnly(id);
            }

            _hideAllIdBuffer.Clear();
            _blurHandler.ClearSnapshots();
#if UNITY_EDITOR
            AssertShowedConsistency("HideAllInternal");
#endif
        }

        /// <summary>
        /// 隐藏所有UI
        /// </summary>
        public void HideAll()
        {
            HideAllInternal(null);
        }

        /// <summary>
        /// 隐藏所有UI，排除指定ID的UI
        /// </summary>
        /// <param name="ids">需要排除的UI ID 列表</param>
        public void HideAllExcept(IList<int> ids)
        {
            if (ids == null || ids.Count == 0)
            {
                HideAllInternal(null);
                return;
            }

            // 用局部集合而不是字段：HideAll 期间可能被界面回调重入，
            // 复用字段会把外层的排除列表覆盖掉。
            var ignoreSet = Pool.Get<HashSet<int>>();
            try
            {
                for (int i = 0; i < ids.Count; i++)
                {
                    ignoreSet.Add(ids[i]);
                }

                HideAllInternal(ignoreSet);
            }
            finally
            {
                ignoreSet.Clear();
                Pool.Push(ignoreSet);
            }
        }

        /// <summary>
        /// 隐藏所有UI，排除指定类型的UI
        /// </summary>
        /// <param name="types">需要排除的UI类型列表</param>
        public void HideAllExceptTypes(IList<Type> types)
        {
            if (types == null || types.Count == 0)
            {
                HideAllInternal(null);
                return;
            }

            var ignoreSet = Pool.Get<HashSet<int>>();
            try
            {
                for (int i = 0; i < types.Count; i++)
                {
                    // 未注册的类型本来就没有界面在显示，不需要为它分配一个 Id。
                    if (TryGetTypeId(types[i], out var ignoreId))
                    {
                        ignoreSet.Add(ignoreId);
                    }
                }

                HideAllInternal(ignoreSet);
            }
            finally
            {
                ignoreSet.Clear();
                Pool.Push(ignoreSet);
            }
        }

        /// <summary>
        /// 隐藏指定类型的UI
        /// </summary>
        /// <typeparam name="T">UI类型</typeparam>
        /// <param name="isAnim">是否播放动画</param>
        public void Hide<T>(bool isAnim = true) where T : UIBase
        {
            if (TryGetTypeId(typeof(T), out var id))
            {
                Hide(id, isAnim);
            }
        }

        /// <summary>
        /// 隐藏指定ID的UI
        /// </summary>
        /// <param name="id">UI ID</param>
        /// <param name="isAnim">是否播放动画</param>
        public void Hide(int id, bool isAnim = true)
        {
            _Hide(id, isAnim, true).Forget();
        }

        /// <summary>
        /// 隐藏指定类型的UI（不检查栈）
        /// </summary>
        /// <typeparam name="T">UI类型</typeparam>
        /// <param name="isAnim">是否播放动画</param>
        public void HideNotStack<T>(bool isAnim = true) where T : UIBase
        {
            if (TryGetTypeId(typeof(T), out var id))
            {
                HideNotStack(id, isAnim);
            }
        }

        /// <summary>
        /// 隐藏指定ID的UI（不检查栈）
        /// </summary>
        /// <param name="id">UI ID</param>
        /// <param name="isAnim">是否播放动画</param>
        public void HideNotStack(int id, bool isAnim = true)
        {
            _Hide(id, isAnim, false).Forget();
        }

        /// <summary>
        /// 内部隐藏方法。返回 UniTask 而不是 UniTaskVoid，
        /// 便于 ShowMode.Replace 这种「先关掉再显示」的流程等待隐藏真正完成。
        /// </summary>
        private async UniTask _Hide(int id, bool isAnim, bool checkStack)
        {
            // 入口先把「类型ID」归一化成「实例ID」：外部调用可能传类型ID，
            // 而根链路里的 CurrentChildId / 记录表都是按实例ID比较的，混用会让隐藏静默失效。
            var requestId = ResolveRecordId(id);
            if (requestId == 0)
            {
                requestId = id;
            }

            // 注意：这里不再以 UIData 为入口。
            // 动态界面的 UIData 可能在 Release/RemoveUIData 时先于实例被移除，
            // 如果仍然依赖 UIData，这些界面就再也关不掉了（Hide 会直接 return）。
            var rootId = ResolveRootId(requestId);
            var rootRecord = GetRecord(rootId);
            if (rootRecord == null || rootRecord.IsHidingLike || rootRecord.Phase == UIPhase.Hidden)
            {
                return;
            }
            var session = Pool.Get<HideSession>();
            session.Reset(requestId, rootId, isAnim, checkStack);
            try
            {
                // 1. 在任何隐藏回调改变可见性之前，收集当前根链路的所有目标
                CollectHideTargets(rootId, session.Targets, requestId);
                if (session.Targets.Count == 0)
                {
                    return;
                }

                rootRecord = GetRecord(rootId);
                // 如果不是根界面且当前激活的子界面不是目标界面，则不隐藏
                if (rootId != requestId && rootRecord != null && !IsRootChildRequested(rootId, requestId))
                {
                    return;
                }

                ResetRootChildRequestIfCurrent(rootId, requestId);

                // 2. 先触发所有隐藏，使父子动画可以重叠播放
                BeginHideChain(session);

                // 3. 等待所有待处理的隐藏完成后，再处理栈回退
                await WaitHideChain(session);
                if (!IsHideSessionCurrent(session))
                {
                    return;
                }

                // 从栈中移除根界面
                _stackHandler.RemoveRoot(rootId);
                if (checkStack)
                {
                    // 尝试显示上一个界面
                    var previous = _stackHandler.PeekPrevious(rootId);
                    if (previous.HasValue)
                    {
                        _ShowAsync<IUI>(previous.Value.ActiveTypeId, previous.Value.Param, false, false).Forget();
                    }
                }
            }
            catch (Exception ex)
            {
                // 隐藏链路异常时把记录从 Hiding 复位，
                // 否则 IsHidingLike 会一直为 true，这条根链上后续的 Hide 全都会被跳过。
                Log.Error("隐藏UI异常，已复位记录状态。Id[{0}] {1}", id, ex);
                RecoverHidingRecords(session);
            }
            finally
            {
                session.Release();
            }
        }

        /// <summary>
        /// 解析界面所属的根界面 Id。
        /// 优先取记录上的根链路（最可靠），其次退化为 UIData，最后退化为自身，
        /// 保证 UIData 被移除后隐藏流程依然可用。
        /// </summary>
        private int ResolveRootId(int id)
        {
            var record = GetRecord(id);
            if (record != null)
            {
                return record.ParentRootId != 0 ? record.ParentRootId : record.Id;
            }

            // 没有记录时只可能是「还没走过显示流程」：退化成按 UIData 的父类型解析，
            // 之后 GetRecord 又会按类型落到主实例，语义与之前保持一致。
            if (_idUIData.TryGetValue(id, out var data) && data != null)
            {
                var parentId = data.GetParentID();
                return parentId != 0 ? parentId : id;
            }

            return id;
        }

        /// <summary>
        /// 隐藏流程异常时的兜底：把仍然停在 Hiding 的记录复位，
        /// 并主动完成等待句柄，避免显示/隐藏请求永久悬挂。
        /// </summary>
        private void RecoverHidingRecords(HideSession session)
        {
            for (int i = session.Targets.Count - 1; i >= 0; i--)
            {
                var record = session.Targets[i];
                if (record == null || record.Phase != UIPhase.Hiding)
                {
                    continue;
                }

                SetPhase(record, UIPhase.Hidden);
                if (record.UI != null)
                {
                    _cacheHandler.TrackHidden(record);
                }

                CompletePendingShow(record, false);
                CompletePendingHide(record, true);
            }
        }

        /// <summary>
        /// 开始隐藏链式调用
        /// </summary>
        private void BeginHideChain(HideSession session)
        {
            session.TargetVersions.Clear();
            for (int i = 0; i < session.Targets.Count; i++)
            {
                session.TargetVersions.Add(0);
            }

            // 从后往前遍历，确保子界面先隐藏
            for (int i = session.Targets.Count - 1; i >= 0; i--)
            {
                var record = session.Targets[i];
                session.TargetVersions[i] = BeginHideRecord(record, session.IsAnim);
            }
        }

        /// <summary>
        /// 检查隐藏会话是否仍然拥有它启动的所有隐藏事务。
        /// </summary>
        private bool IsHideSessionCurrent(HideSession session)
        {
            if (session == null || session.Targets.Count != session.TargetVersions.Count)
            {
                return false;
            }

            for (int i = 0; i < session.Targets.Count; i++)
            {
                var version = session.TargetVersions[i];
                if (version == 0)
                {
                    continue;
                }

                if (!IsRequestCurrent(session.Targets[i], version))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 等待隐藏链完成
        /// </summary>
        private async UniTask WaitHideChain(HideSession session)
        {
            for (int i = session.Targets.Count - 1; i >= 0; i--)
            {
                var record = session.Targets[i];
                var pendingHide = record.PendingHide;
                if (pendingHide != null)
                {
                    await pendingHide.Task;
                }
            }
        }

        /// <summary>
        /// 开始隐藏单个UI记录
        /// 快速显示->隐藏的竞态会被折叠为立即关闭，不播放隐藏动画
        /// </summary>
        private int BeginHideRecord(UIRecord record, bool isAnim)
        {
            ClearMountedRootChild(record);
            var wasVisible = record.IsVisibleCommitted;
            var wasShowing = record.IsShowingLike;
            // 「刚显示又立刻隐藏」的折叠判断收敛到一个入口（按时间窗口，与帧率解耦）。
            var collapseJitter = ShouldSkipHideAnim(record);
            var version = BeginHideTransition(record);
            SetPhase(record, UIPhase.Hiding);

            // 如果是刚显示就立即隐藏的情况，中断显示动画但不播放隐藏动画
            if (collapseJitter)
            {
                CompletePendingShow(record, false);
                record.UI?.DoHide(false, false);
                // DoHide可能因为State已经是Hide而短路（父节点的递归ToHide已经处理过），
                // 此时_OnUIHidden不会再触发，需要主动完成PendingHide。
                CompletePendingHide(record, true);
                return version;
            }

            // 如果请求在真正的显示提交之前就失去了竞态，立即关闭
            if (!wasVisible && !wasShowing)
            {
                SetPhase(record, UIPhase.Hidden);
                if (record.UI != null)
                {
                    _cacheHandler.TrackHidden(record);
                }
                CompletePendingHide(record, true);
                return version;
            }

            // 正常隐藏流程
            if (record.UI != null && (wasVisible || wasShowing))
            {
                // 如果正在显示中（尚未提交可见），Show流程的回调将被_async机制拦截，
                // 需要主动完成PendingShow以避免WaitForTargetVisible永久挂起。
                if (wasShowing && !wasVisible)
                {
                    CompletePendingShow(record, false);
                }
                record.UI.DoHide(isAnim, false);
                // DoHide可能因为State已经是Hide而短路（父节点的递归ToHide已经处理过），
                // 或者无动画同步完成。此时_OnUIHidden要么已触发（PendingHide已完成），
                // 要么不会触发（短路）。两种情况下都需要确保PendingHide被完成。
                // 如果有HideAnim正在播放，_OnUIHidden会在动画完成后调用CompletePendingHide，
                // 此时不应提前完成。
                if (record.UI.State == UIState.Hide)
                {
                    CompletePendingHide(record, true);
                }
            }
            else
            {
                // UI实例尚未就绪（如仍在Creating阶段），无法执行DoHide。
                SetPhase(record, UIPhase.Hidden);
                CompletePendingShow(record, false);
                CompletePendingHide(record, true);
            }

            return version;
        }

        /// <summary>
        /// 收集需要隐藏的目标列表
        /// </summary>
        private void CollectHideTargets(int rootId, List<UIRecord> targets, int requestedId)
        {
            targets.Clear();
            var seen = Pool.Get<HashSet<int>>();
            try
            {
                if (_rootRecordIds.TryGetValue(rootId, out var rootSet) && rootSet.Count > 0)
                {
                    // 收集根链路下所有需要隐藏的UI记录
                    foreach (var recordId in rootSet)
                    {
                        if (!_records.TryGetValue(recordId, out var record))
                        {
                            continue;
                        }

                        // 只收集有UI实例或正在显示的记录
                        if (record.UI == null && !record.IsShowingLike)
                        {
                            continue;
                        }

                        if (seen.Add(recordId))
                        {
                            targets.Add(record);
                        }
                    }
                }
                else
                {
#if UNITY_EDITOR
                    ValidateMissingRootIndex(rootId, requestedId);
#endif
                }

                // 根链路索引在某些竞态下可能尚未建立或已临时失配。
                // 这里必须真正回退扫描记录表补齐整条根链：只靠索引的话一个目标都收不到，
                // 表现就是「界面关不掉」。
                foreach (var kv in _records)
                {
                    var record = kv.Value;
                    if (record == null)
                    {
                        continue;
                    }

                    if (record.Id != rootId && record.ParentRootId != rootId)
                    {
                        continue;
                    }

                    if (record.UI == null && !record.IsShowingLike)
                    {
                        continue;
                    }

                    if (seen.Add(record.Id))
                    {
                        targets.Add(record);
                    }
                }
            }
            finally
            {
                seen.Clear();
                Pool.Push(seen);
            }

            // 排序：层级深的子界面优先隐藏，同层级再按根界面和 Id 稳定排序
            targets.Sort((a, b) =>
            {
                var depthA = TryGetUIData(a.TypeId, out var dataA) ? dataA.GetParentDepth() : 0;
                var depthB = TryGetUIData(b.TypeId, out var dataB) ? dataB.GetParentDepth() : 0;
                if (depthA != depthB) return depthB.CompareTo(depthA);
                if (a.Id == rootId && b.Id != rootId) return 1;
                if (a.Id != rootId && b.Id == rootId) return -1;
                return a.Id.CompareTo(b.Id);
            });
        }

#if UNITY_EDITOR
        private void ValidateMissingRootIndex(int rootId, int requestedId)
        {
            if (_showed.ContainsKey(rootId) || _showed.ContainsKey(requestedId))
            {
                Log.Error("UI root index missing while hiding. RootId[{0}] RequestId[{1}]", rootId, requestedId);
                return;
            }

            foreach (var kv in _records)
            {
                var record = kv.Value;
                if (record == null)
                {
                    continue;
                }

                if (record.Id != rootId && record.Id != requestedId && record.ParentRootId != rootId)
                {
                    continue;
                }

                if (record.UI == null && !record.IsShowingLike && !record.IsVisibleCommitted)
                {
                    continue;
                }

                Log.Error("UI root index missing while hiding. RootId[{0}] RequestId[{1}] RecordId[{2}]",
                    rootId, requestedId, record.Id);
                return;
            }
        }
#endif

        private void ForceHideShowedOnly(int id)
        {
            if (!_showed.TryGetValue(id, out var ui) || ui == null)
            {
                return;
            }

            RemoveShowed(id);
            // State 已经是 Hide 时 DoHide 会直接短路，_OnUIHidden 不会触发，需要在这里收尾。
            // 其余情况统一交给 _OnUIHidden，避免 UI_HIDE 事件被派发两次。
            var alreadyHidden = ui.State == UIState.Hide;
            ui.DoHide(false, false);
            if (alreadyHidden)
            {
                NotifyOrphanHidden(ui);
            }

#if UNITY_EDITOR
            AssertShowedConsistency("ForceHideShowedOnly");
#endif
        }

        /// <summary>
        /// UI隐藏完成回调
        /// 隐藏完成是提交可见性和生命周期缓存的唯一位置
        /// </summary>
        private void _OnUIHidden(IUI ui)
        {
            var record = GetRecord(ui.ID);
            if (record == null)
            {
                // 没有记录说明这个实例已经游离在管理之外（例如 _showed 中只有 UI 没有记录的异常残留），
                // 统一在这里收尾：补发事件并销毁，避免实例既不在缓存里也不被释放。
                NotifyOrphanHidden(ui);
                return;
            }

            if (!ReferenceEquals(record.UI, ui))
            {
                ui.Dispose();
                return;
            }

            // 从可见记录中分离
            DetachVisibleRecord(record);
            SetPhase(record, UIPhase.Hidden);
            // 跟踪隐藏的UI到缓存处理器
            _cacheHandler.TrackHidden(record);
            CompletePendingHide(record, true);

#if UNITY_EDITOR
            __Debugger_Showed_Event();
#endif
            // 发送UI隐藏事件
            EventMgr.Ins.Run(MainEventType.UI_HIDE, ui.ID);
            EventMgr.Ins.Run(MainEventType.UI_HIDE, ui.GetType());
            _blurHandler.OnHide(ui);
            _focusHandler.OnHidden(ui);
#if UNITY_EDITOR
            AssertShowedConsistency("OnUIHidden");
#endif
        }

        /// <summary>
        /// 处理「没有对应记录」的隐藏收尾：补发隐藏事件并直接销毁实例。
        /// 事件必须在 Dispose 之前派发，Dispose 之后 IUI.ID 和 Data 已经不可用。
        /// </summary>
        private void NotifyOrphanHidden(IUI ui)
        {
            if (ui == null)
            {
                return;
            }

            var id = ui.ID;
            var type = ui.GetType();

#if UNITY_EDITOR
            __Debugger_Showed_Event();
#endif
            EventMgr.Ins.Run(MainEventType.UI_HIDE, id);
            EventMgr.Ins.Run(MainEventType.UI_HIDE, type);
            _blurHandler.OnHide(ui);
            _focusHandler.OnHidden(ui);
            // 走完整的销毁流程：即使记录已经不存在，也要把资源包引用释放掉（否则整包无法卸载）。
            Dispose(ui);
        }

        /// <summary>
        /// 释放UI资源
        /// </summary>
        private void Dispose(IUI ui)
        {
            if (ui == null)
            {
                return;
            }

            var id = ui.ID;
            var typeId = ui.TypeId;
            var record = GetRecord(id);

            // 同一个 Id 下可能同时存在「缓存里的旧实例」和「记录上的新实例」（显示竞态 + 请求取消）：
            // 回收旧实例时如果无条件 RemoveRecord(id)，会把新实例的记录一起删掉，
            // 之后这个界面就再也隐藏不了了。只有记录确实属于当前实例（或是空壳）时才销毁记录。
            var ownsRecord = record == null || record.UI == null || ReferenceEquals(record.UI, ui);

            // 实例自身还持有 Data，在 Dispose 之前先取出包名：
            // 记录已经被重置（例如 Release 期间）时，这是唯一还能找回资源包的地方。
            var instancePkgs = ui.Data != null ? ui.Data.Pkgs : null;

            _focusHandler.OnDisposed(ui);
            ui.Dispose();

            if (record != null && ownsRecord)
            {
                _cacheHandler.RemoveRecord(record);
                RemoveRecord(id);
            }

            // 资源包引用一定要释放：动态界面可能在销毁前就被移除了 UIData，
            // 因此按「记录缓存 -> 实例自身 -> UIData」的顺序依次兜底。
            var pkgs = record?.Pkgs;
            if (pkgs == null || pkgs.Length == 0)
            {
                pkgs = instancePkgs;
            }

            if ((pkgs == null || pkgs.Length == 0) && typeId != 0 && _idUIData.TryGetValue(typeId, out var data))
            {
                pkgs = data.Pkgs;
            }

            if (pkgs != null && pkgs.Length > 0)
            {
                ResMgr.Ins.RemoveUIPackage(pkgs);
            }

            // 对话框类型需要移除UIData
            if (ui is UIDialog && typeId != 0)
            {
                RemoveUIData(typeId);
            }
        }
    }
}
