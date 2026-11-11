using Cysharp.Threading.Tasks;
using UnityEngine;
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
            if (RejectGameObjectType<TObject>(assetInfo.Address)) return default(TObject);

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
            if (RejectGameObjectType<TObject>(location)) return default(TObject);

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

        /// <summary>
        /// GameObject 必须走 <see cref="LoadGameObject"/>：那边才会实例化 + 挂父级。
        /// 用 LoadAsset 拿预制体会得到"没有父级的裸实例"，语义对不上，直接拦掉。
        /// </summary>
        private static bool RejectGameObjectType<TObject>(string location) where TObject : UnityEngine.Object
        {
            if (!typeof(GameObject).IsAssignableFrom(typeof(TObject)))
                return false;

            Log.Error("加载 GameObject 请用 ResMgr.LoadGameObject / LoadGameObjectAsync：{0}", location);
            return true;
        }

        TObject _LoadAsset<TObject>(string location, AssetHandle handle) where TObject : UnityEngine.Object
        {
            var obj = handle.GetAssetObject<TObject>();
            if (obj is UnityEngine.GameObject)
            {
                // 泛型是 Object/Component 却取到预制体：调用方走错了入口。
                if (typeof(TObject) != typeof(GameObject))
                {
                    Log.Error("加载 GameObject 请用 ResMgr.LoadGameObject / LoadGameObjectAsync：{0}", location);
                    handle.Release();
                    return default(TObject);
                }

                var ins = handle.InstantiateSync();
                ins.AddComponent<ResHandleMono>().Init(location, handle);
                return ins as TObject;
            }

            using (handle)
            {
                return obj;
            }
        }


        /// <summary>
        /// 同步加载并交出句柄，调用方负责 Release。
        /// LoadAsset 对非 GameObject 是"用完即弃"语义（using 立刻释放），要长期持有的资源走这里。
        /// </summary>
        public AssetHandle LoadAssetHandle(string location, YooType resType = YooType.None)
        {
            var package = resType == YooType.None
                ? YooMgr.Ins.GetPackageByLocation(location)
                : YooMgr.Ins.GetPackage(resType);
            return package != null ? package.Package.LoadAssetSync(location) : null;
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
            if (RejectGameObjectType<TObject>(assetInfo.Address)) return default(TObject);

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
            if (RejectGameObjectType<TObject>(location)) return default(TObject);

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
                // 泛型是 Object/Component 却取到预制体：调用方走错了入口。
                if (typeof(TObject) != typeof(GameObject))
                {
                    Log.Error("加载 GameObject 请用 ResMgr.LoadGameObject / LoadGameObjectAsync：{0}", location);
                    handle.Release();
                    return default(TObject);
                }

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


        /// <summary>异步加载并交出句柄，加载完成后返回，调用方负责 Release。</summary>
        public async UniTask<AssetHandle> LoadAssetHandleAsync(string location, YooType resType = YooType.None)
        {
            var package = resType == YooType.None
                ? YooMgr.Ins.GetPackageByLocation(location)
                : YooMgr.Ins.GetPackage(resType);
            if (package == null) return null;

            var handle = package.Package.LoadAssetAsync(location);
            try
            {
                await handle.ToUniTask();
            }
            catch
            {
                // 加载失败也要还回去，否则这个句柄会一直占着包引用。
                handle.Release();
                throw;
            }

            return handle;
        }

        #endregion

        #region GameObject

        /// <summary>
        /// 同步加载 GameObject 实例并挂到指定父级。
        /// 文本 / 贴图 / 音频这类非实例化资源请继续用 <see cref="LoadAsset"/>。
        /// parent 传了就一步挂好，省掉"先拿到再 SetParent"的第二次层级操作；
        /// 传 null 时按对象池的默认放置规则（回到当前场景根）。
        /// </summary>
        public GameObject LoadGameObject(string location, Transform parent = null, YooType resType = YooType.None)
        {
            var pooled = UnityPool.Get<GameObject>(location, parent);
            if (pooled != null)
            {
                return pooled;
            }

            var package = resType == YooType.None
                ? YooMgr.Ins.GetPackageByLocation(location)
                : YooMgr.Ins.GetPackage(resType);
            if (package == null)
            {
                return null;
            }

            var handle = package.Package.LoadAssetSync<GameObject>(location);
            var go = _LoadAsset<GameObject>(location, handle);
            if (go != null && parent != null)
            {
                go.transform.SetParent(parent, false);
            }
            return go;
        }

        /// <summary>异步版 <see cref="LoadGameObject"/>。</summary>
        public async UniTask<GameObject> LoadGameObjectAsync(string location, Transform parent = null, YooType resType = YooType.None)
        {
            var pooled = UnityPool.Get<GameObject>(location, parent);
            if (pooled != null)
            {
                return pooled;
            }

            var package = resType == YooType.None
                ? YooMgr.Ins.GetPackageByLocation(location)
                : YooMgr.Ins.GetPackage(resType);
            if (package == null)
            {
                return null;
            }

            var handle = package.Package.LoadAssetAsync<GameObject>(location);
            var go = await _LoadAssetAsync<GameObject>(location, handle);
            if (go != null && parent != null)
            {
                go.transform.SetParent(parent, false);
            }
            return go;
        }

        #endregion
    }
}
