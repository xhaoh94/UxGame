using System.Collections.Generic;

namespace Ux
{
    internal class UILifecycleCache
    {
        private readonly IUICacheHandlerCallback _callback;
        private readonly Dictionary<int, IUI> _cache = new Dictionary<int, IUI>();
        private readonly Dictionary<int, UIMgr.WaitDel> _waitDels = new Dictionary<int, UIMgr.WaitDel>();

        internal UILifecycleCache(IUICacheHandlerCallback callback)
        {
            _callback = callback;
        }

        internal bool TryTakeCached(int id, out IUI ui)
        {
            if (_cache.TryGetValue(id, out ui))
            {
                _cache.Remove(id);
#if UNITY_EDITOR
                UIMgr.__Debugger_Cacel_Event();
#endif
                return true;
            }

            if (_waitDels.TryGetValue(id, out var waitDel))
            {
                waitDel.GetUI(out ui);
                _waitDels.Remove(id);
                return true;
            }

            ui = null;
            return false;
        }

        // Cache only deals with instance lifetime after hide; request cancellation is handled by UIMgr versions.
        internal void TrackHidden(UIMgr.UIRecord record)
        {
            if (record?.UI == null)
            {
                return;
            }

            record.CacheState = UIMgr.CacheState.None;
            record.WaitDel = null;
            TrackHidden(record.UI, record.Id, record);
        }

        internal void TrackHidden(IUI ui, int id)
        {
            TrackHidden(ui, id, null);
        }

        private void TrackHidden(IUI ui, int id, UIMgr.UIRecord record)
        {
            if (ui == null)
            {
                return;
            }

            if (ui.HideDestroyTime < 0)
            {
                _cache[id] = ui;
                if (record != null)
                {
                    record.CacheState = UIMgr.CacheState.Cached;
                }
#if UNITY_EDITOR
                UIMgr.__Debugger_Cacel_Event();
#endif
                return;
            }

            var wd = Pool.Get<UIMgr.WaitDel>();
            wd.Init(ui, OnRemoveWaitDel, OnDisposeWaitDel);
            _waitDels[id] = wd;
            if (record != null)
            {
                record.WaitDel = wd;
                record.CacheState = UIMgr.CacheState.WaitDestroy;
            }
#if UNITY_EDITOR
            UIMgr.__Debugger_WaitDel_Event();
#endif
        }

        internal void RemoveRecord(UIMgr.UIRecord record)
        {
            if (record == null)
            {
                return;
            }

            _cache.Remove(record.Id);
            if (_waitDels.TryGetValue(record.Id, out var waitDel))
            {
                _waitDels.Remove(record.Id);
                if (!ReferenceEquals(record.WaitDel, waitDel))
                {
                    waitDel.Dispose();
                }
            }

            record.WaitDel = null;
            record.CacheState = UIMgr.CacheState.None;
        }

        internal void ClearMemory()
        {
            foreach (var kv in _cache)
            {
                _callback.DisposeUI(kv.Value);
            }
            _cache.Clear();

            if (_waitDels.Count == 0)
            {
                return;
            }

            var keys = Pool.Get<List<int>>();
            keys.AddRange(_waitDels.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                if (_waitDels.TryGetValue(keys[i], out var wd))
                {
                    wd.Dispose();
                }
            }
            keys.Clear();
            Pool.Push(keys);
        }

        internal List<string> GetCacheDebugNames()
        {
            var list = new List<string>();
            foreach (var kv in _cache)
            {
                list.Add(kv.Value.Name);
            }
            return list;
        }

        internal List<string> GetWaitDestroyDebugNames()
        {
            var list = new List<string>();
            foreach (var kv in _waitDels)
            {
                list.Add(kv.Value.Name);
            }
            return list;
        }

        void OnRemoveWaitDel(int id)
        {
            _waitDels.Remove(id);
#if UNITY_EDITOR
            UIMgr.__Debugger_WaitDel_Event();
#endif
        }

        void OnDisposeWaitDel(IUI ui)
        {
            var record = UIMgr.Ins.GetRecord(ui.ID);
            if (record != null)
            {
                record.UI = null;
                record.Phase = UIMgr.UIPhase.Disposed;
                record.WaitDel = null;
                record.CacheState = UIMgr.CacheState.None;
                UIMgr.Ins.RemoveRecord(ui.ID);
            }
            _callback.DisposeUI(ui);
        }
    }
}
