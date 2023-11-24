using System;
using System.Collections;
using System.Collections.Generic;
using YooAsset;

namespace Ux
{
    public partial class ResMgr : Singleton<ResMgr>
    {
        public static readonly ResLazyload Lazyload = new ResLazyload();

        static readonly Dictionary<ResType, ResPackage> _Packages = new Dictionary<ResType, ResPackage>()
        {
            { ResType.Main,new ResMainPackage() },
            { ResType.UI,new ResUIPackage() },
            { ResType.RawFile,new RawFilePackage() }
        };

        readonly Dictionary<string, ResPackage> _locationToPackage = new Dictionary<string, ResPackage>();

        public IEnumerator Initialize(EPlayMode playMode)
        {
            // 初始化资源系统
            YooAssets.Initialize();
            // 创建资源包            
            ForEachPackage(x => x.CreatePackage());
            // 初始化资源包
            yield return ForEachPackage(x => x.Initialize(playMode));
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
                throw (new Exception($"资源找不到Package:{location}"));
            }
        }

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