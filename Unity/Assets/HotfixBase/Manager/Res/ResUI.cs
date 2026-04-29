using Cysharp.Threading.Tasks;
using FairyGUI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using YooAsset;

namespace Ux
{
    public interface IResUIDebuggerAccess
    {
        Dictionary<string, UIPkgRef> GetPkgRefs();
    }

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

    public partial class ResMgr : IResUIDebuggerAccess
    {
        private YooPackage _yoo;
        private YooPackage Yoo
        {
            get
            {
                if (_yoo == null)
                {
                    _yoo = YooMgr.Ins.GetPackage(YooType.Main);
                }
                return _yoo;
            }
        }
        private readonly Dictionary<string, List<AssetHandle>> _pkgToHandles = new();
        private readonly Dictionary<string, AutoResetUniTaskCompletionSource<bool>> _pkgLoadingTasks = new();

        private readonly Dictionary<string, UIPkgRef> _pkgToRef = new();

        Dictionary<string, UIPkgRef> IResUIDebuggerAccess.GetPkgRefs()
        {
            return _pkgToRef;
        }
        public void RemoveUIPackage(string[] pkgs)
        {
            if (pkgs.Length == 0) return;
            foreach (var pkg in pkgs)
            {
                // 使用 _pkgLoadingTasks 判断是否正在加载
                if (_pkgLoadingTasks.ContainsKey(pkg))
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
                // 使用 _pkgLoadingTasks 判断是否正在加载
                if (_pkgLoadingTasks.TryGetValue(pkg, out var existingTcs))
                {
                    // 等待其他协程加载完成
                    var success = await existingTcs.Task;
                    if (success && _pkgToRef.TryGetValue(pkg, out var pr))
                    {
                        refsToRollback ??= new List<UIPkgRef>();
                        refsToRollback.Add(pr);
                        pr.Add();
                    }
                    else
                    {
                        // 另一个协程加载失败
                        RollbackAllRefCounts(refsToRollback);
                        return false;
                    }
                }
                else
                {
                    // 开始加载，创建 AutoResetUniTaskCompletionSource 用于其他协程等待
                    var loadTcs = AutoResetUniTaskCompletionSource<bool>.Create();
                    _pkgLoadingTasks[pkg] = loadTcs;

                    bool loadSuccess = await _ToLoadUIPackage(pkg);

                    if (loadSuccess)
                    {
                        var pr = Pool.Get<UIPkgRef>();
                        pr.Init(pkg, 1);
                        _pkgToRef.Add(pkg, pr);
                        refsToRollback ??= new List<UIPkgRef>();
                        refsToRollback.Add(pr);
                    }

                    // 先通知等待的协程，再清理状态
                    loadTcs.TrySetResult(loadSuccess);
                    _pkgLoadingTasks.Remove(pkg);

                    if (!loadSuccess)
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

            var handle = Yoo.Package.LoadAssetAsync<TextAsset>(resName);
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
            var handle = Yoo.Package.LoadAssetAsync<Texture>(resName);
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