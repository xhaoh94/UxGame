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
            /// 是否通过回退链路收集到了目标
            /// </summary>
            public bool UsedFallback { get; private set; }
            
            /// <summary>
            /// 隐藏目标列表
            /// </summary>
            internal readonly List<UIRecord> Targets = new List<UIRecord>();

            /// <summary>
            /// 重置会话数据
            /// </summary>
            public void Reset(int requestedId, int rootId, bool isAnim, bool checkStack)
            {
                RequestedId = requestedId;
                RootId = rootId;
                IsAnim = isAnim;
                CheckStack = checkStack;
                UsedFallback = false;
                Targets.Clear();
            }

            public void MarkFallbackUsed()
            {
                UsedFallback = true;
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
                UsedFallback = false;
                Targets.Clear();
                Pool.Push(this);
            }
        }

        /// <summary>
        /// 隐藏所有UI（内部方法）
        /// </summary>
        /// <param name="ignoreSet">需要忽略的UI ID集合</param>
        void _HideAll(HashSet<int> ignoreSet)
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
                if (ignoreSet != null && ignoreSet.Contains(id))
                {
                    continue;
                }

                if (GetUIData(id) != null)
                {
                    Hide(id, false);
                    continue;
                }

                ForceHideShowedOnly(id);
            }

            _hideAllIdBuffer.Clear();
        }

        /// <summary>
        /// 隐藏所有UI
        /// </summary>
        public void HideAll()
        {
            _HideAll(null);
        }

        /// <summary>
        /// 隐藏所有UI，排除指定ID的UI
        /// </summary>
        /// <param name="ignoreList">需要排除的UI ID列表</param>
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

        /// <summary>
        /// 隐藏所有UI，排除指定类型的UI
        /// </summary>
        /// <param name="ignoreList">需要排除的UI类型列表</param>
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
                _ignoreSet.Add(GetTypeId(ignoreList[i]));
            }
            _HideAll(_ignoreSet);
        }

        /// <summary>
        /// 隐藏指定类型的UI
        /// </summary>
        /// <typeparam name="T">UI类型</typeparam>
        /// <param name="isAnim">是否播放动画</param>
        public void Hide<T>(bool isAnim = true) where T : UIBase
        {
            Hide(GetTypeId(typeof(T)), isAnim);
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
            HideNotStack(GetTypeId(typeof(T)), isAnim);
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
        /// 内部隐藏方法
        /// </summary>
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

            var session = Pool.Get<HideSession>();
            session.Reset(id, rootId, isAnim, checkStack);
            try
            {
                // 1. 在任何隐藏回调改变可见性之前，收集当前根链路的所有目标
                CollectHideTargets(rootId, session.Targets, id, session);
                if (session.Targets.Count == 0)
                {
                    return;
                }

                rootRecord = GetRecord(rootId);
                // 如果不是根界面且当前激活的子界面不是目标界面，则不隐藏
                if (!session.UsedFallback && rootId != id && rootRecord != null && rootRecord.CurrentChildId != id)
                {
                    return;
                }

                if (rootRecord != null && rootRecord.CurrentChildId == id)
                {
                    rootRecord.CurrentChildId = rootId;
                }

                // 2. 先触发所有隐藏，使父子动画可以重叠播放
                BeginHideChain(session);

                // 3. 等待所有待处理的隐藏完成后，再处理栈回退
                await WaitHideChain(session);

                // 从栈中移除根界面
                _stackHandler.RemoveRoot(rootId);
                if (checkStack)
                {
                    // 尝试显示上一个界面
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

        /// <summary>
        /// 开始隐藏链式调用
        /// </summary>
        private void BeginHideChain(HideSession session)
        {
            // 从后往前遍历，确保子界面先隐藏
            for (int i = session.Targets.Count - 1; i >= 0; i--)
            {
                var record = session.Targets[i];
                BeginHideRecord(record, session.IsAnim);
            }
        }

        /// <summary>
        /// 等待隐藏链完成
        /// </summary>
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

        /// <summary>
        /// 开始隐藏单个UI记录
        /// 快速显示->隐藏的竞态会被折叠为立即关闭，不播放隐藏动画
        /// </summary>
        private void BeginHideRecord(UIRecord record, bool isAnim)
        {
            var version = NextRequestVersion(record);
            record.LastHideRequestFrame = Time.frameCount;
            var wasVisible = record.IsVisibleCommitted;
            var wasShowing = record.IsShowingLike;
            var wasFreshlyShown = IsFreshlyShown(record);
            var wasFreshlyVisible = IsFreshlyVisible(record);
            ResetPendingHide(record);
            record.Phase = UIPhase.Hiding;

            // 如果是刚显示就立即隐藏的情况，中断显示动画但不播放隐藏动画
            if (wasFreshlyShown || wasFreshlyVisible)
            {
                record.UI?.DoHide(false, false);
                return;
            }

            // 如果请求在真正的显示提交之前就失去了竞态，立即关闭
            if (!wasVisible && !wasShowing)
            {
                record.Phase = UIPhase.Hidden;
                if (record.UI != null)
                {
                    _cacheHandler.TrackHidden(record);
                }
                CompletePendingHide(record, true);
                return;
            }

            // 正常隐藏流程
            if (record.UI != null && (wasVisible || wasShowing))
            {
                record.UI.DoHide(isAnim, false);
            }
            else if (!IsRequestCurrent(record, version))
            {
                CompletePendingHide(record, false);
            }
        }

        /// <summary>
        /// 收集需要隐藏的目标列表
        /// </summary>
        private void CollectHideTargets(int rootId, List<UIRecord> targets, int requestedId, HideSession session)
        {
            targets.Clear();
            if (_rootRecordIds.TryGetValue(rootId, out var rootSet))
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

                    targets.Add(record);
                }
            }

            // 根链路索引在某些竞态下可能尚未建立或已临时失配，回退到扫描记录表补齐整条根链。
            if (targets.Count == 0)
            {
                session.MarkFallbackUsed();
                foreach (var kv in _records)
                {
                    var record = kv.Value;
                    if (record == null)
                    {
                        continue;
                    }

                    var data = GetUIData(record.Id);
                    var staticRootId = data?.GetParentID() ?? record.Id;
                    if (record.Id != rootId && staticRootId != rootId && record.Id != requestedId)
                    {
                        continue;
                    }

                    if (record.UI == null && !record.IsShowingLike && !record.IsVisibleCommitted)
                    {
                        continue;
                    }

                    targets.Add(record);
                }
            }

            // 排序：层级深的子界面优先隐藏，同层级再按根界面和 Id 稳定排序
            targets.Sort((a, b) =>
            {
                var depthA = GetUIData(a.Id)?.GetParentDepth() ?? 0;
                var depthB = GetUIData(b.Id)?.GetParentDepth() ?? 0;
                if (depthA != depthB) return depthB.CompareTo(depthA);
                if (a.Id == rootId && b.Id != rootId) return 1;
                if (a.Id != rootId && b.Id == rootId) return -1;
                return a.Id.CompareTo(b.Id);
            });
        }

        private void ForceHideShowedOnly(int id)
        {
            if (!_showed.TryGetValue(id, out var ui) || ui == null)
            {
                return;
            }

            _showed.Remove(id);
            ui.DoHide(false, false);
#if UNITY_EDITOR
            __Debugger_Showed_Event();
#endif
            EventMgr.Ins.Run(MainEventType.UI_HIDE, id);
            EventMgr.Ins.Run(MainEventType.UI_HIDE, ui.GetType());
            _blurHandler.OnHide(ui);
        }

        /// <summary>
        /// UI隐藏完成回调
        /// 隐藏完成是提交可见性和生命周期缓存的唯一位置
        /// </summary>
        private void _OnUIHidden(IUI ui)
        {
            var record = GetRecord(ui.ID);
            if (record != null)
            {
                // 从可见记录中分离
                DetachVisibleRecord(record);
                record.Phase = UIPhase.Hidden;
                // 跟踪隐藏的UI到缓存处理器
                _cacheHandler.TrackHidden(record);
                CompletePendingHide(record, true);
            }

#if UNITY_EDITOR
            __Debugger_Showed_Event();
#endif
            // 发送UI隐藏事件
            EventMgr.Ins.Run(MainEventType.UI_HIDE, ui.ID);
            EventMgr.Ins.Run(MainEventType.UI_HIDE, ui.GetType());
            _blurHandler.OnHide(ui);
        }

        /// <summary>
        /// 释放UI资源
        /// </summary>
        private void Dispose(IUI ui)
        {
            var id = ui.ID;
            var record = GetRecord(id);
            ui.Dispose();
            _cacheHandler.RemoveRecord(record);
            RemoveRecord(id);

            // 如果UI有资源包，释放资源包
            var data = GetUIData(id);
            if (data == null || data.Pkgs == null || data.Pkgs.Length == 0)
            {
                return;
            }

            ResMgr.Ins.RemoveUIPackage(data.Pkgs);
            // 对话框类型需要移除UIData
            if (ui is UIDialog)
            {
                RemoveUIData(id);
            }
        }
    }
}
