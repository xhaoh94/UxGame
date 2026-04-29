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

    public class UIResourceHandler
    {
        private readonly YooPackage _yoo;
        private readonly Dictionary<string, UIPkgRef> _pkgToRef = new();
        private readonly Dictionary<string, List<AssetHandle>> _pkgToHandles = new();
        private readonly HashSet<string> _pkgToLoading = new();

        public Dictionary<string, UIPkgRef> PkgToRef => _pkgToRef;

        public UIResourceHandler()
        {
            _yoo = YooMgr.Ins.GetPackage(YooType.Main);
        }

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
                    CleanupPackageResources(pkg);
                    _pkgToRef.Remove(pkg);
                    pr.Release();
                }
                else
                {
                    Log.Error("卸载没有引用计数的包:" + pkg);
                }
            }
        }

        private void RollbackAllRefCounts(List<UIPkgRef> refsToRollback)
        {
            if (refsToRollback == null) return;

            foreach (var pr in refsToRollback)
            {
                if (pr.Remove())
                {
                    CleanupPackageResources(pr.PkgName);
                    _pkgToRef.Remove(pr.PkgName);
                    pr.Release();
                }
            }
        }

        private void CleanupPackageResources(string pkgName)
        {
            UIPackage.RemovePackage(pkgName);
            if (_pkgToHandles.TryGetValue(pkgName, out var handles))
            {                
                for (int i = 0; i < handles.Count; i++)
                {
                    var handle = handles[i];
                    if (handle.IsValid)
                    {
                        handle.Release();
                    }
                }
                _pkgToHandles.Remove(pkgName);
            }
        }

        public async UniTask<bool> LoadUIPackage(string[] pkgs)
        {
            if (pkgs.Length == 0) return false;

            // 用于记录所有需要加载的新包
            List<string> packagesToLoad = null;
            // 用于记录所有需要回滚的 UIPkgRef（包括已存在的包和新加载的包）
            List<UIPkgRef> refsToRollback = null;

            // 第一阶段：准备阶段
            foreach (var pkg in pkgs)
            {
                if (_pkgToRef.TryGetValue(pkg, out var pr))
                {
                    // 已存在的包：增加引用计数并记录以便回滚
                    refsToRollback ??= new List<UIPkgRef>();
                    refsToRollback.Add(pr);
                    pr.Add();
                    continue;
                }

                packagesToLoad ??= new List<string>();
                if (!packagesToLoad.Contains(pkg)) packagesToLoad.Add(pkg);
            }

            if (packagesToLoad is not { Count: > 0 })
            {
                // 没有新包需要加载，直接返回成功
                return true;
            }

            foreach (var pkg in packagesToLoad)
            {
                if (_pkgToLoading.Contains(pkg))
                {
                    // 等待其他线程加载完成
                    while (_pkgToLoading.Contains(pkg))
                    {
                        await UniTask.Yield();
                    }

                    if (_pkgToRef.TryGetValue(pkg, out var pr))
                    {
                        // 其他线程加载成功，增加引用计数并记录以便回滚
                        refsToRollback ??= new List<UIPkgRef>();
                        refsToRollback.Add(pr);
                        pr.Add();
                    }
                    else
                    {
                        // 其他线程加载失败，回滚所有引用计数
                        RollbackAllRefCounts(refsToRollback);
                        return false;
                    }
                }
                else
                {
                    _pkgToLoading.Add(pkg);
                    bool loadSuccess = await _ToLoadUIPackage(pkg);
                    _pkgToLoading.Remove(pkg);

                    if (loadSuccess)
                    {
                        var pr = Pool.Get<UIPkgRef>();
                        pr.Init(pkg, 1);
                        _pkgToRef.Add(pkg, pr);
                        refsToRollback ??= new List<UIPkgRef>();
                        refsToRollback.Add(pr);
                    }
                    else
                    {
                        // 加载失败，回滚所有引用计数
                        RollbackAllRefCounts(refsToRollback);
                        return false;
                    }
                }
            }

            return true;
        }

        private async UniTask<bool> _ToLoadUIPackage(string pkg)
        {
            string resName = string.Format(PathHelper.Res.UI, pkg, "fui");

            var handle = _yoo.Package.LoadAssetAsync<TextAsset>(resName);
            await handle.ToUniTask();
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
            return suc;
        }

        private async void _LoadTextureFn(string name, string ex, Type type, PackageItem item)
        {
            if (type != typeof(Texture)) return;
            string resName = string.Format(PathHelper.Res.UIAtlas, name);
            var handle = _yoo.Package.LoadAssetAsync<Texture>(resName);
            await handle.ToUniTask();

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