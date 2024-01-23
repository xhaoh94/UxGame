using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using YooAsset;

namespace Ux
{
    public partial class YooMgr : Singleton<YooMgr>
    {
        static readonly Dictionary<YooType, YooPackage> _Packages = new Dictionary<YooType, YooPackage>()
        {
            { YooType.Main,new YooMainPackage() },
            { YooType.Code,new YooCodePackage() },
            { YooType.UI,new YooUIPackage() },
            { YooType.Config,new YooConfigPackage() },
            { YooType.RawFile,new YooRawFilePackage() },
        };

        readonly Dictionary<string, YooPackage> _locationToPackage = new Dictionary<string, YooPackage>();

        public async UniTask Initialize(EPlayMode playMode)
        {
            // 初始化资源系统
            YooAssets.Initialize();
            // 创建资源包            
            ForEachPackage(x => x.CreatePackage());
            // 初始化资源包
            await ForEachPackage(x => x.Initialize(playMode));
        }
        public void ForEachPackage(Action<YooPackage> fn)
        {
            _Packages.ForEachValue(fn);
        }
        public void ForEachPackage(Func<YooPackage, bool> fn)
        {
            _Packages.ForEachValue(fn);
        }
        public IEnumerator ForEachPackage(Func<YooPackage, IEnumerator> fn)
        {
            yield return _Packages.ForEachValue(fn);
        }
        public async UniTask ForEachPackage(Func<YooPackage, UniTask> fn)
        {
            await _Packages.ForEachValue(fn);
        }
        public YooPackage GetPackage(YooType resType)
        {
            if (_Packages.TryGetValue(resType, out var result))
            {
                return result;
            }
            throw new Exception($"GetAssetsPackage:AssetsType[{resType}]资源包不存在");
        }

        public YooPackage GetPackageByLocation(string location)
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