using Cysharp.Threading.Tasks;
using System;
using System.IO;
using UnityEngine;
using YooAsset;

namespace Ux
{
    public enum YooType
    {
        None,
        //主包
        Main,
        //原生文件,由于微信小游戏不支持多Pacage,所以暂时不用原生文件。如果要读取原生文件，放到Main里面，用TextAsset方式读取
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
    }

    public abstract class YooPackage : IYooPackage
    {
        public abstract YooType YooType { get; }
        public abstract string Name { get; }                
        public ResourcePackage Package { get; private set; }
        public string Version { get; set; }
        void _CreatePackage()
        {
            Package = YooAssets.TryGetPackage(Name);
            Package ??= YooAssets.CreatePackage(Name);
            if (YooType.Main == YooType)
            {
                // 设置该资源包为默认的资源包
                YooAssets.SetDefaultPackage(Package);
            }
        }
        async UniTask<bool> IYooPackage.Initialize(EPlayMode playMode)
        {
            // 创建资源包            
            _CreatePackage();
            InitializationOperation initializationOperation = null;            
            switch (playMode)
            {
#if UNITY_EDITOR
                // 编辑器模拟模式
                case EPlayMode.EditorSimulateMode:
                    {
                        var simulateBuildResult = EditorSimulateModeHelper.SimulateBuild(Name);
                        var packageRoot = simulateBuildResult.PackageRootDirectory;                        
                        var editorFileSystemParams = FileSystemParameters.CreateDefaultEditorFileSystemParameters(packageRoot);
                        var initParameters = new EditorSimulateModeParameters();
                        initParameters.EditorFileSystemParameters = editorFileSystemParams;
                        initializationOperation = Package.InitializeAsync(initParameters);
                        break;
                    }
#endif
                // 单机模式
                case EPlayMode.OfflinePlayMode:
                    {
                        var buildinFileSystemParams = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
                        var initParameters = new OfflinePlayModeParameters();
                        initParameters.BuildinFileSystemParameters = buildinFileSystemParams;
                        initializationOperation = Package.InitializeAsync(initParameters);
                        break;
                    }
                // 联机模式
                case EPlayMode.HostPlayMode:
                    {
                        IRemoteServices remoteServices = new RemoteServices(Global.GetHostServerURL(),
                            Global.GetFallbackHostServerURL());
                        var buildinFileSystemParams = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
                        var cacheFileSystemParams = FileSystemParameters.CreateDefaultCacheFileSystemParameters(remoteServices);
                        var initParameters = new HostPlayModeParameters();
                        initParameters.BuildinFileSystemParameters = buildinFileSystemParams;
                        initParameters.CacheFileSystemParameters = cacheFileSystemParams;
                        initializationOperation = Package.InitializeAsync(initParameters);
                        break;
                    }
                // WebGL运行模式
                case EPlayMode.WebPlayMode:
                    {
                        IRemoteServices remoteServices = new RemoteServices(Global.GetHostServerURL(),
                            Global.GetFallbackHostServerURL());
                        var webServerFileSystemParams = FileSystemParameters.CreateDefaultWebServerFileSystemParameters();
                        var webRemoteFileSystemParams = FileSystemParameters.CreateDefaultWebRemoteFileSystemParameters(remoteServices); //支持跨域下载
                        var initParameters = new WebPlayModeParameters();
                        initParameters.WebServerFileSystemParameters = webServerFileSystemParams;
                        initParameters.WebRemoteFileSystemParameters = webRemoteFileSystemParams;
                        initializationOperation = Package.InitializeAsync(initParameters);
                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }                        
            await initializationOperation;
            if (initializationOperation.Status != EOperationStatus.Succeed)
            {
                Log.Warning($"{initializationOperation.Error}");
                return false;
            }
            return true;
        }

        #region 远端地址
        /// <summary>
        /// 远端资源地址查询服务类
        /// </summary>
        private class RemoteServices : IRemoteServices
        {
            private readonly string _defaultHostServer;
            private readonly string _fallbackHostServer;

            public RemoteServices(string defaultHostServer, string fallbackHostServer)
            {
                _defaultHostServer = defaultHostServer;
                _fallbackHostServer = fallbackHostServer;
            }
            string IRemoteServices.GetRemoteMainURL(string fileName)
            {
                return $"{_defaultHostServer}/{fileName}";
            }
            string IRemoteServices.GetRemoteFallbackURL(string fileName)
            {
                return $"{_fallbackHostServer}/{fileName}";
            }
        }
        #endregion      
    }    
}