using System;
using System.Collections;
using System.IO;
using UnityEngine;
using YooAsset;

namespace Ux
{
    public enum ResType
    {
        Main,//主包
        UI//UI包
    }
    public abstract class ResPackage
    {
        public abstract ResType ResType { get; }
        public abstract string Name { get; }
        public abstract Type DecryptionType { get; }
        public ResourcePackage Package { get; private set; }
        public string Version { get; set; }

        public void CreatePackage()
        {
            Package = YooAssets.TryGetPackage(Name);
            if (Package == null)
                Package = YooAssets.CreatePackage(Name);
            if (ResType.Main == ResType)
            {
                // 设置该资源包为默认的资源包
                YooAssets.SetDefaultPackage(Package);
            }
        }
        public IEnumerator Initialize(EPlayMode playMode)
        {
            InitializeParameters initializeParameters = null;
            IDecryptionServices decryptionServices = null;
            if (DecryptionType != null)
            {
                decryptionServices = Activator.CreateInstance(DecryptionType) as IDecryptionServices;
            }
            switch (playMode)
            {
                // 编辑器模拟模式
                case EPlayMode.EditorSimulateMode:
                    {
                        initializeParameters = new EditorSimulateModeParameters
                        {
                            SimulateManifestFilePath = EditorSimulateModeHelper.SimulateBuild(
                                EDefaultBuildPipeline.ScriptableBuildPipeline, Name)
                        };
                        break;
                    }
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
            yield return initializationOperation;

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

    public class ResMainPackage : ResPackage
    {
        public override ResType ResType => ResType.Main;
        public override string Name => "MainPackage";
        public override Type DecryptionType => null;
    }

    public class ResUIPackage : ResPackage
    {
        public override ResType ResType => ResType.UI;
        public override string Name => "UIPackage";
        public override Type DecryptionType => typeof(FileStreamDecryption);
    }
}