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

    public partial class UIMgr
    {
        private readonly YooPackage _yoo = YooMgr.Ins.GetPackage(YooType.Main);
        private readonly Dictionary<string, UIPkgRef> _pkgToRef = new Dictionary<string, UIPkgRef>();

        private readonly Dictionary<string, List<AssetHandle>> _pkgToHandles =
            new Dictionary<string, List<AssetHandle>>();

        private readonly HashSet<string> _pkgToLoading = new HashSet<string>();

        public void RemoveUIPackage(string[] pkgs)
        {

            if (pkgs.Length == 0) return;
            foreach (var pkg in pkgs)
            {
                if (_pkgToLoading.Contains(pkg))
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
            __Debugger_Pkg_Event();
#endif
        }

        public async UniTask<bool> LoaUIdPackage(string[] pkgs)
        {
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
                __Debugger_Pkg_Event();
#endif
                return true;
            }

            foreach (var pkg in tem)
            {
                if (_pkgToLoading.Contains(pkg)) //已经在加载中了
                {
                    //循环等待，直到包加载完成
                    while (_pkgToLoading.Contains(pkg))
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
                    _pkgToLoading.Add(pkg);
                    if (!await _ToLoadUIPackage(pkg)) return false;
                }
            }

#if UNITY_EDITOR
            __Debugger_Pkg_Event();
#endif
            return true;
        }

        private async UniTask<bool> _ToLoadUIPackage(string pkg)
        {            
            
            string resName = string.Format(PathHelper.Res.UI,pkg,"fui");
#if UNITY_EDITOR
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
#endif

            var handle = _yoo.Package.LoadAssetAsync<TextAsset>(resName);
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
            //string resName = $"{PathHelper.Res.UI}/{item.owner.name}/{name}";
            string resName = string.Format(PathHelper.Res.UI, item.owner.name, name);
#if UNITY_EDITOR
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
#endif
            var handle = _yoo.Package.LoadAssetAsync<Texture>(resName);
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

            if (!_pkgToHandles.TryGetValue(item.owner.name, out var handles))
            {
                handles = new List<AssetHandle>();
                _pkgToHandles.Add(item.owner.name, handles);
            }

            handles.Add(handle);
        }

    }
}