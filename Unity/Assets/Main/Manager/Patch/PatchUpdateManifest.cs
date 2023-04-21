using System.Collections;
using YooAsset;
using EventType = Ux.Main.EventType;
namespace Ux
{
    internal class PatchUpdateManifest : PatchStateNode
    {
        protected override void OnEnter(object args)
        {
            GameMain.Instance.StartCoroutine(UpdateManifest());
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
            ResMgr.Instance.ForceUnloadAllAssets();
            // 更新补丁清单
            yield return ResMgr.Instance.ForEachPackage(UpdateManifestAsync);
            if (IsSucceed)
            {
                PatchMgr.Enter<PatchCreateDownloader>();
            }
            else
            {
                EventMgr.Instance.Send(EventType.PATCH_MANIFEST_UPDATE_FAILED);
            }
        }

        IEnumerator UpdateManifestAsync(ResPackage package)
        {
            var operation = package.Package.UpdatePackageManifestAsync(package.Version);
            yield return operation;
            if (operation.Status != EOperationStatus.Succeed)
            {
                Log.Warning(operation.Error);
                if (IsSucceed) IsSucceed = false;
            }
        }
    }
}