using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Ux
{
    public partial class UIMgr
    {
        private readonly List<int> _hideAllIdBuffer = new List<int>();

        private sealed class HideSession
        {
            public int RequestedId { get; private set; }
            public int RootId { get; private set; }
            public bool IsAnim { get; private set; }
            public bool CheckStack { get; private set; }
            internal readonly List<UIRecord> Targets = new List<UIRecord>();

            public void Reset(int requestedId, int rootId, bool isAnim, bool checkStack)
            {
                RequestedId = requestedId;
                RootId = rootId;
                IsAnim = isAnim;
                CheckStack = checkStack;
                Targets.Clear();
            }

            public void Release()
            {
                RequestedId = 0;
                RootId = 0;
                IsAnim = true;
                CheckStack = true;
                Targets.Clear();
                Pool.Push(this);
            }
        }

        void _HideAll(HashSet<int> ignoreSet)
        {
            _stackHandler.Clear();
            _hideAllIdBuffer.Clear();

            foreach (var kv in _records)
            {
                var record = kv.Value;
                if (record.IsVisibleCommitted || record.IsShowingLike || _showing.ContainsKey(kv.Key))
                {
                    _hideAllIdBuffer.Add(kv.Key);
                }
            }

            for (int i = 0; i < _hideAllIdBuffer.Count; i++)
            {
                var id = _hideAllIdBuffer[i];
                if (ignoreSet != null && ignoreSet.Contains(id))
                {
                    continue;
                }
                Hide(id, false);
            }

            _hideAllIdBuffer.Clear();
        }

        public void HideAll()
        {
            _HideAll(null);
        }

        public void HideAll(IList<int> ignoreList = null)
        {
            if (ignoreList == null || ignoreList.Count == 0)
            {
                _HideAll(null);
                return;
            }

            _ignoreSet ??= new HashSet<int>();
            _ignoreSet.Clear();
            for (int i = 0; i < ignoreList.Count; i++)
            {
                _ignoreSet.Add(ignoreList[i]);
            }
            _HideAll(_ignoreSet);
        }

        public void HideAll(IList<Type> ignoreList = null)
        {
            if (ignoreList == null || ignoreList.Count == 0)
            {
                _HideAll(null);
                return;
            }

            _ignoreSet ??= new HashSet<int>();
            _ignoreSet.Clear();
            for (int i = 0; i < ignoreList.Count; i++)
            {
                _ignoreSet.Add(ConverterID(ignoreList[i]));
            }
            _HideAll(_ignoreSet);
        }

        public void Hide<T>(bool isAnim = true) where T : UIBase
        {
            Hide(ConverterID(typeof(T)), isAnim);
        }

        public void Hide(int id, bool isAnim = true)
        {
            _Hide(id, isAnim, true).Forget();
        }

        public void HideNotStack<T>(bool isAnim = true) where T : UIBase
        {
            HideNotStack(ConverterID(typeof(T)), isAnim);
        }

        public void HideNotStack(int id, bool isAnim = true)
        {
            _Hide(id, isAnim, false).Forget();
        }

        private async UniTaskVoid _Hide(int id, bool isAnim, bool checkStack)
        {
            var data = GetUIData(id);
            if (data == null)
            {
                return;
            }

            var rootId = data.GetParentID();
            var rootRecord = GetRecord(rootId);
            if (ShouldSkipHideRequest(rootRecord))
            {
                return;
            }
            if (rootId != id && rootRecord != null && rootRecord.CurrentChildId != id)
            {
                return;
            }
            var session = Pool.Get<HideSession>();
            session.Reset(id, rootId, isAnim, checkStack);
            try
            {
                // 1. Snapshot the current root chain before any hide callback mutates visibility.
                CollectHideTargets(rootId, session.Targets);

                // 2. Fire every hide first so parent/child animations can overlap.
                BeginHideChain(session);

                // 3. Then wait for every pending hide to settle before touching stack fallback.
                await WaitHideChain(session);

                _stackHandler.RemoveRoot(rootId);
                if (checkStack)
                {
                    var previous = _stackHandler.PeekPrevious(rootId);
                    if (previous.HasValue)
                    {
                        _ShowAsync<IUI>(previous.Value.ActiveId, previous.Value.Param, false, false).Forget();
                    }
                }
            }
            finally
            {
                session.Release();
            }
        }

        private void BeginHideChain(HideSession session)
        {
            for (int i = session.Targets.Count - 1; i >= 0; i--)
            {
                var record = session.Targets[i];
                BeginHideRecord(record, session.IsAnim);
            }
        }

        private async UniTask WaitHideChain(HideSession session)
        {
            for (int i = session.Targets.Count - 1; i >= 0; i--)
            {
                var pendingHide = session.Targets[i].PendingHide;
                if (pendingHide != null)
                {
                    await pendingHide.Task;
                }
            }
        }

        // Fast show->hide races are collapsed into an immediate close without playing a hide animation.
        private void BeginHideRecord(UIRecord record, bool isAnim)
        {
            var version = NextRequestVersion(record);
            record.LastHideRequestFrame = Time.frameCount;
            var wasVisible = record.IsVisibleCommitted;
            var wasShowing = record.IsShowingLike || _showing.ContainsKey(record.Id);
            var wasFreshlyShown = IsFreshlyShown(record);
            var wasFreshlyVisible = IsFreshlyVisible(record);
            record.PendingHide = new UniTaskCompletionSource<bool>();
            record.Phase = UIPhase.Hiding;

            if (wasFreshlyShown || wasFreshlyVisible)
            {
                // Show immediately followed by Hide should interrupt the show animation,
                // but must not play a hide animation on top of it.
                record.UI?.DoHide(false, false);
                return;
            }

            if (!wasVisible && !wasShowing)
            {
                // The request lost the race before a real show commit happened; close it immediately.
                record.Phase = UIPhase.Hidden;
                if (record.UI != null)
                {
                    _cacheHandler.TrackHidden(record);
                }
                record.PendingHide.TrySetResult(true);
                record.PendingHide = null;
                return;
            }

            if (record.UI != null && (wasVisible || wasShowing))
            {
                record.UI.DoHide(isAnim, false);
            }
            else if (!IsRequestCurrent(record, version))
            {
                record.PendingHide?.TrySetResult(false);
                record.PendingHide = null;
            }
        }

        private void CollectHideTargets(int rootId, List<UIRecord> targets)
        {
            targets.Clear();
            if (!_rootRecordIds.TryGetValue(rootId, out var rootSet))
            {
                return;
            }

            foreach (var recordId in rootSet)
            {
                if (!_records.TryGetValue(recordId, out var record))
                {
                    continue;
                }

                if (record.UI == null && !record.IsShowingLike && !_showing.ContainsKey(record.Id))
                {
                    continue;
                }

                targets.Add(record);
            }

            targets.Sort((a, b) =>
            {
                if (a.Id == rootId && b.Id != rootId) return -1;
                if (a.Id != rootId && b.Id == rootId) return 1;
                return a.Id.CompareTo(b.Id);
            });
        }

        private void _OnUIHidden(IUI ui)
        {
            var record = GetRecord(ui.ID);
            if (record != null)
            {
                // Hide completion is the single place where visibility and lifecycle cache are committed.
                DetachVisibleRecord(record);
                record.Phase = UIPhase.Hidden;
                _cacheHandler.TrackHidden(record);
                record.PendingHide?.TrySetResult(true);
                record.PendingHide = null;
            }

#if UNITY_EDITOR
            __Debugger_Showed_Event();
#endif
            EventMgr.Ins.Run(MainEventType.UI_HIDE, ui.ID);
            EventMgr.Ins.Run(MainEventType.UI_HIDE, ui.GetType());
            _blurHandler.OnHide(ui);
        }

        private void Dispose(IUI ui)
        {
            var id = ui.ID;
            var record = GetRecord(id);
            ui.Dispose();
            _cacheHandler.RemoveRecord(record);
            RemoveRecord(id);

            var data = GetUIData(id);
            if (data == null || data.Pkgs == null || data.Pkgs.Length == 0)
            {
                return;
            }

            ResMgr.Ins.RemoveUIPackage(data.Pkgs);
            if (ui is UIDialog)
            {
                RemoveUIData(id);
            }
        }
    }
}
