using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using YooAsset;
namespace Ux
{
    internal class PatchUpdateManifest : PatchStateNode
    {
        protected override void OnEnter(object args)
        {
            UpdateManifest().Forget();
        }
        protected override void OnUpdate()
        {
        }
        protected override void OnExit()
        {
        }

        bool IsSucceed;
        private async UniTaskVoid UpdateManifest()
        {
            IsSucceed = true;
            // 强制卸载所有资源
            YooMgr.Ins.ForceUnloadAllAssets();
            // 更新补丁清单
            await YooMgr.Ins.ForEachPackage(UpdateManifestAsync);
            if (IsSucceed)
            {
                PatchMgr.Enter<PatchCreateDownloader>();
            }
            else
            {
                OnPatchManifestUpdateFailed();
            }
        }

        void OnPatchManifestUpdateFailed()
        {
            Action callback = () =>
            {
                PatchMgr.Ins.Enter<PatchUpdateManifest>();
            };
            PatchMgr.View.ShowMessageBox($"获取补丁清单失败，请检测网络状态。", "确定", callback);
        }

        async UniTask UpdateManifestAsync(YooPackage package)
        {
            var operation = package.Package.UpdatePackageManifestAsync(package.Version);
            await operation;
            if (operation.Status != EOperationStatus.Succeed)
            {
                Log.Error(operation.Error);
                if (IsSucceed) IsSucceed = false;
            }
        }
    }
}