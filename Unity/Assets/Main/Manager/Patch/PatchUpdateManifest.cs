using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using YooAsset;
namespace Ux
{
    internal class PatchUpdateManifest : PatchStateNode
    {
        protected override void OnEnter()
        {
            base.OnEnter();
            UpdateManifest().Forget();
        }
        
        private async UniTaskVoid UpdateManifest()
        {            
            // 强制卸载所有资源
            //YooMgr.Ins.UnloadAllAssetsAsync();
            // 更新补丁清单
            var succeed = await YooMgr.Ins.ForEachPackage(UpdateManifestAsync);            
            if (succeed)
            {
                // 新清单的地址集合可能变了，之前缓存的「地址→包裹」全部作废
                YooMgr.Ins.ClearLocationCache();
                PatchMgr.Enter<PatchCreateDownloader>();
            }
            else
            {
                Action callback = () =>
                {
                    PatchMgr.Ins.Enter<PatchUpdateManifest>();
                };
                PatchMgr.View.ShowMessageBox($"获取补丁清单失败，请检测网络状态。", "确定", callback);
            }
        }

        async UniTask<bool> UpdateManifestAsync(YooPackage package)
        {
            var operation = package.Package.LoadPackageManifestAsync(new LoadPackageManifestOptions(package.Version, 60));
            await operation;
            if (operation.Status != EOperationStatus.Succeeded)
            {
                Log.Error(operation.Error);
                return false;
            }
            return true;
        }
    }
}