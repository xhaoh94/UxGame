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
            InitializationOperation initializationOperation = null;
            switch (playMode)
            {
                // 编辑器模拟模式
                case EPlayMode.EditorSimulateMode:
                    {
                        var createParameters = new EditorSimulateModeParameters
                        {
                            SimulateManifestFilePath = EditorSimulateModeHelper.SimulateBuild(DefaultBuildPipeline.BuiltinBuildPipelineName, Name)
                        };
                        initializationOperation = Package.InitializeAsync(createParameters);
                        break;
                    }
                // 单机模式
                case EPlayMode.OfflinePlayMode:
                    {
                        var createParameters = new OfflinePlayModeParameters();
                        createParameters.DecryptionServices = new FileStreamDecryption();
                        initializationOperation = Package.InitializeAsync(createParameters);
                        break;
                    }
                // 联机模式
                case EPlayMode.HostPlayMode:
                    {
                        var createParameters = new HostPlayModeParameters
                        {
                            DecryptionServices = new FileStreamDecryption(),
                            BuildinQueryServices = new GameQueryServices(),
                            //DeliveryQueryServices = new DefaultDeliveryQueryServices(),
                            RemoteServices = new RemoteServices(Global.GetHostServerURL(), Global.GetFallbackHostServerURL())
                        };
                        initializationOperation = Package.InitializeAsync(createParameters);
                        break;
                    }
                // WebGL运行模式
                case EPlayMode.WebPlayMode:
                    {
                        var createParameters = new WebPlayModeParameters()
                        {
                            DecryptionServices = new FileStreamDecryption(),
                            BuildinQueryServices = new GameQueryServices(),
                            RemoteServices = new RemoteServices(Global.GetHostServerURL(), Global.GetFallbackHostServerURL()),
                        };
                        initializationOperation = Package.InitializeAsync(createParameters);

                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }
            yield return initializationOperation;

            // 如果初始化失败弹出提示界面
            if (initializationOperation.Status != EOperationStatus.Succeed)
            {
                Log.Warning($"{initializationOperation.Error}");
                //PatchEventDefine.InitializeFailed.SendEventMessage();
            }
            else
            {
                //_machine.ChangeState<FsmUpdatePackageVersion>();
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

        #region 解密
        /// <summary>
        /// 资源文件流加载解密类
        /// </summary>
        private class FileStreamDecryption : IDecryptionServices
        {
            /// <summary>
            /// 同步方式获取解密的资源包对象
            /// 注意：加载流对象在资源包对象释放的时候会自动释放
            /// </summary>
            AssetBundle IDecryptionServices.LoadAssetBundle(DecryptFileInfo fileInfo, out Stream managedStream)
            {
                BundleStream bundleStream = new BundleStream(fileInfo.FileLoadPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                managedStream = bundleStream;
                return AssetBundle.LoadFromStream(bundleStream, fileInfo.ConentCRC, GetManagedReadBufferSize());
            }

            /// <summary>
            /// 异步方式获取解密的资源包对象
            /// 注意：加载流对象在资源包对象释放的时候会自动释放
            /// </summary>
            AssetBundleCreateRequest IDecryptionServices.LoadAssetBundleAsync(DecryptFileInfo fileInfo, out Stream managedStream)
            {
                BundleStream bundleStream = new BundleStream(fileInfo.FileLoadPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                managedStream = bundleStream;
                return AssetBundle.LoadFromStreamAsync(bundleStream, fileInfo.ConentCRC, GetManagedReadBufferSize());
            }

            private static uint GetManagedReadBufferSize()
            {
                return 1024;
            }
        }

        /// <summary>
        /// 资源文件偏移加载解密类
        /// </summary>
        private class FileOffsetDecryption : IDecryptionServices
        {
            /// <summary>
            /// 同步方式获取解密的资源包对象
            /// 注意：加载流对象在资源包对象释放的时候会自动释放
            /// </summary>
            AssetBundle IDecryptionServices.LoadAssetBundle(DecryptFileInfo fileInfo, out Stream managedStream)
            {
                managedStream = null;
                return AssetBundle.LoadFromFile(fileInfo.FileLoadPath, fileInfo.ConentCRC, GetFileOffset());
            }

            /// <summary>
            /// 异步方式获取解密的资源包对象
            /// 注意：加载流对象在资源包对象释放的时候会自动释放
            /// </summary>
            AssetBundleCreateRequest IDecryptionServices.LoadAssetBundleAsync(DecryptFileInfo fileInfo, out Stream managedStream)
            {
                managedStream = null;
                return AssetBundle.LoadFromFileAsync(fileInfo.FileLoadPath, fileInfo.ConentCRC, GetFileOffset());
            }

            private static ulong GetFileOffset()
            {
                return 32;
            }
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
        #endregion

        //#region 默认的分发资源查询服务
        ///// <summary>
        ///// 默认的分发资源查询服务类
        ///// </summary>
        //private class DefaultDeliveryQueryServices : IDeliveryQueryServices
        //{
        //    public DeliveryFileInfo GetDeliveryFileInfo(string packageName, string fileName)
        //    {
        //        throw new NotImplementedException();
        //    }
        //    public bool QueryDeliveryFiles(string packageName, string fileName)
        //    {
        //        return false;
        //    }
        //}
        //#endregion
    }
}