using Cysharp.Threading.Tasks;
using FairyGUI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using YooAsset;

namespace Ux
{
    public class UIPkgRef
    {
        public void Init(string pkg, int refCnt)
        {
            PkgName = pkg;
            RefCnt = refCnt;
        }

        public string PkgName { get; private set; }
        public int RefCnt { get; private set; }

        public void Add()
        {
            RefCnt++;
        }

        public bool Remove()
        {
            RefCnt--;
            return RefCnt == 0;
        }

        public void Release()
        {
            PkgName = string.Empty;
            RefCnt = 0;
            Pool.Push(this);
        }
    }

    public partial class ResMgr
    {
        #region FGUI

        private readonly Dictionary<string, UIPkgRef> _pkgToRef = new Dictionary<string, UIPkgRef>();

        private readonly Dictionary<string, List<AssetHandle>> _pkgToHandles =
            new Dictionary<string, List<AssetHandle>>();

        private readonly Dictionary<string, bool> _pkgToLoading = new Dictionary<string, bool>();

        public void RemoveUIPackage(string[] pkgs)
        {
            //热更没完成前，加载资源的语柄都是直接Release，
            //不会计算引用计数，所以不需要走此逻辑
            if (!PatchMgr.Ins.IsDone)
            {
                return;
            }

            if (pkgs.Length == 0) return;
            foreach (var pkg in pkgs)
            {
                if (_pkgToLoading.ContainsKey(pkg))
                {
                    Log.Warning("卸载正在加载中的包???");
                    continue;
                }

                if (_pkgToRef.TryGetValue(pkg, out var pr))
                {
                    if (!pr.Remove()) continue;
                    UIPackage.RemovePackage(pkg);
                    if (_pkgToHandles.TryGetValue(pkg, out var handles))
                    {
                        foreach (var handle in handles.Where(handle => handle.IsValid))
                        {
                            handle.Release();
                        }

                        _pkgToHandles.Remove(pkg);
                    }

                    _pkgToRef.Remove(pkg);
                    pr.Release();
                }
                else
                {
                    Log.Warning("卸载没有引用计数的包:" + pkg);
                }
            }

#if UNITY_EDITOR
            __Debugger_Event();
#endif
        }

        public async UniTask<bool> LoaUIdPackage(string[] pkgs)
        {
            //热更没完成前，直接加载资源，不需要引用计数判断
            if (!PatchMgr.Ins.IsDone)
            {
                foreach (var pkg in pkgs)
                {
                    if (!await _ToLoadUIPackage(pkg)) return false;
                }

                return true;
            }

            if (pkgs.Length == 0) return false;
            List<string> tem = null;
            foreach (var pkg in pkgs)
            {
                if (_pkgToRef.TryGetValue(pkg, out var pr))
                {
                    pr.Add();
                    continue;
                }

                tem ??= new List<string>();
                if (!tem.Contains(pkg)) tem.Add(pkg);
            }

            if (tem is not { Count: > 0 })
            {
#if UNITY_EDITOR
                __Debugger_Event();
#endif
                return true;
            }

            foreach (var pkg in tem)
            {
                if (_pkgToLoading.ContainsKey(pkg)) //已经在加载中了
                {
                    //循环等待，直到包加载完成
                    while (_pkgToLoading.ContainsKey(pkg))
                    {
                        await UniTask.Yield();
                    }

                    if (_pkgToRef.TryGetValue(pkg, out var pr))
                    {
                        pr.Add();
                    }
                    else
                    {
                        return false; //加载失败
                    }
                }
                else
                {
                    _pkgToLoading.Add(pkg, true);
                    if (!await _ToLoadUIPackage(pkg)) return false;
                }
            }

#if UNITY_EDITOR
            __Debugger_Event();
#endif
            return true;
        }

        private async UniTask<bool> _ToLoadUIPackage(string pkg)
        {
            zstring resName;
            using (zstring.Block())
            {
                resName = pkg;
                resName = zstring.Format("{0}_{1}_fui", resName, resName);
            }
#if UNITY_EDITOR
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
#endif
            var handle = GetPackage(ResType.UI).Package.LoadAssetAsync<TextAsset>(resName);
            await handle.ToUniTask();
#if UNITY_EDITOR
            sw.Stop();
            Log.Debug($"load {resName}:{sw.ElapsedMilliseconds}");
#endif
            var suc = true;
            if (handle.Status == EOperationStatus.Succeed && handle.AssetObject is TextAsset ta && ta != null)
            {
                UIPackage.AddPackage(ta.bytes, pkg, _LoadTextureFn);
            }
            else
            {
                Log.Error($"UI包加载错误PKG: {resName}");
                suc = false;
            }

            //热更没完成前，加载资源的语柄，都直接Release掉
            if (!PatchMgr.Ins.IsDone)
            {
                handle.Release();
                return suc;
            }

            if (!_pkgToHandles.TryGetValue(pkg, out var handles))
            {
                handles = new List<AssetHandle>();
                _pkgToHandles.Add(pkg, handles);
            }

            handles.Add(handle);
            if (suc)
            {
                var pr = Pool.Get<UIPkgRef>();
                pr.Init(pkg, 1);
                _pkgToRef.Add(pkg, pr);
            }

            _pkgToLoading.Remove(pkg);
            return suc;
        }

        private async void _LoadTextureFn(string name, string ex, Type type, PackageItem item)
        {
            if (type != typeof(Texture)) return;
            zstring resName;
            using (zstring.Block())
            {
                resName = zstring.Format("{0}_{1}", item.owner.name, name);
            }
#if UNITY_EDITOR
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
#endif
            var handle = GetPackage(ResType.UI).Package.LoadAssetAsync<Texture>(resName);
            await handle.ToUniTask();
#if UNITY_EDITOR
            sw.Stop();
            Log.Debug($"load {resName}:{sw.ElapsedMilliseconds}");
#endif

            if (handle.Status != EOperationStatus.Succeed)
            {
                Log.Error($"UI图集加载错误KEY: {resName}");
                handle.Release();
                return;
            }

            Texture texture = handle.AssetObject as Texture;
            item.owner.SetItemAsset(item, texture, DestroyMethod.None);

            //热更没完成前，加载资源的语柄，都直接Release掉
            if (!PatchMgr.Ins.IsDone)
            {
                handle.Release();
                return;
            }

            if (!_pkgToHandles.TryGetValue(item.owner.name, out var handles))
            {
                handles = new List<AssetHandle>();
                _pkgToHandles.Add(item.owner.name, handles);
            }

            handles.Add(handle);
        }

        #endregion

        #region 编辑器

#if UNITY_EDITOR
        public static void __Debugger_Event()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                __Debugger_CallBack?.Invoke(Ins._pkgToRef);
            }
        }

        public static Action<Dictionary<string, UIPkgRef>> __Debugger_CallBack;
#endif

        #endregion
    }
}