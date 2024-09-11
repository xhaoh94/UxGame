using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using YooAsset;
namespace Ux
{
    internal class PatchUpdateStaticVersion : PatchStateNode
    {
        protected override void OnEnter()
        {
            base.OnEnter();
            GetStaticVersion().Forget();
        }
        async UniTaskVoid GetStaticVersion()
        {
            // 更新资源版本号
            var succeed = await YooMgr.Ins.ForEachPackage(UpdateStaticVersionAsync);            
            if (succeed)
            {
                PatchMgr.Enter<PatchUpdateManifest>();
            }
            else
            {
                Action callback = () =>
                {
                    PatchMgr.Ins.Enter<PatchUpdateStaticVersion>();
                };
                PatchMgr.View.ShowMessageBox($"获取资源版本失败，请检测网络状态。", "确定", callback);
            }
        }


        async UniTask<bool> UpdateStaticVersionAsync(YooPackage package)
        {
            var operation = package.Package.UpdatePackageVersionAsync();
            await operation;
            if (operation.Status == EOperationStatus.Succeed)
            {
                Log.Debug($"{package.Name}:资源版本 : {operation.PackageVersion}");
                package.Version = operation.PackageVersion;
            }
            else
            {
                Log.Warning(operation.Error);
                return false;
            }
            return true;
        }
    }
}