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
            using (var handle = package.Package.LoadRawFileSync(location))
            {
                return handle.GetRawFileData();
            }
        }

        /// <summary>
        /// 获取原生文件的文本数据
        /// </summary>
        public string GetRawFileText(string location)
        {
            var package = YooMgr.Ins.GetPackage(YooType.RawFile);
            if (package == null) return null;
            using (var handle = package.Package.LoadRawFileSync(location))
            {
                return handle.GetRawFileText();
            }
        }

        /// <summary>
        /// 获取原生文件的路径
        /// </summary>
        public string GetRawFilePath(string location)
        {
            var package = YooMgr.Ins.GetPackage(YooType.RawFile);
            if (package == null) return null;
            using (var handle = package.Package.LoadRawFileSync(location))
            {
                return handle.GetRawFilePath();
            }
        }

        /// <summary>
        /// 获取原生文件的二进制数据
        /// </summary>
        public byte[] GetRawFileData(AssetInfo assetInfo)
        {
            var package = YooMgr.Ins.GetPackage(YooType.RawFile);
            if (package == null) return null;
            using (var handle = package.Package.LoadRawFileSync(assetInfo))
            {
                return handle.GetRawFileData();
            }
        }

        /// <summary>
        /// 获取原生文件的文本数据
        /// </summary>
        public string GetRawFileText(AssetInfo assetInfo)
        {
            var package = YooMgr.Ins.GetPackage(YooType.RawFile);
            if (package == null) return null;
            using (var handle = package.Package.LoadRawFileSync(assetInfo))
            {
                return handle.GetRawFileText();
            }
        }

        /// <summary>
        /// 获取原生文件的路径
        /// </summary>
        public string GetRawFilePath(AssetInfo assetInfo)
        {
            var package = YooMgr.Ins.GetPackage(YooType.RawFile);
            if (package == null) return null;
            using (var handle = package.Package.LoadRawFileSync(assetInfo))
            {
                return handle.GetRawFilePath();
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
            using (var handle = package.Package.LoadRawFileAsync(location))
            {
                await handle.ToUniTask();
                return handle.GetRawFileData();
            }
        }

        /// <summary>
        /// 异步获取原生文件的文本数据
        /// </summary>
        public async UniTask<string> GetRawFileTextAsync(string location)
        {
            var package = YooMgr.Ins.GetPackage(YooType.RawFile);
            if (package == null) return null;
            using (var handle = package.Package.LoadRawFileAsync(location))
            {
                await handle.ToUniTask();
                return handle.GetRawFileText();
            }
        }

        /// <summary>
        /// 异步获取原生文件的路径
        /// </summary>
        public async UniTask<string> GetRawFilePathAsync(string location)
        {
            var package = YooMgr.Ins.GetPackage(YooType.RawFile);
            if (package == null) return null;
            using (var handle = package.Package.LoadRawFileAsync(location))
            {
                await handle.ToUniTask();
                return handle.GetRawFilePath();
            }
        }

        /// <summary>
        /// 异步获取原生文件的二进制数据
        /// </summary>
        public async UniTask<byte[]> GetRawFileDataAsync(AssetInfo assetInfo)
        {
            var package = YooMgr.Ins.GetPackage(YooType.RawFile);
            if (package == null) return null;
            using (var handle = package.Package.LoadRawFileAsync(assetInfo))
            {
                await handle.ToUniTask();
                return handle.GetRawFileData();
            }
        }

        /// <summary>
        /// 异步获取原生文件的文本数据
        /// </summary>
        public async UniTask<string> GetRawFileTextAsync(AssetInfo assetInfo)
        {
            var package = YooMgr.Ins.GetPackage(YooType.RawFile);
            if (package == null) return null;
            using (var handle = package.Package.LoadRawFileAsync(assetInfo))
            {
                await handle.ToUniTask();
                return handle.GetRawFileText();
            }
        }

        /// <summary>
        /// 异步获取原生文件的路径
        /// </summary>
        public async UniTask<string> GetRawFilePathAsync(AssetInfo assetInfo)
        {
            var package = YooMgr.Ins.GetPackage(YooType.RawFile);
            if (package == null) return null;
            using (var handle = package.Package.LoadRawFileAsync(assetInfo))
            {
                await handle.ToUniTask();
                return handle.GetRawFilePath();
            }
        }
        #endregion        

    }
}
