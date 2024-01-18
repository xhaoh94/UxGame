using System.Collections;
using YooAsset;
namespace Ux
{
    internal class PatchUpdateStaticVersion : PatchStateNode
    {
        protected override void OnEnter(object args = null)
        {
            GameMain.Ins.StartCoroutine(GetStaticVersion());
        }
        protected override void OnUpdate()
        {
        }
        protected override void OnExit()
        {
        }
        bool IsSucceed;
        private IEnumerator GetStaticVersion()
        {
            IsSucceed = true;
            // 更新资源版本号
            yield return ResMgr.Ins.ForEachPackage(UpdateStaticVersionAsync);
            if (IsSucceed)
            {
                PatchMgr.Enter<PatchUpdateManifest>();
            }
            else
            {
                PatchMgr.Ins.OnStaticVersionUpdateFailed();                
            }
        }

        IEnumerator UpdateStaticVersionAsync(ResPackage package)
        {
            var operation = package.Package.UpdatePackageVersionAsync();
            yield return operation;
            if (operation.Status == EOperationStatus.Succeed)
            {
                Log.Debug($"{Name}:资源版本 : {operation.PackageVersion}");
                package.Version = operation.PackageVersion;
            }
            else
            {
                Log.Warning(operation.Error);
                if (IsSucceed) IsSucceed = false;
            }
        }
    }
}