using FairyGUI;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using static Ux.UIMgr;

namespace Ux
{
    public interface IUI
    {
        UIState State { get; }
        UIType Type { get; }
        UIBlur Blur { get; }
        string Name { get; }
        int ID { get; }
        IUIData Data { get; }
        int HideDestroyTime { get; }
        bool Visable { get; set; }
        IFilter Filter { get; set; }
        void InitData(IUIData data, CallBackData initData);
        void Dispose();
        UniTask DoShow(bool isAnim, int id, IUIParam param, bool isStack);
        void DoHide(bool isAnim, bool checkStack);
    }
    
    public enum UIType
    {
        /// <summary>
        /// 打开时不会关闭任何界面，但会被Stack界面打开的时候关闭（Stack界面关闭的时候，会自动重新打开）
        /// </summary>
        Normal,
        /// <summary>
        /// 打开时会关闭除Fixed之外的界面，也会被其他Stack界面打开的时候关闭（Stack界面关闭的时候，会自动重新打开） 
        /// </summary>
        Stack,
        /// <summary>
        /// 固定界面,不会关闭其他界面，也不会被其他界面关闭
        /// </summary>
        Fixed,
    }

    //必须是2的n次方,可组合使用 (PS:Blur|Fixed)
    public enum UIBlur
    {
        /// <summary>
        /// 不会模糊其他界面也不会被其他界面模糊
        /// </summary>
        None = 0x1,
        /// <summary>
        /// 不会模糊其他界面但会被其他界面模糊
        /// </summary>
        Normal = 0x2,
        /// <summary>
        /// 模糊非固定界面
        /// </summary>
        Blur = 0x4,
        /// <summary>
        /// 模糊固定界面
        /// </summary>
        Fixed = 0x8,
        /// <summary>
        /// 模糊场景
        /// </summary>
        Scene = 0x16,
    }
    public partial class UIMgr
    {
        public readonly struct CallBackData
        {
            public CallBackData(Action<IUI, IUIParam, bool> _showCb, Action<IUI> _hideCb, Func<IUI, bool, bool> _stackCb)
            {
                showCb = _showCb;
                hideCb = _hideCb;
                stackCb = _stackCb;                
            }
            public readonly Action<IUI, IUIParam, bool> showCb;
            public readonly Action<IUI> hideCb;
            public readonly Func<IUI, bool, bool> stackCb;            
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
            public IUIParam Param;
            public bool ParamIsNew;
            public readonly UIType Type;

            public UIStack(int parentID, int id, IUIParam param, UIType type)
            {
                ParentID = parentID;
                ID = id;
                Param = param;
                ParamIsNew = false;
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

        public readonly struct ItemUrlParse
        {
            public ItemUrlParse(Type type, string url)
            {
                Type = type;
                Url = url;
            }
            Type Type { get; }
            string Url { get; }

            public void Add(Dictionary<Type, string> _urls)
            {
                if (_urls.ContainsKey(Type))
                {
                    Log.Error("ItemRenderer 注册URL 重复了。Type[{0}]", Type.FullName);
                    return;
                }
                _urls.Add(Type, Url);
            }
        }

        public readonly struct UITask<T> where T : IUI
        {
            readonly UniTask<T> _task;

            public UITask(UniTask<T> task)
            {
                _task = task;
            }

            public UniTask<T> Task()
            {
                return _task;
            }
        }

        public readonly struct DownloadData
        {
            public DownloadData(int uiid, IUIParam param, bool isAnim)
            {
                this.UIID = uiid;
                this.Param = param;
                this.IsAnim = isAnim;
            }
            public int UIID { get; }
            public IUIParam Param { get; }
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
                timeKey = TimeMgr.Ins.DoOnce(_ui.HideDestroyTime, this, Exe); //一段时间后执行删除                
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