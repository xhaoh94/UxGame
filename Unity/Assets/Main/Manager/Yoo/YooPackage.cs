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
        public override EDefaultBuildPipeline EDefaultBuildPipeline => EDefaultBuildPipeline.ScriptableBuildPipeline;
    }
    public class YooRawFilePackage : YooPackage
    {
        public override YooType YooType => YooType.RawFile;
        public override string Name => "RawFilePackage";
        public override EDefaultBuildPipeline EDefaultBuildPipeline => EDefaultBuildPipeline.RawFileBuildPipeline;
    }

    public abstract class YooPackage : IYooPackage
    {
        public abstract YooType YooType { get; }
        public abstract string Name { get; }        
        public abstract EDefaultBuildPipeline EDefaultBuildPipeline { get; }
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
                        var simulateBuildResult = EditorSimulateModeHelper.SimulateBuild(EDefaultBuildPipeline, Name);
                        var createParameters = new EditorSimulateModeParameters();
                        createParameters.EditorFileSystemParameters = FileSystemParameters.CreateDefaultEditorFileSystemParameters(simulateBuildResult);
                        initializationOperation = Package.InitializeAsync(createParameters);
                        break;
                    }
#endif
                // 单机模式
                case EPlayMode.OfflinePlayMode:
                    {
                        var createParameters = new OfflinePlayModeParameters();
                        createParameters.BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
                        initializationOperation = Package.InitializeAsync(createParameters);
                        break;
                    }
                // 联机模式
                case EPlayMode.HostPlayMode:
                    {
                        IRemoteServices remoteServices = new RemoteServices(Global.GetHostServerURL(),
                            Global.GetFallbackHostServerURL());
                        var createParameters = new HostPlayModeParameters();
                        createParameters.BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
                        createParameters.CacheFileSystemParameters = FileSystemParameters.CreateDefaultCacheFileSystemParameters(remoteServices);
                        initializationOperation = Package.InitializeAsync(createParameters);
                        break;
                    }
                // WebGL运行模式
                case EPlayMode.WebPlayMode:
                    {
                        var createParameters = new WebPlayModeParameters();
#if UNITY_WEBGL && WEIXINMINIGAME && !UNITY_EDITOR
			            IRemoteServices remoteServices = new RemoteServices(Global.GetHostServerURL(),
                            Global.GetFallbackHostServerURL());
                        createParameters.WebFileSystemParameters = WechatFileSystemCreater.CreateWechatFileSystemParameters(remoteServices);
#else
                        createParameters.WebFileSystemParameters = FileSystemParameters.CreateDefaultWebFileSystemParameters();
#endif
                        initializationOperation = Package.InitializeAsync(createParameters);
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

        #region 解密
        /// <summary>
        /// 资源文件流加载解密类
        /// </summary>
        class FileStreamDecryption : IDecryptionServices
        {
            /// <summary>
            /// 同步方式获取解密的资源包对象
            /// 注意：加载流对象在资源包对象释放的时候会自动释放
            /// </summary>
            AssetBundle IDecryptionServices.LoadAssetBundle(DecryptFileInfo fileInfo, out Stream managedStream)
            {
                BundleStream bundleStream = new BundleStream(fileInfo.FileLoadPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                managedStream = bundleStream;
                return AssetBundle.LoadFromStream(bundleStream, fileInfo.FileLoadCRC, GetManagedReadBufferSize());
            }

            /// <summary>
            /// 异步方式获取解密的资源包对象
            /// 注意：加载流对象在资源包对象释放的时候会自动释放
            /// </summary>
            AssetBundleCreateRequest IDecryptionServices.LoadAssetBundleAsync(DecryptFileInfo fileInfo, out Stream managedStream)
            {
                BundleStream bundleStream = new BundleStream(fileInfo.FileLoadPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                managedStream = bundleStream;
                return AssetBundle.LoadFromStreamAsync(bundleStream, fileInfo.FileLoadCRC, GetManagedReadBufferSize());
            }

            /// <summary>
            /// 获取解密的字节数据
            /// </summary>
            byte[] IDecryptionServices.ReadFileData(DecryptFileInfo fileInfo)
            {
                throw new System.NotImplementedException();
            }

            /// <summary>
            /// 获取解密的文本数据
            /// </summary>
            string IDecryptionServices.ReadFileText(DecryptFileInfo fileInfo)
            {
                throw new System.NotImplementedException();
            }

            private static uint GetManagedReadBufferSize()
            {
                return 1024;
            }
        }

        /// <summary>
        /// 资源文件偏移加载解密类
        /// </summary>
        class FileOffsetDecryption : IDecryptionServices
        {
            /// <summary>
            /// 同步方式获取解密的资源包对象
            /// 注意：加载流对象在资源包对象释放的时候会自动释放
            /// </summary>
            AssetBundle IDecryptionServices.LoadAssetBundle(DecryptFileInfo fileInfo, out Stream managedStream)
            {
                managedStream = null;
                return AssetBundle.LoadFromFile(fileInfo.FileLoadPath, fileInfo.FileLoadCRC, GetFileOffset());
            }

            /// <summary>
            /// 异步方式获取解密的资源包对象
            /// 注意：加载流对象在资源包对象释放的时候会自动释放
            /// </summary>
            AssetBundleCreateRequest IDecryptionServices.LoadAssetBundleAsync(DecryptFileInfo fileInfo, out Stream managedStream)
            {
                managedStream = null;
                return AssetBundle.LoadFromFileAsync(fileInfo.FileLoadPath, fileInfo.FileLoadCRC, GetFileOffset());
            }

            /// <summary>
            /// 获取解密的字节数据
            /// </summary>
            byte[] IDecryptionServices.ReadFileData(DecryptFileInfo fileInfo)
            {
                throw new System.NotImplementedException();
            }

            /// <summary>
            /// 获取解密的文本数据
            /// </summary>
            string IDecryptionServices.ReadFileText(DecryptFileInfo fileInfo)
            {
                throw new System.NotImplementedException();
            }

            private static ulong GetFileOffset()
            {
                return 32;
            }
        }
        #endregion
    }

    /// <summary>
    /// 资源文件解密流
    /// </summary>
    public class BundleStream : FileStream
    {
        public const byte KEY = 64;

        public BundleStream(string path, FileMode mode, FileAccess access, FileShare share) : base(path, mode, access, share)
        {
        }
        public BundleStream(string path, FileMode mode) : base(path, mode)
        {
        }

        public override int Read(byte[] array, int offset, int count)
        {
            var index = base.Read(array, offset, count);
            for (int i = 0; i < array.Length; i++)
            {
                array[i] ^= KEY;
            }
            return index;
        }
    }
}