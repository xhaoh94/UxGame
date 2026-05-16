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
            /// 请求显示的UI ID
            /// </summary>
            public int RequestedId { get; private set; }
            
            /// <summary>
            /// 目标UI ID（可能是Tab页的子界面）
            /// </summary>
            public int TargetId { get; private set; }
            
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
            
            /// <summary>
            /// 获取根界面ID
            /// </summary>
            public int RootId => Chain.Count > 0 ? Chain[0] : TargetId;

            /// <summary>
            /// 重置会话数据
            /// </summary>
            public void Reset(int requestedId, int targetId, IUIParam param, bool isAnim, bool checkStack)
            {
                RequestedId = requestedId;
                TargetId = targetId;
                Param = param;
                IsAnim = isAnim;
                CheckStack = checkStack;
                Chain.Clear();
                Activated.Clear();
                ActivatedVersions.Clear();
            }

            /// <summary>
            /// 释放会话资源
            /// </summary>
            public void Release()
            {
                RequestedId = 0;
                TargetId = 0;
                Param = null;
                IsAnim = true;
                CheckStack = true;
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
        private async UniTask<T> _ShowAsync<T>(int id, IUIParam param, bool isAnim, bool checkStack) where T : IUI
        {
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

            var targetRecord = GetRecord(targetId);
            // 如果同帧已有显示请求，等待正在进行的显示完成后再返回
            if (ShouldSkipShowRequest(targetRecord))
            {
                if (targetRecord != null && !targetRecord.IsVisibleCommitted && targetRecord.PendingShow != null)
                {
                    var showResult = await targetRecord.PendingShow.Task;
                    if (!showResult)
                    {
                        return default;
                    }
                }

                return targetRecord is { IsVisibleCommitted: true, UI: T cachedUI } ? cachedUI : default;
            }

            var session = Pool.Get<ShowSession>();
            session.Reset(id, targetId, param, isAnim, checkStack);
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
            // 1. 确保父子链中的每个节点都有可用的实例
            if (!await PrepareShowChain(session))
            {
                return false;
            }

            // 2. 同时启动每个节点的显示，不需要等待父动画完成
            if (!await StartShowChain(session))
            {
                return false;
            }

            // 3. 当请求的目标变为可见时，认为请求完成
            if (!await WaitForTargetVisible(session))
            {
                return false;
            }

            // 4. 在目标赢得竞态后，丢弃过期的完成状态
            if (!ValidateShowChain(session))
            {
                return false;
            }

            // 协调根界面的子界面
            ReconcileRootChildren(session);
            return true;
        }

        /// <summary>
        /// 在任何显示回调改变可见状态之前，构建或复用目标链所需的所有记录
        /// </summary>
        private async UniTask<bool> PrepareShowChain(ShowSession session)
        {
            for (int i = 0; i < session.Chain.Count; i++)
            {
                var nodeId = session.Chain[i];
                var record = GetOrCreateRecord(nodeId);
                var version = NextRequestVersion(record);
                RegisterRecordToRoot(record, session.RootId);
                record.CurrentChildId = session.TargetId;
                record.LastShowParam = nodeId == session.RequestedId ? session.Param : null;
                record.LastShowRequestFrame = Time.frameCount;
                ResetPendingShow(record);

                if (!await PrepareRecordForShow(record, version))
                {
                    CompletePendingShow(record, false);
                    await AbortShowSession(session, i);
                    return false;
                }

                session.Activated.Add(record);
                session.ActivatedVersions.Add(version);
            }

            return true;
        }

        /// <summary>
        /// 父子界面显示是连续启动的，只等待目标界面变为可见
        /// </summary>
        private async UniTask<bool> StartShowChain(ShowSession session)
        {
            for (int i = 0; i < session.Activated.Count; i++)
            {
                var record = session.Activated[i];
                var version = session.ActivatedVersions[i];
                if (!IsRequestCurrent(record, version))
                {
                    CompletePendingShow(record, false);
                    await AbortShowSession(session, i);
                    return false;
                }

                record.Phase = UIPhase.Showing;
                record.LastShowStartFrame = Time.frameCount;
                await record.UI.DoShow(session.IsAnim, session.RequestedId,
                    record.Id == session.RequestedId ? session.Param : null, session.CheckStack);
            }

            return true;
        }

        /// <summary>
        /// 等待目标界面变为可见
        /// </summary>
        private async UniTask<bool> WaitForTargetVisible(ShowSession session)
        {
            var targetRecord = session.Activated[session.Activated.Count - 1];
            var targetVersion = session.ActivatedVersions[session.ActivatedVersions.Count - 1];
            if (targetRecord.IsVisibleCommitted)
            {
                return IsRequestCurrent(targetRecord, targetVersion);
            }

            if (targetRecord.PendingShow != null)
            {
                await targetRecord.PendingShow.Task;
            }
            return IsRequestCurrent(targetRecord, targetVersion);
        }

        /// <summary>
        /// 一旦目标赢得竞态，链中的每个记录仍然必须属于同一请求版本
        /// </summary>
        private bool ValidateShowChain(ShowSession session)
        {
            for (int i = 0; i < session.Activated.Count; i++)
            {
                var record = session.Activated[i];
                var version = session.ActivatedVersions[i];
                if (!IsRequestCurrent(record, version))
                {
                    AbortShowSession(session, i + 1).Forget();
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 准备用于显示的UI记录
        /// </summary>
        private async UniTask<bool> PrepareRecordForShow(UIRecord record, int version)
        {
            // 如果UI已存在且可见，直接返回
            if (record.UI != null && record.IsVisibleCommitted)
            {
                return true;
            }

            // 等待之前的隐藏完成
            if (record.PendingHide != null)
            {
                // 后续的显示可以在之前的隐藏完成后合法地重用同一记录
                await record.PendingHide.Task;
                record.PendingHide = null;
            }

            // 尝试从缓存中获取
            if (_cacheHandler.TryTakeCached(record.Id, out var cached))
            {
                record.UI = cached;
                record.Phase = UIPhase.Hidden;
                record.WaitDel = null;
                record.CacheState = CacheState.None;
                return IsRequestCurrent(record, version);
            }

            // 创建新的UI实例
            record.Phase = UIPhase.Creating;
            var created = await CreateUI(GetUIData(record.Id));
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
                record.Phase = UIPhase.Idle;
                return false;
            }

            record.UI = created;
            record.Phase = UIPhase.Hidden;
            return true;
        }

        /// <summary>
        /// 中止显示会话，回滚部分显示的UI
        /// </summary>
        private async UniTask AbortShowSession(ShowSession session, int activatedCount)
        {
            for (int i = activatedCount - 1; i >= 0; i--)
            {
                var record = session.Activated[i];
                if (record == null || record.UI == null)
                {
                    continue;
                }

                if (record.IsVisibleCommitted)
                {
                    continue;
                }

                record.Phase = UIPhase.Hidden;
                _cacheHandler.TrackHidden(record);
                CompletePendingShow(record, false);
            }

            await UniTask.CompletedTask;
        }

        /// <summary>
        /// 协调根界面的子界面，确保Tab切换后只有一个子界面保持可见
        /// </summary>
        private void ReconcileRootChildren(ShowSession session)
        {
            for (int i = 0; i < session.Activated.Count; i++)
            {
                var record = session.Activated[i];
                record.ParentRootId = session.RootId;
                record.CurrentChildId = session.TargetId;
#if UNITY_EDITOR
                ValidateRecordRootBinding(record);
#endif
            }

            // 隐藏同一根下的其他子界面
            foreach (var kv in _records)
            {
                var record = kv.Value;
                // 跳过根界面和目标界面
                if (record.Id == session.RootId || record.Id == session.TargetId)
                {
                    continue;
                }

                // 跳过不同根的界面
                if (record.ParentRootId != session.RootId)
                {
                    continue;
                }

                // 跳过不可见或没有UI实例的界面
                if (!record.IsVisibleCommitted || record.UI == null)
                {
                    continue;
                }

                // Tab切换后，同一根下只有一个子界面应该保持可见
                NextRequestVersion(record);
                ResetPendingHide(record);
                record.Phase = UIPhase.Hiding;
                record.UI.DoHide(false, false);
            }
        }

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
        private async UniTask<IUI> CreateUI(IUIData data)
        {
            // 加载UI资源包
            if (data.Pkgs is { Length: > 0 })
            {
                if (!await ResMgr.Ins.LoadUIPackage(data.Pkgs))
                {
                    Log.Error($"[{data.Name}]资源包加载失败");
                    return null;
                }
            }

            // 创建UI实例
            var ui = (IUI)Activator.CreateInstance(data.CType);
            ui.InitData(data, _initData);
            return ui;
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

            // 附加到可见记录
            AttachVisibleRecord(record);
            record.LastVisibleFrame = Time.frameCount;
            var rootId = record.ParentRootId == 0 ? record.Id : record.ParentRootId;
            
            // 如果需要检查栈，将界面加入栈管理
            if (checkStack)
            {
                _stackHandler.CommitVisible(rootId, record.CurrentChildId == 0 ? record.Id : record.CurrentChildId,
                    param, ResolveStackType(ui, rootId));
#if UNITY_EDITOR
                ValidateCurrentChild(rootId, record.CurrentChildId == 0 ? record.Id : record.CurrentChildId);
#endif
            }
            
            // 通知模糊效果处理器
            _blurHandler.OnShowed(ui);
            CompletePendingShow(record, true);
        }
    }
}
