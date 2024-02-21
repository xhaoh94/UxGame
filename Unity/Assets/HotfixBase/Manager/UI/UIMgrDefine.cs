using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace Ux
{
    public partial class UIMgr
    {
        public readonly struct CallBackData
        {
            public CallBackData(Action<IUI, object, bool> _showCb, Action<IUI> _hideCb, Action<IUI, bool> _stackCb, Action<int, bool> _backCb)
            {
                showCb = _showCb;
                hideCb = _hideCb;
                stackCb = _stackCb;
                backCb = _backCb;
            }
            public readonly Action<IUI> hideCb;
            public readonly Action<IUI, bool> stackCb;
            public readonly Action<int, bool> backCb;
            public readonly Action<IUI, object, bool> showCb;
        }
        public struct BlurStack
        {
            public readonly UIBlur Blur;
            public readonly int ID;
#if UNITY_EDITOR
            public readonly string IDStr;
            public BlurStack(string idStr, int id, UIBlur blur)
            {
                IDStr = idStr;
                ID = id;
                Blur = blur;
            }
#else
            public BlurStack(int id, UIBlur blur)
            {
                ID = id;
                Blur = blur;
            }
#endif
        }
        public struct UIStack
        {
            public readonly int ParentID;
            public int ID;
            public object Param;
            public readonly UIType Type;
#if UNITY_EDITOR
            public readonly string IDStr;
            public UIStack(int parentID, string idStr, int id, object param, UIType type)
            {
                ParentID = parentID;
                IDStr = idStr;
                ID = id;
                Param = param;
                Type = type;
            }
#else
          public UIStack(int parentID, int id, object param, UIType type)
            {
                ParentID = parentID;
                ID = id;
                Param = param;
                Type = type;
            }
#endif
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
            public string Name => ui.Name;
            public void Init(IUI _ui)
            {
                ui = _ui;
                timeKey = TimeMgr.Ins.DoOnce(_waitDelTime, this, Exe); //一段时间后执行删除                
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
                MessageBox._waitDels.Remove(ui.ID);
                Tip._waitDels.Remove(ui.ID);
                Ins._waitDels.Remove(ui.ID);
                Ins.Dispose(ui);
                Release();
            }

            public void GetUI(out IUI outUI)
            {
                outUI = ui;
                MessageBox._waitDels.Remove(ui.ID);
                Tip._waitDels.Remove(ui.ID);
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