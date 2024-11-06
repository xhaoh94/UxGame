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
            var operation = package.Package.UpdatePackageManifestAsync(package.Version);
            await operation;
            if (operation.Status != EOperationStatus.Succeed)
            {
                Log.Error(operation.Error);
                return false;
            }
            return true;
        }
    }
}