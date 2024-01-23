using System.Collections;
using YooAsset;
namespace Ux
{
    internal class PatchUpdateManifest : PatchStateNode
    {
        protected override void OnEnter(object args)
        {
            GameMain.Ins.StartCoroutine(UpdateManifest());
        }
        protected override void OnUpdate()
        {
        }
        protected override void OnExit()
        {
        }

        bool IsSucceed;
        private IEnumerator UpdateManifest()
        {
            IsSucceed = true;
            // 强制卸载所有资源
            YooMgr.Ins.ForceUnloadAllAssets();
            // 更新补丁清单
            yield return YooMgr.Ins.ForEachPackage(UpdateManifestAsync);
            if (IsSucceed)
            {
                PatchMgr.Enter<PatchCreateDownloader>();
            }
            else
            {
                PatchMgr.Ins.OnPatchManifestUpdateFailed();                
            }
        }

        IEnumerator UpdateManifestAsync(YooPackage package)
        {
            var operation = package.Package.UpdatePackageManifestAsync(package.Version);
            yield return operation;
            if (operation.Status != EOperationStatus.Succeed)
            {
                Log.Error(operation.Error);
                if (IsSucceed) IsSucceed = false;
            }
        }
    }
}