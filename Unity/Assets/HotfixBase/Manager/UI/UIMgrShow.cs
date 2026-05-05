using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Ux
{
    public partial class UIMgr
    {
        private sealed class ShowSession
        {
            public int RequestedId { get; private set; }
            public int TargetId { get; private set; }
            public IUIParam Param { get; private set; }
            public bool IsAnim { get; private set; }
            public bool CheckStack { get; private set; }
            public readonly List<int> Chain = new List<int>();
            internal readonly List<UIRecord> Activated = new List<UIRecord>();
            public int RootId => Chain.Count > 0 ? Chain[0] : TargetId;

            public void Reset(int requestedId, int targetId, IUIParam param, bool isAnim, bool checkStack)
            {
                RequestedId = requestedId;
                TargetId = targetId;
                Param = param;
                IsAnim = isAnim;
                CheckStack = checkStack;
                Chain.Clear();
                Activated.Clear();
            }

            public void Release()
            {
                RequestedId = 0;
                TargetId = 0;
                Param = null;
                IsAnim = true;
                CheckStack = true;
                Chain.Clear();
                Activated.Clear();
                Pool.Push(this);
            }
        }

        public UIShowBuilder Create(int id = 0)
        {
            var builder = Pool.Get<UIShowBuilder>();
            builder.SetId(id);
            return builder;
        }

        private async UniTask<T> _ShowAsync<T>(int id, IUIParam param, bool isAnim, bool checkStack) where T : IUI
        {
            var data = GetUIData(id);
            if (data == null)
            {
                return default;
            }

            var targetId = data.GetChildID();
            if (_CheckDownload(targetId, param, isAnim))
            {
                return default;
            }

            var targetRecord = GetRecord(targetId);
            if (ShouldSkipShowRequest(targetRecord))
            {
                return targetRecord?.UI is T cachedUI ? cachedUI : default;
            }

            var session = Pool.Get<ShowSession>();
            session.Reset(id, targetId, param, isAnim, checkStack);
            try
            {
                BuildShowChain(targetId, session.Chain);
                var success = await RunShowSession(session);
                if (!success)
                {
                    return default;
                }

                var record = GetRecord(id);
                return record?.UI is T ui ? ui : default;
            }
            finally
            {
                session.Release();
            }
        }

        private void BuildShowChain(int targetId, List<int> chain)
        {
            chain.Clear();
            var data = GetUIData(targetId);
            if (data == null)
            {
                return;
            }

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

        // Show is split into clear phases so request invalidation is only handled at a few choke points.
        private async UniTask<bool> RunShowSession(ShowSession session)
        {
            // 1. Ensure every node in the parent-child chain has a usable instance.
            if (!await PrepareShowChain(session))
            {
                return false;
            }

            // 2. Start every node without serially waiting on parent animations.
            if (!await StartShowChain(session))
            {
                return false;
            }

            // 3. The request is considered complete when the requested target becomes visible.
            if (!await WaitForTargetVisible(session))
            {
                return false;
            }

            // 4. Drop stale completions after the target wins the race.
            if (!ValidateShowChain(session))
            {
                return false;
            }

            ReconcileRootChildren(session);
            return true;
        }

        // Build or reuse all records needed by the target chain before any show callback mutates visible state.
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

                if (!await PrepareRecordForShow(record, version))
                {
                    await AbortShowSession(session, i);
                    return false;
                }

                session.Activated.Add(record);
            }

            return true;
        }

        // Parent and child shows are started back-to-back; only the requested target is awaited to visible.
        private async UniTask<bool> StartShowChain(ShowSession session)
        {
            for (int i = 0; i < session.Activated.Count; i++)
            {
                var record = session.Activated[i];
                if (!IsRequestCurrent(record, record.RequestVersion))
                {
                    await AbortShowSession(session, i);
                    return false;
                }

                record.Phase = UIPhase.Showing;
                record.LastShowStartFrame = Time.frameCount;
                _showing[record.Id] = record.RequestVersion;
                record.PendingVisible = new UniTaskCompletionSource<bool>();
                await record.UI.DoShow(session.IsAnim, session.RequestedId,
                    record.Id == session.RequestedId ? session.Param : null, session.CheckStack);
            }

            return true;
        }

        private async UniTask<bool> WaitForTargetVisible(ShowSession session)
        {
            var targetRecord = session.Activated[session.Activated.Count - 1];
            if (targetRecord.IsVisibleCommitted)
            {
                return IsRequestCurrent(targetRecord, targetRecord.RequestVersion);
            }

            if (targetRecord.PendingVisible != null)
            {
                await targetRecord.PendingVisible.Task;
            }
            return IsRequestCurrent(targetRecord, targetRecord.RequestVersion);
        }

        // Once the target wins, every record in the chain must still belong to the same request version.
        private bool ValidateShowChain(ShowSession session)
        {
            for (int i = 0; i < session.Activated.Count; i++)
            {
                var record = session.Activated[i];
                _showing.Remove(record.Id);
                if (!IsRequestCurrent(record, record.RequestVersion))
                {
                    AbortShowSession(session, i + 1).Forget();
                    return false;
                }
            }

            return true;
        }

        private async UniTask<bool> PrepareRecordForShow(UIRecord record, int version)
        {
            if (record.UI != null && record.IsVisibleCommitted)
            {
                return true;
            }

            if (record.PendingHide != null)
            {
                // A later show can legally reuse the same record after the previous hide settles.
                await record.PendingHide.Task;
                record.PendingHide = null;
            }

            if (_cacheHandler.TryTakeCached(record.Id, out var cached))
            {
                record.UI = cached;
                record.Phase = UIPhase.Hidden;
                record.WaitDel = null;
                record.CacheState = CacheState.None;
                return IsRequestCurrent(record, version);
            }

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
                record.PendingVisible?.TrySetResult(false);
                record.PendingVisible = null;
            }

            await UniTask.CompletedTask;
        }

        private void ReconcileRootChildren(ShowSession session)
        {
            for (int i = 0; i < session.Activated.Count; i++)
            {
                var record = session.Activated[i];
                record.ParentRootId = session.RootId;
                record.CurrentChildId = session.TargetId;
            }

            foreach (var kv in _records)
            {
                var record = kv.Value;
                if (record.Id == session.RootId || record.Id == session.TargetId)
                {
                    continue;
                }

                if (record.ParentRootId != session.RootId)
                {
                    continue;
                }

                if (!record.IsVisibleCommitted || record.UI == null)
                {
                    continue;
                }

                // Only one child under the same root should remain visible after a tab switch.
                NextRequestVersion(record);
                record.PendingHide = new UniTaskCompletionSource<bool>();
                record.Phase = UIPhase.Hiding;
                record.UI.DoHide(false, false);
            }
        }

        // Tabs inherit stack type from their root container instead of deciding it independently.
        private UIType ResolveStackType(IUI ui, int rootId)
        {
            if (ui is not UITabView)
            {
                return ui.Type;
            }

            var rootRecord = GetRecord(rootId);
            return rootRecord?.UI?.Type ?? ui.Type;
        }

        private async UniTask<IUI> CreateUI(IUIData data)
        {
            if (data.Pkgs is { Length: > 0 })
            {
                if (!await ResMgr.Ins.LoadUIPackage(data.Pkgs))
                {
                    Log.Error($"[{data.Name}]鍖呭姞杞介敊璇?");
                    return null;
                }
            }

            var ui = (IUI)Activator.CreateInstance(data.CType);
            ui.InitData(data, _initData);
            return ui;
        }

        void _OnUIShown(IUI ui, IUIParam param, bool checkStack)
        {
            var record = GetRecord(ui.ID);
            if (record == null)
            {
                return;
            }

            AttachVisibleRecord(record);
            record.LastVisibleFrame = Time.frameCount;
            var rootId = record.ParentRootId == 0 ? record.Id : record.ParentRootId;
            if (checkStack)
            {
                _stackHandler.CommitVisible(rootId, record.CurrentChildId == 0 ? record.Id : record.CurrentChildId,
                    param, ResolveStackType(ui, rootId));
            }
            _blurHandler.OnShowed(ui);
            record.PendingVisible?.TrySetResult(true);
            record.PendingVisible = null;

            if (_showing.TryGetValue(ui.ID, out var version) && version == record.RequestVersion)
            {
                _showing.Remove(ui.ID);
            }
        }
    }
}
