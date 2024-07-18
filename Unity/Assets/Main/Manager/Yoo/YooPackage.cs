using Cysharp.Threading.Tasks;
using System;
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
    public abstract class YooPackage : IYooPackage
    {
        public abstract YooType YooType { get; }
        public abstract string Name { get; }
        public abstract Type DecryptionType { get; }
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

            InitializeParameters initializeParameters = null;
            IDecryptionServices decryptionServices = null;
            if (DecryptionType != null)
            {
                decryptionServices = Activator.CreateInstance(DecryptionType) as IDecryptionServices;
            }
            switch (playMode)
            {
#if UNITY_EDITOR
                // 编辑器模拟模式
                case EPlayMode.EditorSimulateMode:
                    {
                        initializeParameters = new EditorSimulateModeParameters
                        {
                            SimulateManifestFilePath = EditorSimulateModeHelper.SimulateBuild(EDefaultBuildPipeline, Name)
                        };
                        break;
                    }
#endif
                // 单机模式
                case EPlayMode.OfflinePlayMode:
                    {
                        initializeParameters = new OfflinePlayModeParameters();
                        break;
                    }
                // 联机模式
                case EPlayMode.HostPlayMode:
                    {
                        initializeParameters = new HostPlayModeParameters
                        {
                            BuildinQueryServices = new GameQueryServices(),
                            DeliveryQueryServices = new DeliveryQueryServices(),
                            DeliveryLoadServices = new DeliveryLoadServices(),
                            RemoteServices = new RemoteServices(Global.GetHostServerURL(), Global.GetFallbackHostServerURL())
                        };
                        break;
                    }
                // WebGL运行模式
                case EPlayMode.WebPlayMode:
                    {
                        initializeParameters = new WebPlayModeParameters()
                        {
                            BuildinQueryServices = new GameQueryServices(),
                            RemoteServices = new RemoteServices(Global.GetHostServerURL(), Global.GetFallbackHostServerURL()),
                        };
                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }
            initializeParameters.DecryptionServices = decryptionServices;
            var initializationOperation = Package.InitializeAsync(initializeParameters);
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

    public interface IYooPackage
    {
        UniTask<bool> Initialize(EPlayMode playMode);
    }
    public class YooMainPackage : YooPackage
    {
        public override YooType YooType => YooType.Main;
        public override string Name => "MainPackage";
        public override Type DecryptionType => null;

        public override EDefaultBuildPipeline EDefaultBuildPipeline => EDefaultBuildPipeline.ScriptableBuildPipeline;
    }  
    public class YooRawFilePackage : YooPackage
    {
        public override YooType YooType => YooType.RawFile;
        public override string Name => "RawFilePackage";
        public override Type DecryptionType => null;
        public override EDefaultBuildPipeline EDefaultBuildPipeline => EDefaultBuildPipeline.RawFileBuildPipeline;
    }
}