using Cysharp.Threading.Tasks;
using YooAsset;

namespace Ux
{
    public partial class ResMgr : Singleton<ResMgr>
    {
        #region 同步
        /// <summary>
        /// 同步加载资源对象
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        public UnityEngine.Object LoadAsset(AssetInfo assetInfo, YooType resType = YooType.None)
        {
            return LoadAsset<UnityEngine.Object>(assetInfo, resType);
        }

        /// <summary>
        /// 同步加载资源对象
        /// </summary>
        /// <param name="location">资源的定位地址</param>
        public UnityEngine.Object LoadAsset(string location, YooType resType = YooType.None)
        {
            return LoadAsset<UnityEngine.Object>(location, resType);
        }

        /// <summary>
        /// 同步加载资源对象
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        public TObject LoadAsset<TObject>(AssetInfo assetInfo, YooType resType = YooType.None) where TObject : UnityEngine.Object
        {
            var obj = UnityPool.Get<TObject>(assetInfo.Address);
            if (obj != null)
            {
                return obj;
            }
            var package = resType == YooType.None
                ? YooMgr.Ins.GetPackageByLocation(assetInfo.Address)
                : YooMgr.Ins.GetPackage(resType);
            if (package == null) return default(TObject);
            var handle = package.Package.LoadAssetSync(assetInfo);
            return _LoadAsset<TObject>(assetInfo.Address, handle);
        }

        /// <summary>
        /// 同步加载资源对象
        /// </summary>
        /// <typeparam name="TObject">资源类型</typeparam>
        /// <param name="location">资源的定位地址</param>
        public TObject LoadAsset<TObject>(string location, YooType resType = YooType.None) where TObject : UnityEngine.Object
        {
            var obj = UnityPool.Get<TObject>(location);
            if (obj != null)
            {
                return obj;
            }
            var package = resType == YooType.None
                ? YooMgr.Ins.GetPackageByLocation(location)
                : YooMgr.Ins.GetPackage(resType);
            if (package == null) return default(TObject);
            var handle = package.Package.LoadAssetSync<TObject>(location);
            return _LoadAsset<TObject>(location, handle);
        }

        TObject _LoadAsset<TObject>(string location, AssetHandle handle) where TObject : UnityEngine.Object
        {
            var obj = handle.GetAssetObject<TObject>();
            if (obj is UnityEngine.GameObject)
            {
                var ins = handle.InstantiateSync();
                ins.AddComponent<ResHandleMono>().Init(location, handle);
                return ins as TObject;
            }

            using (handle)
            {
                return obj;
            }
        }


        #endregion

        #region 异步
        /// <summary>
        /// 异步加载资源对象
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        public async UniTask<UnityEngine.Object> LoadAssetAsync(AssetInfo assetInfo, YooType resType = YooType.None)
        {
            return await LoadAssetAsync<UnityEngine.Object>(assetInfo, resType);
        }

        /// <summary>
        /// 异步加载资源对象
        /// </summary>
        /// <param name="location">资源的定位地址</param>
        public async UniTask<UnityEngine.Object> LoadAssetAsync(string location, YooType resType = YooType.None)
        {
            return await LoadAssetAsync<UnityEngine.Object>(location, resType);
        }


        /// <summary>
        /// 异步加载资源对象
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        public async UniTask<TObject> LoadAssetAsync<TObject>(AssetInfo assetInfo, YooType resType = YooType.None) where TObject : UnityEngine.Object
        {
            var obj = UnityPool.Get<TObject>(assetInfo.Address);
            if (obj != null)
            {
                return obj;
            }
            var package = resType == YooType.None
                ? YooMgr.Ins.GetPackageByLocation(assetInfo.Address)
                : YooMgr.Ins.GetPackage(resType);
            if (package == null) return default(TObject);
            var handle = package.Package.LoadAssetAsync(assetInfo);
            return await _LoadAssetAsync<TObject>(assetInfo.Address, handle);
        }

        /// <summary>
		/// 异步加载资源对象
		/// </summary>
		/// <typeparam name="TObject">资源类型</typeparam>
		/// <param name="location">资源的定位地址</param>
		public async UniTask<TObject> LoadAssetAsync<TObject>(string location, YooType resType = YooType.None) where TObject : UnityEngine.Object
        {
            var obj = UnityPool.Get<TObject>(location);
            if (obj != null)
            {
                return obj;
            }
            var package = resType == YooType.None
                ? YooMgr.Ins.GetPackageByLocation(location)
                : YooMgr.Ins.GetPackage(resType);
            if (package == null) return default(TObject);
            var handle = package.Package.LoadAssetAsync<TObject>(location);
            return await _LoadAssetAsync<TObject>(location, handle);
        }

        async UniTask<TObject> _LoadAssetAsync<TObject>(string location, AssetHandle handle) where TObject : UnityEngine.Object
        {
            await handle.ToUniTask();
            var obj = handle.GetAssetObject<TObject>();
            if (obj is UnityEngine.GameObject)
            {
                var insHandle = handle.InstantiateAsync();
                await insHandle.ToUniTask();
                var ins = insHandle.Result;
                ins.AddComponent<ResHandleMono>().Init(location, handle);
                return ins as TObject;
            }

            using (handle)
            {
                return obj;
            }
        }


        #endregion
    }
}
