using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using YooAsset;

namespace Ux
{
    public enum YooType
    {
        None,
        Main,//主包
        Code,//代码
        UI,//UI包
        Config,//配置
        RawFile,//原生文件
    }
    public abstract class YooPackage
    {
        public abstract YooType YooType { get; }
        public abstract string Name { get; }
        public abstract Type DecryptionType { get; }
        public abstract EDefaultBuildPipeline EDefaultBuildPipeline { get; }
        public ResourcePackage Package { get; private set; }
        public string Version { get; set; }
        public void CreatePackage()
        {
            Package = YooAssets.TryGetPackage(Name);
            if (Package == null)
                Package = YooAssets.CreatePackage(Name);
            if (YooType.Main == YooType)
            {
                // 设置该资源包为默认的资源包
                YooAssets.SetDefaultPackage(Package);
            }
        }
        public async UniTask Initialize(EPlayMode playMode)
        {
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

            // 如果初始化失败弹出提示界面
            if (initializationOperation.Status != EOperationStatus.Succeed)
            {
                Log.Warning($"{initializationOperation.Error}");
            }
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

    public class YooMainPackage : YooPackage
    {
        public override YooType YooType => YooType.Main;
        public override string Name => "MainPackage";
        public override Type DecryptionType => null;

        public override EDefaultBuildPipeline EDefaultBuildPipeline => EDefaultBuildPipeline.ScriptableBuildPipeline;
    }
    public class YooCodePackage : YooPackage
    {
        public override YooType YooType => YooType.Code;
        public override string Name => "CodePackage";
        public override Type DecryptionType => null;
        public override EDefaultBuildPipeline EDefaultBuildPipeline => EDefaultBuildPipeline.RawFileBuildPipeline;
    }
    public class YooUIPackage : YooPackage
    {
        public override YooType YooType => YooType.UI;
        public override string Name => "UIPackage";
        public override Type DecryptionType => typeof(FileStreamDecryption);
        public override EDefaultBuildPipeline EDefaultBuildPipeline => EDefaultBuildPipeline.ScriptableBuildPipeline;
    }
    public class YooConfigPackage : YooPackage
    {
        public override YooType YooType => YooType.Config;
        public override string Name => "ConfigPackage";
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