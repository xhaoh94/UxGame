using System;
using System.Collections;
using System.IO;
using YooAsset;

namespace Ux
{
    public enum ResType
    {
        Main,//主包
        UI//UI包
    }
    public class ResPackage
    {
        public ResType ResType { get; private set; }
        public string Name { get; private set; }
        public ResourcePackage Package { get; private set; }
        public string Version { get; set; }

        public ResPackage(ResType _resType, string name)
        {
            ResType = _resType;
            Name = name;
        }
        public void CreatePackage()
        {
            Package = YooAssets.CreatePackage(Name);
            if (ResType.Main == ResType)
            {
                // 设置该资源包为默认的资源包
                YooAssets.SetDefaultPackage(Package);
            }
        }
        public IEnumerator Initialize(EPlayMode playMode)
        {
            switch (playMode)
            {
                // 编辑器模拟模式
                case EPlayMode.EditorSimulateMode:
                    {
                        var createParameters = new EditorSimulateModeParameters
                        {
                            SimulateManifestFilePath = EditorSimulateModeHelper.SimulateBuild(Name)
                        };
                        yield return Package.InitializeAsync(createParameters);
                        break;
                    }
                // 单机模式
                case EPlayMode.OfflinePlayMode:
                    {
                        var createParameters = new OfflinePlayModeParameters();
                        yield return Package.InitializeAsync(createParameters);
                        break;
                    }
                // 联机模式
                case EPlayMode.HostPlayMode:
                    {
                        var createParameters = new HostPlayModeParameters
                        {
                            DecryptionServices = new GameDecryptionServices(),
                            QueryServices = new GameQueryServices(),
                            RemoteServices = new RemoteServices(Global.GetHostServerURL(), Global.GetFallbackHostServerURL())
                        };
                        yield return Package.InitializeAsync(createParameters);
                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException();
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

        #region 加密
        private class GameDecryptionServices : IDecryptionServices
        {
            public ulong LoadFromFileOffset(DecryptFileInfo fileInfo)
            {
                return 32;
            }

            public byte[] LoadFromMemory(DecryptFileInfo fileInfo)
            {
                throw new NotImplementedException();
            }

            Stream IDecryptionServices.LoadFromStream(DecryptFileInfo fileInfo)
            {
                BundleStream bundleStream = new BundleStream(fileInfo.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                return bundleStream;
            }

            public uint GetManagedReadBufferSize()
            {
                return 1024;
            }


        }
        #endregion
    }
}