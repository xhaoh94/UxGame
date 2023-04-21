using System;
using System.Collections.Generic;
using YooAsset;
using System.Collections;
using UnityEngine.SceneManagement;

namespace Ux
{
    public partial class ResMgr : Singleton<ResMgr>
    {
        public static readonly ResLazyload Lazyload = new ResLazyload();

        static readonly Dictionary<ResType, ResPackage> _Packages = new Dictionary<ResType, ResPackage>() {
            { ResType.Main,new ResPackage(ResType.Main,"MainPackage") },
            { ResType.UI,new ResPackage(ResType.UI,"UIPackage") }
        };

        readonly Dictionary<string, ResPackage> _locationToPackage = new Dictionary<string, ResPackage>();
        public IEnumerator Initialize()
        {
            // 初始化资源系统
            YooAssets.Initialize();
            // 创建资源包            
            ForEachPackage(x => x.CreatePackage());
            // 初始化资源包
            yield return ForEachPackage(x => x.Initialize());

        }
        public void ForEachPackage(Action<ResPackage> fn)
        {
            _Packages.ForEachValue(fn);
        }
        public void ForEachPackage(Func<ResPackage, bool> fn)
        {
            _Packages.ForEachValue(fn);
        }
        public IEnumerator ForEachPackage(Func<ResPackage, IEnumerator> fn)
        {
            yield return _Packages.ForEachValue(fn);
        }
        public ResPackage GetPackage(ResType resType)
        {
            if (_Packages.TryGetValue(resType, out var result))
            {
                return result;
            }
            throw new Exception($"GetAssetsPackage:AssetsType[{resType}]资源包不存在");
        }

        public ResPackage GetPackageByLocation(string location)
        {
            if (!_locationToPackage.ContainsKey(location))
            {
                ForEachPackage(x =>
                {
                    var valid = x.Package.CheckLocationValid(location);
                    if (!valid) return false;
                    _locationToPackage.Add(location, x);
                    return true;
                });
            }
            try
            {
                return _locationToPackage[location];
            }
            catch
            {
                throw (new Exception("资源找不到Package"));
            }
        }
        
        #region 原生文件
        /// <summary>
        /// 同步加载原生文件
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        public RawFileOperationHandle LoadRawFileSync(AssetInfo assetInfo)
        {
            var package = GetPackageByLocation(assetInfo.Address);
            return package.Package.LoadRawFileSync(assetInfo);
        }

        /// <summary>
        /// 同步加载原生文件
        /// </summary>
        /// <param name="location">资源的定位地址</param>
        public RawFileOperationHandle LoadRawFileSync(string location)
        {
            var package = GetPackageByLocation(location);
            return package.Package.LoadRawFileSync(location);
        }

        /// <summary>
        /// 异步加载原生文件
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        public RawFileOperationHandle LoadRawFileAsync(AssetInfo assetInfo)
        {
            var package = GetPackageByLocation(assetInfo.Address);
            return package.Package.LoadRawFileAsync(assetInfo);
        }

        /// <summary>
        /// 异步加载原生文件
        /// </summary>
        /// <param name="location">资源的定位地址</param>
        public RawFileOperationHandle LoadRawFileAsync(string location)
        {
            var package = GetPackageByLocation(location);
            return package.Package.LoadRawFileAsync(location);
        }


       
        #endregion
        
        #region 资源加载
        /// <summary>
		/// 异步加载场景
		/// </summary>
		/// <param name="location">场景的定位地址</param>
		/// <param name="sceneMode">场景加载模式</param>
		/// <param name="activateOnLoad">加载完毕时是否主动激活</param>
		/// <param name="priority">优先级</param>
        public SceneOperationHandle LoadSceneAsync(string location, LoadSceneMode sceneMode = LoadSceneMode.Single, bool activateOnLoad = true, int priority = 100)
        {
            var package = GetPackageByLocation(location);
            return package.Package.LoadSceneAsync(location, sceneMode, activateOnLoad, priority);
        }
        /// <summary>
        /// 同步加载资源对象
        /// </summary>
        /// <typeparam name="TObject">资源类型</typeparam>
        /// <param name="location">资源的定位地址</param>
        public AssetOperationHandle LoadAssetSync<TObject>(string location) where TObject : UnityEngine.Object
        {
            return LoadAssetSync(location, typeof(TObject));
        }

        /// <summary>
        /// 同步加载资源对象
        /// </summary>
        /// <param name="location">资源的定位地址</param>
        /// <param name="type">资源类型</param>
        public AssetOperationHandle LoadAssetSync(string location, Type type)
        {
            var package = GetPackageByLocation(location);
            return package.Package.LoadAssetSync(location, type);
        }

        /// <summary>
		/// 异步加载资源对象
		/// </summary>
		/// <typeparam name="TObject">资源类型</typeparam>
		/// <param name="location">资源的定位地址</param>
		public AssetOperationHandle LoadAssetAsync<TObject>(string location) where TObject : UnityEngine.Object
        {
            return LoadAssetAsync(location, typeof(TObject));
        }

        /// <summary>
        /// 异步加载资源对象
        /// </summary>
        /// <param name="location">资源的定位地址</param>
        /// <param name="type">资源类型</param>
        public AssetOperationHandle LoadAssetAsync(string location, Type type)
        {
            var package = GetPackageByLocation(location);
            return package.Package.LoadAssetAsync(location, type);
        }

        #endregion

        #region 资源卸载
        public void ForceUnloadAllAssets()
        {
            ForEachPackage(x => x.Package.ForceUnloadAllAssets());
        }
        public void UnloadUnusedAssets()
        {
            ForEachPackage(x => x.Package.UnloadUnusedAssets());
        }
        #endregion
        public void OnLowMemory()
        {
            UnloadUnusedAssets();
        }
    }
}