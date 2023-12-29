using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace Ux
{
    public partial class UIMgr
    {
        struct UIStack
        {
            public readonly int ParentID;
            public int ID;
            public readonly object Param;
            public readonly UIType Type;
            public UIStack(int parentID, int id, object param, UIType type)
            {
                ParentID = parentID;
                ID = id;
                Param = param;
                Type = type;
            }
        }
        public readonly struct UIParse
        {
            public UIParse(Type type, int id, IUITabData tabData)
            {
                id = id == 0 ? type.FullName.ToHash() : id;
                Data = new UIData(id, type, tabData);
            }

            UIData Data { get; }

            public void Add(Dictionary<int, IUIData> _id2data)
            {
                if (_id2data.ContainsKey(Data.ID))
                {
                    Log.Error("UIData注册重复了。ID[{0}]", Data.ID);
                    return;
                }

                _id2data.Add(Data.ID, Data);
            }

            public void Parse(Dictionary<int, IUIData> _id2data)
            {
                if (Data.TabData == null) return;

                if (Data.TabData.PID == 0)
                {
                    Log.Error("UITabData父ID为空。ID[{0}]", Data.ID);
                    return;
                }

                var pId = Data.TabData.PID;
                if (!_id2data.TryGetValue(pId, out var pIData))
                {
                    Log.Error("UIData注册的父面板不存在。ID[{0}]", Data.ID);
                    return;
                }

                var pData = pIData as UIData;
                pData?.Children.Add(Data.ID);
            }
        }

        public readonly struct UITask<T> where T : IUI
        {
            readonly UniTask<T> task;

            public UITask(UniTask<T> _task)
            {
                task = _task;
            }

            public UniTask<T> Task()
            {
                return task;
            }
        }

        public readonly struct DownloadData
        {
            public DownloadData(int uiid, object param, bool isAnim)
            {
                this.UIID = uiid;
                this.Param = param;
                this.IsAnim = isAnim;
            }
            public int UIID { get; }
            public object Param { get; }
            public bool IsAnim { get; }
        }
        private class WaitDel
        {
            IUI ui;
            long timeKey;
#if UNITY_EDITOR
            public string IDStr => ui.IDStr;
#endif
            public void Init(IUI _ui)
            {
                ui = _ui;
                timeKey = TimeMgr.Ins.DoOnce(_waitDelTime, Exe); //一段时间后执行删除                
            }

            void Release()
            {
                RemoveTime();
                ui = null;
                Pool.Push(this);
#if UNITY_EDITOR
                __Debugger_WaitDel_Event();
#endif
            }

            public void Dispose()
            {
                Dialog._waitDels.Remove(ui.ID);
                Ins._waitDels.Remove(ui.ID);
                Ins.Dispose(ui);
                Release();
            }

            public void GetUI(out IUI outUI)
            {
                outUI = ui;
                Dialog._waitDels.Remove(ui.ID);
                Ins._waitDels.Remove(ui.ID);
                Release();
            }

            void Exe()
            {
                if (timeKey == 0 || ui == null) return;
                timeKey = 0;
                Dispose();
            }

            void RemoveTime()
            {
                if (timeKey == 0) return;
                TimeMgr.Ins.RemoveKey(timeKey);
                timeKey = 0;
            }
        }

    }
}