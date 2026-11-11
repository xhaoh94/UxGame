using Cysharp.Threading.Tasks;
using YooAsset;

namespace Ux
{
    public partial class ResMgr
    {
        #region 同步

        /// <summary>
        /// 获取原生文件的二进制数据
        /// </summary>
        public byte[] GetRawFileData(string location)
        {
            var package = YooMgr.Ins.GetPackage(YooType.RawFile);
            if (package == null) return null;
            using (var handle = package.Package.LoadAssetSync<RawFileObject>(location))
            {
                return (handle.AssetObject as RawFileObject)?.GetBytes();
            }
        }

        /// <summary>
        /// 获取原生文件的文本数据
        /// </summary>
        public string GetRawFileText(string location)
        {
            var package = YooMgr.Ins.GetPackage(YooType.RawFile);
            if (package == null) return null;
            using (var handle = package.Package.LoadAssetSync<RawFileObject>(location))
            {
                return (handle.AssetObject as RawFileObject)?.GetText();
            }
        }

        /// <summary>
        /// 获取原生文件的二进制数据
        /// </summary>
        public byte[] GetRawFileData(AssetInfo assetInfo)
        {
            var package = YooMgr.Ins.GetPackage(YooType.RawFile);
            if (package == null) return null;
            using (var handle = package.Package.LoadAssetSync(assetInfo))
            {
                return (handle.AssetObject as RawFileObject)?.GetBytes();
            }
        }

        /// <summary>
        /// 获取原生文件的文本数据
        /// </summary>
        public string GetRawFileText(AssetInfo assetInfo)
        {
            var package = YooMgr.Ins.GetPackage(YooType.RawFile);
            if (package == null) return null;
            using (var handle = package.Package.LoadAssetSync(assetInfo))
            {
                return (handle.AssetObject as RawFileObject)?.GetText();
            }
        }

        #endregion

        #region 异步
        /// <summary>
        /// 异步获取原生文件的二进制数据
        /// </summary>
        public async UniTask<byte[]> GetRawFileDataAsync(string location)
        {
            var package = YooMgr.Ins.GetPackage(YooType.RawFile);
            if (package == null) return null;
            using (var handle = package.Package.LoadAssetAsync<RawFileObject>(location))
            {
                await handle.ToUniTask();
                return (handle.AssetObject as RawFileObject)?.GetBytes();
            }
        }

        /// <summary>
        /// 异步获取原生文件的文本数据
        /// </summary>
        public async UniTask<string> GetRawFileTextAsync(string location)
        {
            var package = YooMgr.Ins.GetPackage(YooType.RawFile);
            if (package == null) return null;
            using (var handle = package.Package.LoadAssetAsync<RawFileObject>(location))
            {
                await handle.ToUniTask();
                return (handle.AssetObject as RawFileObject)?.GetText();
            }
        }

        /// <summary>
        /// 异步获取原生文件的路径
        /// </summary>
        public async UniTask<string> GetRawFilePathAsync(string location)
        {
            var package = YooMgr.Ins.GetPackage(YooType.RawFile);
            if (package == null) return null;
            var operation = package.Package.EnsureBundleFileAsync(new EnsureBundleFileOptions(location));
            await operation.ToUniTask();
            return operation.Status == EOperationStatus.Succeeded ? operation.Detail.BundleFilePath : null;
        }

        /// <summary>
        /// 异步获取原生文件的二进制数据
        /// </summary>
        public async UniTask<byte[]> GetRawFileDataAsync(AssetInfo assetInfo)
        {
            var package = YooMgr.Ins.GetPackage(YooType.RawFile);
            if (package == null) return null;
            using (var handle = package.Package.LoadAssetAsync(assetInfo))
            {
                await handle.ToUniTask();
                return (handle.AssetObject as RawFileObject)?.GetBytes();
            }
        }

        /// <summary>
        /// 异步获取原生文件的文本数据
        /// </summary>
        public async UniTask<string> GetRawFileTextAsync(AssetInfo assetInfo)
        {
            var package = YooMgr.Ins.GetPackage(YooType.RawFile);
            if (package == null) return null;
            using (var handle = package.Package.LoadAssetAsync(assetInfo))
            {
                await handle.ToUniTask();
                return (handle.AssetObject as RawFileObject)?.GetText();
            }
        }

        /// <summary>
        /// 异步获取原生文件的路径
        /// </summary>
        public async UniTask<string> GetRawFilePathAsync(AssetInfo assetInfo)
        {
            var package = YooMgr.Ins.GetPackage(YooType.RawFile);
            if (package == null) return null;
            var operation = package.Package.EnsureBundleFileAsync(new EnsureBundleFileOptions(assetInfo));
            await operation.ToUniTask();
            return operation.Status == EOperationStatus.Succeeded ? operation.Detail.BundleFilePath : null;
        }
        #endregion

    }
}
