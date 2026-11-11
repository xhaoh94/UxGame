using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using YooAsset;

namespace Ux
{
    public enum YooType
    {
        None,
        //主包
        Main,
        //原生文件
        RawFile,
    }
    public interface IYooPackage
    {
        UniTask<bool> Initialize(EPlayMode playMode);
    }
    public class YooMainPackage : YooPackage
    {
        public override YooType YooType => YooType.Main;
        public override string Name => "MainPackage";
    }
    public class YooRawFilePackage : YooPackage
    {
        public override YooType YooType => YooType.RawFile;
        public override string Name => "RawFilePackage";
        // 原生文件包不产 AB，编辑器模拟必须走 VirtualRawBundle，否则会按 AB 去解析源文件。
        protected override EBundleType SimulateBundleType => EBundleType.VirtualRawBundle;
    }

    public abstract class YooPackage : IYooPackage
    {
        public abstract YooType YooType { get; }
        public abstract string Name { get; }

        /// <summary>编辑器模拟构建时用的包裹类型：主包是 AB，原生文件包是 Raw。</summary>
        protected virtual EBundleType SimulateBundleType => EBundleType.VirtualAssetBundle;
        public ResourcePackage Package { get; private set; }
        public string Version { get; set; }
        void _CreatePackage()
        {            
            Package = YooAssets.TryGetPackage(Name, out var package) ? package : null;
            Package ??= YooAssets.CreatePackage(Name);
        }
        async UniTask<bool> IYooPackage.Initialize(EPlayMode playMode)
        {
            // 创建资源包
            _CreatePackage();
            InitializePackageOperation initializationOperation = null;

            switch (playMode)
            {
#if UNITY_EDITOR
                // 编辑器模拟模式
                case EPlayMode.EditorSimulateMode:
                    {
                        var simulateBuildResult = EditorSimulateBuildInvoker.Build(Name, (int)SimulateBundleType);
                        var packageRoot = simulateBuildResult.PackageRootDirectory;
                        var editorFileSystemParams = FileSystemParameters.CreateDefaultEditorFileSystemParameters(packageRoot);
                        var options = new EditorSimulateModeOptions
                        {
                            EditorFileSystemParameters = editorFileSystemParams
                        };
                        initializationOperation = Package.InitializePackageAsync(options);
                        break;
                    }
#endif
                // 单机模式
                case EPlayMode.OfflinePlayMode:
                    {
                        var buildinFileSystemParams = FileSystemParameters.CreateDefaultBuiltinFileSystemParameters();
                        AddBundleDecryptor(buildinFileSystemParams);
                        AddManifestDecryptor(buildinFileSystemParams);
                        var options = new OfflinePlayModeOptions
                        {
                            BuiltinFileSystemParameters = buildinFileSystemParams
                        };
                        initializationOperation = Package.InitializePackageAsync(options);
                        break;
                    }
                // 联机模式
                case EPlayMode.HostPlayMode:
                    {
                        IRemoteService remoteService = new RemoteService(Global.GetHostServerURL(),
                            Global.GetFallbackHostServerURL());
                        var cacheFileSystemParams = FileSystemParameters.CreateDefaultSandboxFileSystemParameters(remoteService);
                        var buildinFileSystemParams = FileSystemParameters.CreateDefaultBuiltinFileSystemParameters();
                        AddBundleDecryptor(cacheFileSystemParams);
                        AddBundleDecryptor(buildinFileSystemParams);
                        AddManifestDecryptor(buildinFileSystemParams);
                        var options = new HostPlayModeOptions
                        {
                            BuiltinFileSystemParameters = buildinFileSystemParams,
                            CacheFileSystemParameters = cacheFileSystemParams
                        };
                        initializationOperation = Package.InitializePackageAsync(options);
                        break;
                    }
                // WebGL运行模式
                case EPlayMode.WebPlayMode:
                    {
                        IRemoteService remoteService = new RemoteService(Global.GetHostServerURL(),
                            Global.GetFallbackHostServerURL());
                        var webRemoteFileSystemParams = FileSystemParameters.CreateDefaultWebNetworkFileSystemParameters(remoteService); //支持跨域下载
                        var webServerFileSystemParams = FileSystemParameters.CreateDefaultWebServerFileSystemParameters();
                        AddBundleDecryptor(webRemoteFileSystemParams);
                        AddBundleDecryptor(webServerFileSystemParams);
                        AddManifestDecryptor(webServerFileSystemParams);
                        var options = new WebPlayModeOptions
                        {
                            WebServerFileSystemParameters = webServerFileSystemParams,
                            WebNetworkFileSystemParameters = webRemoteFileSystemParams
                        };
                        initializationOperation = Package.InitializePackageAsync(options);
                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }
            await initializationOperation;
            if (initializationOperation.Status != EOperationStatus.Succeeded)
            {
                Log.Warning($"{initializationOperation.Error}");
                return false;
            }
            return true;
        }

        /// <summary>v3 把解密拆成独立接口，必须显式注册；兼容层的工厂方法会忽略旧参数。</summary>
        void AddBundleDecryptor(FileSystemParameters parameters)
        {
            if (GameMain.Ins.UseDecryption)
                parameters.AddParameter(EFileSystemParameter.AssetBundleDecryptor, new UxDecryption());
        }

        void AddManifestDecryptor(FileSystemParameters parameters)
        {
            parameters.AddParameter(EFileSystemParameter.ManifestDecryptor, new UxManifestCryptoServices());
        }

        #region 远端地址
        /// <summary>
        /// 远端资源地址查询服务类
        /// </summary>
        private class RemoteService : IRemoteService
        {
            private readonly string _defaultHostServer;
            private readonly string _fallbackHostServer;

            public RemoteService(string defaultHostServer, string fallbackHostServer)
            {
                _defaultHostServer = defaultHostServer;
                _fallbackHostServer = fallbackHostServer;
            }

            IReadOnlyList<string> IRemoteService.GetRemoteUrls(string fileName)
            {
                return new[]
                {
                    $"{_defaultHostServer}/{fileName}",
                    $"{_fallbackHostServer}/{fileName}"
                };
            }
        }
        #endregion
    }
}
