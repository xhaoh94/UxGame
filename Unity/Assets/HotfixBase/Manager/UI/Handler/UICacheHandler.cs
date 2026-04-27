using System.Collections.Generic;
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
            // 遍历 Cache 并清理
            foreach (var kv in Cache)
            {
                _callback.DisposeUI(kv.Value);
            }
            Cache.Clear();

            while (WaitDels.Count > 0)
            {
                var firstKey = 0;
                WaitDel firstValue = null;

                // 获取第一个键值对
                using (var enumerator = WaitDels.GetEnumerator())
                {
                    if (enumerator.MoveNext())
                    {
                        firstKey = enumerator.Current.Key;
                        firstValue = enumerator.Current.Value;
                    }
                }

                firstValue?.Dispose();
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
                if (!Cache.TryAdd(id, ui))
                {
                    Log.Error($"界面[{ui.Name}]多次放入缓存列表");
                    return;
                }
#if UNITY_EDITOR
                __Debugger_Cacel_Event();
#endif
            }
            else
            {
                var wd = Pool.Get<WaitDel>();
                wd.Init(ui, OnRemoveFromWaitDel, OnDisposeFromWaitDel);
                if (!WaitDels.TryAdd(id, wd))
                {
                    Log.Error($"界面[{ui.Name}]多次放入待删除列表");
                    wd.Dispose();
                    return;
                }
#if UNITY_EDITOR
                __Debugger_WaitDel_Event();
#endif
            }
        }
    }
}