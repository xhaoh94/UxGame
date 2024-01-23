using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using YooAsset;
namespace Ux
{
    internal class PatchUpdateStaticVersion : PatchStateNode
    {
        protected override void OnEnter(object args = null)
        {
            GetStaticVersion().Forget();
        }
        protected override void OnUpdate()
        {
        }
        protected override void OnExit()
        {
        }
        bool IsSucceed;
        async UniTaskVoid GetStaticVersion()
        {
            IsSucceed = true;
            // 更新资源版本号
            await YooMgr.Ins.ForEachPackage(UpdateStaticVersionAsync);
            if (IsSucceed)
            {
                PatchMgr.Enter<PatchUpdateManifest>();
            }
            else
            {
                OnStaticVersionUpdateFailed();
            }
        }

        void OnStaticVersionUpdateFailed()
        {
            Action callback = () =>
            {
                PatchMgr.Ins.Enter<PatchUpdateStaticVersion>();
            };
            PatchMgr.View.ShowMessageBox($"获取资源版本失败，请检测网络状态。", "确定", callback);
        }

        async UniTask UpdateStaticVersionAsync(YooPackage package)
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
                if (IsSucceed) IsSucceed = false;
            }
        }
    }
}