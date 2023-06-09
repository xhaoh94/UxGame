using Cysharp.Threading.Tasks;
using FairyGUI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Ux
{
    public class UIMgr : Singleton<UIMgr>
    {
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
                timeKey = TimeMgr.Ins.DoOnce(5, Exe); //5秒后执行删除                
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

            public void GetUI(out IUI outUi)
            {
                outUi = ui;
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

        //对话弹窗
        public static readonly UIDialogFactory Dialog = new UIDialogFactory();

        //窗口类型对应的ID
        private readonly Dictionary<Type, int> _typeToId = new Dictionary<Type, int>();

        private readonly Dictionary<int, IUIData> _idUIData = new Dictionary<int, IUIData>();

        //动态创建的UI数据
        private readonly List<int> dymUIData = new List<int>();

        //界面缓存，关闭不销毁的界面会缓存起来
        private readonly Dictionary<int, IUI> _cacel = new Dictionary<int, IUI>();

        //临时界面缓存，关闭销毁的界面，如果父界面没销毁，
        //会临时缓存起来，等父界面关闭了，再销毁
        private readonly Dictionary<int, IUI> _temCacel = new Dictionary<int, IUI>();
        private readonly Dictionary<int, List<int>> _bottomTemCacel = new Dictionary<int, List<int>>();

        //正在显示中的ui列表
        private readonly List<int> _showing = new List<int>();

        //已经显示的ui列表
        private readonly Dictionary<int, IUI> _showed = new Dictionary<int, IUI>();

        //等待销毁的界面
        private readonly Dictionary<int, WaitDel> _waitDels = new Dictionary<int, WaitDel>();

        //创建完需要关闭的界面（用于打开界面后正在加载的时候，在其他地方又马上关闭了界面）
        private readonly List<int> _createdDels = new List<int>();

        //界面对应的懒加载标签
        private readonly Dictionary<int, string[]> _idLazyloads = new Dictionary<int, string[]>();

        private readonly Dictionary<int, Downloader> _idDownloader = new Dictionary<int, Downloader>();

        //UI层级
        private readonly Dictionary<UILayer, GComponent> _layerCom = new Dictionary<UILayer, GComponent>()
        {
            { UILayer.Root, GRoot.inst },
            { UILayer.Bottom, _CreateLayer(UILayer.Bottom, -100) },
            { UILayer.Normal, _CreateLayer(UILayer.Normal, 0) },
            { UILayer.Tip, _CreateLayer(UILayer.Tip, 90) },
            { UILayer.Top, _CreateLayer(UILayer.Top, 100) }
        };

        static GComponent _CreateLayer(UILayer layer, int v)
        {
            var com = new GComponent();
            com.name = com.gameObjectName = layer.ToString();
            com.sortingOrder = v;
            GRoot.inst.AddChild(com);
            com.MakeFullScreen();
            com.AddRelation(GRoot.inst, RelationType.Size);
            return com;
        }

        public UIMgr()
        {
            //StageCamera.main.clearFlags = CameraClearFlags.Nothing;
            if (PatchMgr.Ins.IsDone)
            {
                StageCamera.main.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;
            }
        }

        //内存不足时，清理缓存
        public void OnLowMemory()
        {
            Dialog?.Clear();
            if (_cacel.Count > 0)
            {
                var ids = _cacel.Keys.ToList();
                for (var i = ids.Count - 1; i >= 0; i--)
                {
                    var id = ids[i];
                    if (!_cacel.TryGetValue(id, out IUI ui)) continue;
                    Dispose(ui);
                }

                _cacel.Clear();
            }

            if (_temCacel.Count > 0)
            {
                var ids = _temCacel.Keys.ToList();
                for (var i = ids.Count - 1; i >= 0; i--)
                {
                    var id = ids[i];
                    if (!_temCacel.TryGetValue(id, out IUI ui)) continue;
                    Dispose(ui);
                }

                _temCacel.Clear();
            }

            if (_waitDels.Count > 0)
            {
                var ids = _waitDels.Keys.ToList();
                for (int i = ids.Count - 1; i >= 0; i--)
                {
                    var id = ids[i];
                    if (!_waitDels.TryGetValue(id, out var wd)) continue;
                    wd.Dispose();
                }

                _waitDels.Clear();
            }

#if UNITY_EDITOR
            __Debugger_Event();
#endif
        }

        public void Release()
        {
            OnLowMemory();
            //清理掉动态创建的UI数据
            if (dymUIData.Count > 0)
            {
                foreach (var id in dymUIData)
                {
                    _idUIData.Remove(id);
                }

                dymUIData.Clear();
            }

            //清理掉正在下载的资源
            if (_idDownloader.Count > 0)
            {
                foreach (var kv in _idDownloader)
                {
                    var downloader = kv.Value;
                    downloader.CancelDownload();
                }

                _idDownloader.Clear();
            }
        }

        public void Add(List<UIParse> uis)
        {
            uis.ForEach(ui => { ui.Add(_idUIData); });
            uis.ForEach(ui => { ui.Parse(_idUIData); });
#if UNITY_EDITOR
            __Debugger_UI_Event();
#endif
        }

        /// <summary>
        /// 注册UI界面，一般用于动态创建UI界面
        /// </summary>
        /// <param name="data"></param>
        public void RegisterUI(IUIData data)
        {
            if (HasUIData(data.ID))
            {
#if UNITY_EDITOR
                Log.Error("重复注册UI面板:{0}", data.IDStr);
#else
                Log.Error("重复注册UI面板:{0}", data.ID);
#endif
                return;
            }

            _idUIData.Add(data.ID, data);
            dymUIData.Add(data.ID);
#if UNITY_EDITOR
            __Debugger_UI_Event();
#endif
        }

        /// <summary>
        /// 注销UI界面，一般用于动态创建界面的销毁
        /// </summary>
        /// <param name="id"></param>
        public void LogoutUI(int id)
        {
            if (!_idUIData.Remove(id)) return;
            dymUIData.Remove(id);
#if UNITY_EDITOR
            __Debugger_UI_Event();
#endif
        }

        /// <summary>
        /// 获取已注册的UIData
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public IUIData GetUIData(int id)
        {
            if (_idUIData.TryGetValue(id, out var data))
            {
                return data;
            }
            Log.Error($"没有注册UIID[{id}]");
            return null;
        }

        public bool HasUIData(int id)
        {
            return _idUIData.ContainsKey(id);
        }

        public GComponent GetLayer(UILayer layer)
        {
            if (_layerCom.TryGetValue(layer, out var com)) return com;
            return GRoot.inst;
        }

        //获取UI
        public T GetUI<T>(int id) where T : IUI
        {
            if (!_showed.ContainsKey(id)) return default(T);
            return (T)_showed[id];
        }

        public T GetUI<T>() where T : IUI
        {
            return GetUI<T>(ConverterID(typeof(T)));
        }

        public bool IsShow<T>() where T : UIBase
        {
            return IsShow(ConverterID(typeof(T)));
        }

        public bool IsShow(int id)
        {
            return _showed.TryGetValue(id, out var ui) && (ui.State == UIState.ShowAnim || ui.State == UIState.Show);
        }

        int ConverterID(Type type)
        {
            if (_typeToId.TryGetValue(type, out var id))
            {
                return id;
            }
            id = type.FullName.ToHash();
            _typeToId.Add(type, id);
            return id;
        }
        public UITask<T> Show<T>(object param = null, bool isAnim = true) where T : IUI
        {
            return Show<T>(ConverterID(typeof(T)), param, isAnim);
        }

        public UITask<T> Show<T>(int id, object param = null, bool isAnim = true) where T : IUI
        {
            var task = ShowAsync<T>(id, param, isAnim);
            return new UITask<T>(task);
        }

        public UITask<IUI> Show(int id, object param = null, bool isAnim = true)
        {
            var task = ShowAsync<IUI>(id, param, isAnim);
            return new UITask<IUI>(task);
        }

        private async UniTask<T> ShowAsync<T>(int id, object param = null, bool isAnim = true) where T : IUI
        {
            var data = GetUIData(id);
            if (data == null)
            {
                return default;
            }

            var top = data.GetTopID();
            if (CheckDownload(top))
            {
                return default;
            }

            if (_createdDels.Contains(id))
            {
                _createdDels.Remove(id);
            }

            var arr = new List<IUI>();
            var succ = await ToShow(top, arr);
            if (succ)
            {
                foreach (var uiid in arr.Select(ui => ui.ID).Where(uiid => _createdDels.Contains(uiid)))
                {
                    succ = false;
                    _createdDels.Remove(uiid);
                }
            }

            if (succ)
            {
                foreach (var ui in arr)
                {
                    var uiid = ui.ID;
                    if (_showed.ContainsKey(uiid))
                    {
                        ui.DoResume(uiid == id ? param : null);
                        EventMgr.Ins.Send(MainEventType.UI_RESUME, uiid);
                    }
                    else
                    {
                        ui.DoShow(isAnim, uiid == id ? param : null);
                        _showed.Add(uiid, ui);
                        _showing.Remove(uiid);
                        EventMgr.Ins.Send(MainEventType.UI_SHOW, uiid);
                    }
                }
#if UNITY_EDITOR
                __Debugger_Showing_Event();
                __Debugger_Showed_Event();
#endif
                return (T)_showed[id];
            }
            else
            {
                foreach (var ui in arr)
                {
                    var uiid = ui.ID;
                    CheckDestroy(ui);
                    _showing.Remove(uiid);
                }
#if UNITY_EDITOR
                __Debugger_Showing_Event();
                __Debugger_Showed_Event();
#endif
                return default;
            }
        }

        private async UniTask<bool> ToShow(int id, ICollection<IUI> arr)
        {
            var data = GetUIData(id);
            if (data == null)
            {
                Log.Error("没有找到{0}对应的UIData", id);
                return false;
            }

            if (_showed.TryGetValue(id, out var ui))
            {
                arr.Add(ui);
                return true;
            }

            if (_showing.Contains(id))
            {
                float time = Time.unscaledTime;
                while (true)
                {
                    await UniTask.Yield();
                    if (_showed.TryGetValue(id, out ui)) break;
                    if (!_showing.Contains(id)) break;
                    if (Time.unscaledTime - time > 10f) break; //超时
                }

                if (ui == null) return false;
                arr.Add(ui);
                return true;
            }

            _showing.Add(id);
#if UNITY_EDITOR
            __Debugger_Showing_Event();
#endif

            if (data.TabData != null && data.TabData.PID != 0)
            {
                if (!await ToShow(data.TabData.PID, arr))
                {
                    _showing.Remove(id);
#if UNITY_EDITOR
                    __Debugger_Showing_Event();
#endif
                    return false;
                }
            }

            if (_waitDels.TryGetValue(id, out var wd))
            {
                wd.GetUI(out ui);
            }
            else
            {
                if (_temCacel.TryGetValue(id, out ui))
                {
                    _temCacel.Remove(id);
                    var temBottomId = ui.Data.GetBottomID();
                    if (_bottomTemCacel.TryGetValue(temBottomId, out var temList))
                    {
                        if (temList.Remove(id))
                        {
                            if (temList.Count == 0)
                            {
                                _bottomTemCacel.Remove(temBottomId);
                            }
                        }
                    }
#if UNITY_EDITOR
                    __Debugger_TemCacel_Event();
#endif
                }
                else if (_cacel.TryGetValue(id, out ui))
                {
                    _cacel.Remove(id);
#if UNITY_EDITOR
                    __Debugger_Cacel_Event();
#endif
                }
                else
                {
                    ui = await CreateUI(data);
                }
            }

            if (ui == null)
            {
                _showing.Remove(id);
#if UNITY_EDITOR
                __Debugger_Showing_Event();
#endif
                return false;
            }

            arr.Add(ui);
            return true;
        }

        private async UniTask<IUI> CreateUI(IUIData data)
        {
            if (data.Pkgs is { Length: > 0 })
            {
                if (!await ResMgr.Ins.LoaUIdPackage(data.Pkgs))
                {
#if UNITY_EDITOR
                    Log.Error($"[{data.IDStr}]包加载错误");
#else
                    Log.Error($"[{data.ID}]包加载错误");
#endif

                    return null;
                }
            }
            var ui = (IUI)Activator.CreateInstance(data.CType);
            ui.InitData(data, Remove);
            return ui;
        }

        public void HideAll(List<int> ignoreList = null)
        {
            bool Func(int id)
            {
                return ignoreList is { Count: > 0 } && ignoreList.Contains(id);
            }

            foreach (var id in _showing.Where(id => !Func(id)))
            {
                Hide(id, false);
            }

            var ids = _showed.Keys.ToList();
            foreach (var id in ids.Where(id => !Func(id)))
            {
                Hide(id, false);
            }
        }
        public void HideAll(List<string> ignoreList = null)
        {
            bool Func(int id)
            {
                return ignoreList is { Count: > 0 } && ignoreList.FindIndex(x => x.ToHash() == id) >= 0;
            }
            _HideAll(Func);
        }

        public void HideAll(List<Type> ignoreList = null)
        {
            bool Func(int id)
            {
                return ignoreList is { Count: > 0 } && ignoreList.FindIndex(x => ConverterID(x) == id) >= 0;
            }
            _HideAll(Func);
        }

        void _HideAll(Func<int, bool> func)
        {
            foreach (var id in _showing.Where(id => !func(id)))
            {
                Hide(id, false);
            }

            var ids = _showed.Keys.ToList();
            foreach (var id in ids.Where(id => !func(id)))
            {
                Hide(id, false);
            }
        }

        public void Hide<T>(bool isAnim = true) where T : UIBase
        {
            Hide(ConverterID(typeof(T)), isAnim);
        }

        public void Hide(int id, bool isAnim = true)
        {
            if (_showing.Contains(id))
            {
                if (!_createdDels.Contains(id)) _createdDels.Add(id);
                return;
            }

            if (!_showed.ContainsKey(id))
            {
                return;
            }

            if (!_showed.TryGetValue(id, out IUI ui))
            {
                return;
            }

            if (ui.State == UIState.HideAnim || ui.State == UIState.Hide)
            {
                return;
            }
            var bottom = ui.Data.GetBottomID();
            if (bottom != 0 && _showed.TryGetValue(bottom, out ui))
            {
                ui.DoHide(isAnim);
            }
        }

        private void Remove(IUI ui)
        {
            var id = ui.ID;
            _showed.Remove(id);
            CheckDestroy(ui);
#if UNITY_EDITOR
            __Debugger_Showed_Event();
#endif
            EventMgr.Ins.Send(MainEventType.UI_HIDE, id);
        }

        private void CheckDestroy(IUI ui)
        {
            var id = ui.ID;
            var bottomID = ui.Data.GetBottomID();
            if (ui.IsDestroy)
            {
                //存在父界面，且父界面还没关闭，则放入临时缓存中
                if (bottomID != id && IsShow(bottomID))
                {
                    if (_temCacel.ContainsKey(id))
                    {
#if UNITY_EDITOR
                        Log.Error($"界面[{ui.IDStr}]多次放入临时缓存列表");
#else
                        Log.Error($"界面[{id}]多次放入临时缓存列表");
#endif

                        return;
                    }

                    _temCacel.Add(id, ui);
                    if (!_bottomTemCacel.TryGetValue(bottomID, out var temList))
                    {
                        temList = new List<int>();
                        _bottomTemCacel.Add(bottomID, temList);
                    }

                    temList.Add(id);
#if UNITY_EDITOR
                    __Debugger_TemCacel_Event();
#endif
                }
                else
                {
                    if (_waitDels.ContainsKey(id))
                    {
#if UNITY_EDITOR
                        Log.Error($"界面[{ui.IDStr}]多次放入待删除列表");
#else
                        Log.Error($"界面[{id}]多次放入待删除列表");
#endif

                        return;
                    }

                    var wd = Pool.Get<WaitDel>();
                    wd.Init(ui);
                    _waitDels.Add(id, wd);
#if UNITY_EDITOR
                    __Debugger_WaitDel_Event();
#endif
                }
            }
            else
            {
                //不销毁的界面放进缓存列表
                if (_cacel.ContainsKey(id))
                {
#if UNITY_EDITOR
                    Log.Error($"界面[{ui.IDStr}]多次放入待删除列表");
#else
                    Log.Error($"界面[{id}]多次放入待删除列表");
#endif                    
                    return;
                }

                _cacel.Add(id, ui);
#if UNITY_EDITOR
                __Debugger_Cacel_Event();
#endif
            }

            //如果此界面是最底层的界面，则将属于此界面的临时缓存界面从列表清除
            if (id == bottomID)
            {
                if (_bottomTemCacel.TryGetValue(id, out var temList))
                {
                    foreach (var cacelId in temList)
                    {
                        if (_temCacel.TryGetValue(cacelId, out var temUI))
                        {
                            _temCacel.Remove(cacelId);
                            CheckDestroy(temUI);
                        }
                    }

                    temList.Clear();
                    _bottomTemCacel.Remove(id);
#if UNITY_EDITOR
                    __Debugger_TemCacel_Event();
#endif
                }
            }
        }

        private void Dispose(IUI ui)
        {
            var id = ui.ID;
            ui.Dispose();
            var data = GetUIData(id);
            if (data == null) return;
            if (data.Pkgs == null || data.Pkgs.Length == 0) return;
            ResMgr.Ins.RemoveUIPackage(data.Pkgs);
            if (ui is UIDialog)
            {
                LogoutUI(id);
            }
        }

        /// <summary>
        /// 获取懒加载标签列表
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        string[] GetDependenciesLazyload(int id)
        {
            if (id == 0) return null;
            if (!_idLazyloads.TryGetValue(id, out var lazyloads))
            {
                var data = GetUIData(id);
                if (data == null)
                {
                    _idLazyloads.Add(id, null);
                    return null;
                }

                var temLazyloads = new List<string>();
                while (data != null)
                {
                    if (data.Lazyloads != null)
                    {
                        foreach (var lazyload in data.Lazyloads)
                        {
                            if (!temLazyloads.Contains(lazyload))
                            {
                                temLazyloads.Add(lazyload);
                            }
                        }
                    }

                    if (data.TabData == null)
                    {
                        break;
                    }

                    if (data.TabData.PID == 0)
                    {
                        break;
                    }

                    data = GetUIData(data.TabData.PID);
                }

                lazyloads = temLazyloads.ToArray();
                _idLazyloads.Add(id, lazyloads);
            }

            return lazyloads;
        }

        bool CheckDownload(int id)
        {
            if (_idDownloader.TryGetValue(id, out var download))
            {
                if (download.IsDone)
                {
                    _idDownloader.Remove(id);
                    return false;
                }

                //TODO 显示下载界面
                return true;
            }

            var tags = GetDependenciesLazyload(id);
            if (tags == null || tags.Length == 0) return false;
            download = ResMgr.Lazyload.GetDownloaderByTags(tags);
            if (download == null) return false;
            Log.Debug($"一共发现了{download.TotalDownloadCount}个资源需要更新下载。");
            Dialog.DoubleBtn(
                "下载",
                $"一共发现了{download.TotalDownloadCount}个资源需要更新下载。",
                "下载",
                () =>
                {
                    //TODO 显示下载界面
                    _idDownloader.Add(id, download);
                    download.Download();
                },
                "取消", null);
            return true;
        }


#if UNITY_EDITOR
        public static void __Debugger_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                __Debugger_UI_Event();
                __Debugger_Showed_Event();
                __Debugger_Showing_Event();
                __Debugger_Cacel_Event();
                __Debugger_TemCacel_Event();
                __Debugger_WaitDel_Event();
            }
        }

        public static void __Debugger_UI_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                var list = new Dictionary<string, IUIData>();
                foreach (var kv in Ins._idUIData)
                {
                    list.Add(kv.Value.IDStr, kv.Value);
                }
                __Debugger_UI_CallBack?.Invoke(list);
            }
        }

        public static void __Debugger_Showed_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                var list = new List<string>();
                foreach (var kv in Ins._showed)
                {
                    list.Add(kv.Value.IDStr);
                }
                __Debugger_Showed_CallBack?.Invoke(list);
            }
        }

        public static void __Debugger_Showing_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                var list = new List<string>();
                foreach (var id in Ins._showing)
                {
                    list.Add(Ins.GetUIData(id).IDStr);
                }
                __Debugger_Showing_CallBack?.Invoke(list);
            }
        }

        public static void __Debugger_Cacel_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                var list = new List<string>();
                foreach (var kv in Ins._cacel)
                {
                    list.Add(kv.Value.IDStr);
                }
                __Debugger_Cacel_CallBack?.Invoke(list);
            }
        }

        public static void __Debugger_TemCacel_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                var list = new List<string>();
                foreach (var kv in Ins._temCacel)
                {
                    list.Add(kv.Value.IDStr);
                }
                __Debugger_TemCacel_CallBack?.Invoke(list);
            }
        }

        public static void __Debugger_WaitDel_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                var list = new List<string>();
                foreach (var kv in Ins._waitDels)
                {
                    list.Add(kv.Value.IDStr);
                }
                __Debugger_WaitDel_CallBack?.Invoke(list);
            }
        }

        public static Action<Dictionary<string, IUIData>> __Debugger_UI_CallBack;
        public static Action<List<string>> __Debugger_Showed_CallBack;
        public static Action<List<string>> __Debugger_Showing_CallBack;
        public static Action<List<string>> __Debugger_Cacel_CallBack;
        public static Action<List<string>> __Debugger_TemCacel_CallBack;
        public static Action<List<string>> __Debugger_WaitDel_CallBack;

#endif
    }
}