using System.Collections.Generic;
using System.Linq;
using static Ux.UIMgr;

namespace Ux
{
    public class UICacheHandler
    {
        private readonly IUICacheHandlerCallback _callback;

        public List<int> CreatedDels { get; } = new List<int>();
        public Dictionary<int, WaitDel> WaitDels { get; } = new Dictionary<int, WaitDel>();
        public Dictionary<int, IUI> Cache { get; } = new Dictionary<int, IUI>();

        public UICacheHandler(IUICacheHandlerCallback callback)
        {
            _callback = callback;
        }

        public void ClearMemory()
        {
            if (Cache.Count > 0)
            {
                var ids = Cache.Keys.ToList();
                for (var i = ids.Count - 1; i >= 0; i--)
                {
                    var id = ids[i];
                    if (Cache.TryGetValue(id, out IUI ui)) _callback.DisposeUI(ui);
                    Cache.Remove(id);
                }
            }

            if (WaitDels.Count > 0)
            {
                var ids = WaitDels.Keys.ToList();
                for (int i = ids.Count - 1; i >= 0; i--)
                {
                    var id = ids[i];
                    if (WaitDels.TryGetValue(id, out var wd)) wd.Dispose();
                }
                WaitDels.Clear();
            }
        }

        void OnRemoveFromWaitDel(int id)
        {
            WaitDels.Remove(id);
        }
        void OnDisposeFromWaitDel(IUI ui)
        {
            _callback.DisposeUI(ui);
        }

        public void CheckDestroy(IUI ui)
        {
            var id = ui.ID;
            if (ui.HideDestroyTime < 0)
            {
                if (Cache.ContainsKey(id))
                {
                    Log.Error($"界面[{ui.Name}]多次放入缓存列表");
                    return;
                }

                Cache.Add(id, ui);
#if UNITY_EDITOR
                __Debugger_Cacel_Event();
#endif
            }
            else
            {
                if (WaitDels.ContainsKey(id))
                {
                    Log.Error($"界面[{ui.Name}]多次放入待删除列表");
                    return;
                }
                var wd = Pool.Get<WaitDel>();
                wd.Init(ui, OnRemoveFromWaitDel, OnDisposeFromWaitDel);
                WaitDels.Add(id, wd);
#if UNITY_EDITOR
                __Debugger_WaitDel_Event();
#endif
            }
        }
    }
}