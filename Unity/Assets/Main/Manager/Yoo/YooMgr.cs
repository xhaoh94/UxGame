using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using YooAsset;

namespace Ux
{
    public partial class YooMgr : Singleton<YooMgr>
    {
        public enum EncyptionType
        {
            None,
            Offset,
            XOR,
            XXTEA
        }

        static readonly Dictionary<YooType, YooPackage> _Packages = new Dictionary<YooType, YooPackage>()
        {
            { YooType.Main,new YooMainPackage() },
            { YooType.RawFile,new YooRawFilePackage() },
        };

        readonly Dictionary<string, YooPackage> _locationToPackage = new Dictionary<string, YooPackage>();

        protected override void OnCreated()
        {
            GameMain.Ins.LowMemory += UnloadUnusedAssetsAsync;
        }
        public async UniTask<bool> Initialize()
        {
            if (YooAssets.IsInitialized)
            {
                Log.Error($"{nameof(YooAssets)} is initialized !");
                return false;
            }
            // 初始化资源系统
            YooAssets.Initialize();
            // 初始化资源包
            return await InitializePackage();
        }
        async UniTask<bool> InitializePackage()
        {
            foreach (var _value in _Packages.Values)
            {
                var b = await (_value as IYooPackage).Initialize(GameMain.Ins.PlayMode);
                if (!b) return false;
            }
            return true;
        }

        public void ForEachPackage(Action<YooPackage> fn)
        {
            _Packages.ForEachValue(fn);
        }
        public void ForEachPackage(Func<YooPackage, bool> fn)
        {
            _Packages.ForEachValue(fn);
        }
        public async UniTask<bool> ForEachPackage(Func<YooPackage, UniTask<bool>> fn)
        {
            foreach (var _value in _Packages.Values)
            {
                var b = await fn.Invoke(_value);
                if (!b) return false;
            }
            return true;
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
                    var valid = x.Package.IsLocationValid(location);
                    if (!valid) return false;
                    _locationToPackage.Add(location, x);
                    return true;
                });

                // 未命中也要缓存（存 null），否则调用方反复轮询同一个错地址会重复扫描并刷日志。
                if (!_locationToPackage.ContainsKey(location))
                {
                    _locationToPackage.Add(location, null);
                    Log.Error($"资源找不到Package:{location}");
                    return null;
                }
            }

            // 命中 null 说明之前查过且不存在，直接返回，不再重复报错。
            return _locationToPackage[location];
        }

        /// <summary>清单更新后地址集合会变，缓存的归属（含未命中的 null）必须失效。</summary>
        public void ClearLocationCache()
        {
            _locationToPackage.Clear();
        }

        #region 资源卸载
        public void UnloadAllAssetsAsync()
        {
            ForEachPackage(x => x.Package.UnloadAllAssetsAsync());
        }
        public void UnloadUnusedAssetsAsync()
        {
            ForEachPackage(x => x.Package.UnloadUnusedAssetsAsync());
        }
        #endregion
    }
}