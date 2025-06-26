using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using YooAsset;
using YooAsset.Editor;
namespace Ux.Editor.Build.Version
{
    public partial class VersionWindow
    {
        string BuildOutputRoot
        {
            get
            {
                if (!string.IsNullOrEmpty(SelectItem.BundlePath))
                {
                    return SelectItem.BundlePath;
                }
                return AssetBundleBuilderHelper.GetDefaultBuildOutputRoot();
            }
        }
        string TemBuildOutputRoot
        {
            get
            {
                return BuildOutputRoot + "_Tem";
            }
        }

        string StreamingAssetsRoot
        {
            get
            {
                return AssetBundleBuilderHelper.GetStreamingAssetsRoot();
            }
        }

        string SandboxRoot
        {
            get
            {
                string projectPath = Path.GetDirectoryName(UnityEngine.Application.dataPath);
                projectPath = PathUtility.RegularPath(projectPath);
                return PathUtility.Combine(projectPath, YooAssetSettingsData.Setting.DefaultYooFolderName);
            }
        }

        partial void _OnClearClick()
        {
            if (EditorUtility.DisplayDialog("��ʾ", $"�Ƿ����ù�����\n���û�ɾ�����й������Ŀ¼��", "ȷ��", "ȡ��"))
            {
                if (Directory.Exists(SelectItem.ExePath))
                {
                    Directory.Delete(SelectItem.ExePath, true);
                    Log.Debug($"ɾ������Ŀ¼��{SelectItem.ExePath}");
                }
                if (Directory.Exists(SelectItem.CopyPath))
                {
                    Directory.Delete(SelectItem.CopyPath, true);
                    Log.Debug($"ɾ������Ŀ¼��{SelectItem.CopyPath}");
                }

                if (EditorTools.DeleteDirectory(SandboxRoot))
                {
                    Log.Debug($"ɾ��ɳ�л��棺{SandboxRoot}");
                }

                // ɾ��ƽ̨��Ŀ¼                        
                if (EditorTools.DeleteDirectory(BuildOutputRoot))
                {
                    Log.Debug($"ɾ����ԴĿ¼��{BuildOutputRoot}");
                }
                if (EditorTools.DeleteDirectory(TemBuildOutputRoot))
                {
                    Log.Debug($"ɾ����ʱ��ԴĿ¼��{TemBuildOutputRoot}");
                }

                EditorTools.ClearFolder(StreamingAssetsRoot);

                SelectItem.Clear();
                RefreshView();
                AssetDatabase.Refresh();
                Log.Debug("���óɹ�");
            }
        }

        private async UniTask<bool> BuildRes(BuildTarget buildTarget)
        {
            var packageValue = buildPackage.value;
            if (packageValue == 0)
            {
                Log.Error("û��ѡ��Ҫ��������Դ��");
                return false;
            }

            if (IsForceRebuild)
            {
                EditorTools.ClearFolder(StreamingAssetsRoot);

                if (tgClearSandBox.value)
                {
                    if (EditorTools.DeleteDirectory(SandboxRoot))
                    {
                        Log.Debug("---------------------------------------->ɾ��ɳ�л���<---------------------------------------");
                    }
                }
            }

            List<string> packages = new List<string>();

            if (IsExportExecutable || packageValue == -1)
            {
                packages.AddRange(_buildPackageNames);
            }
            else
            {
                var array = Convert.ToString(buildPackage.value, 2).ToCharArray().Reverse();
                var pCnt = array.Count();
                for (int i = 0; i < _buildPackageNames.Count; i++)
                {
                    if (i < pCnt)
                    {
                        if (array.ElementAt(i) == '1')
                        {
                            packages.Add(_buildPackageNames[i]);
                        }
                    }
                }
            }

            if (packages.Count > 0)
            {
                UniTask<bool> CollectSVC(string packageName)
                {
                    Log.Debug($"---------------------------------------->{packageName}:�ռ���ɫ������<---------------------------------------");
                    var collectPath = $"Assets/Data/Art/ShaderVariants/{packageName}";
                    string savePath = $"{collectPath}/{packageName}SV.shadervariants";
                    ShaderVariantCollectorSetting.SetFileSavePath(packageName, savePath);
                    int processCapacity = ShaderVariantCollectorSetting.GeProcessCapacity(packageName);
                    if (processCapacity <= 0)
                    {
                        processCapacity = 1000;
                        ShaderVariantCollectorSetting.SetProcessCapacity(packageName, processCapacity);
                    }
                    var task = AutoResetUniTaskCompletionSource<bool>.Create();
                    Action callback = () =>
                    {
                        Log.Debug("------------------------------------>����YooAsset ��ɫ������<------------------------------");
                        var packages = AssetBundleCollectorSettingData.Setting.Packages;
                        var package = packages.Find(x => x.PackageName == packageName);
                        bool _IsDirty = false;
                        if (package != null)
                        {
                            var group = package.Groups.Find(x => x.GroupName == "ShaderVariant");
                            if (group == null)
                            {
                                group = new AssetBundleCollectorGroup();
                                group.AssetTags = "builtin";
                                group.GroupDesc = "��ɫ��";
                                group.GroupName = "ShaderVariant";
                                package.Groups.Add(group);
                            }
                            var collector = group.Collectors.Find(x => x.CollectPath == collectPath);
                            if (collector == null)
                            {
                                collector = new AssetBundleCollector();
                                collector.CollectPath = collectPath;
                                collector.AddressRuleName = nameof(AddressByFileName);
                                collector.PackRuleName = nameof(PackShaderVariants);
                                collector.FilterRuleName = nameof(CollectShaderVariants);
                                group.Collectors.Add(collector);
                                _IsDirty = true;
                            }
                        }

                        task.TrySetResult(_IsDirty);
                    };
                    ShaderVariantCollector.Run(savePath, packageName, processCapacity, callback);
                    return task.Task;
                }

                bool IsDirty = false;
                foreach (var _packageName in packages)
                {
                    var packageSetting = SelectItem.GetPackageSetting(_packageName);
                    if (packageSetting.IsCollectShaderVariant)
                    {
                        var temb = await CollectSVC(_packageName);
                        if (temb && !IsDirty)
                        {
                            IsDirty = temb;
                        }
                    }
                }
                if (IsDirty)
                {
                    AssetBundleCollectorSettingData.SaveFile();
                }
                Log.Debug("---------------------------------------->��ʼ��Դ���<---------------------------------------");
                foreach (var package in packages)
                {
                    Log.Debug($"---------------------------------------->{package}:��ʼ������Դ��<---------------------------------------");
                    if (!BuildRes(buildTarget, package)) return false;
                }
                Log.Debug("---------------------------------------->�����Դ���<---------------------------------------");
            }
            return true;
        }

        private bool BuildRes(BuildTarget buildTarget, string packageName)
        {
            if (IsForceRebuild)
            {
                // ɾ��ƽ̨��Ŀ¼            
                string platformDirectory = $"{BuildOutputRoot}/{packageName}/{buildTarget}";
                if (EditorTools.DeleteDirectory(platformDirectory))
                {
                    Log.Debug($"ɾ��ƽ̨��Ŀ¼��{platformDirectory}");
                }
            }

            var packageSetting = SelectItem.GetPackageSetting(packageName);

            var buildOutputRoot = BuildOutputRoot;
            var temOutputRoot = TemBuildOutputRoot;
            var versionFileName = YooAssetSettingsData.GetPackageVersionFileName(packageName);
            var outputDir = "Output";
            var buildPath = $"{buildOutputRoot}/{buildTarget}/{packageName}";
            var temBuildPath = $"{temOutputRoot}/{buildTarget}/{packageName}";
            var nowVersion = txtVersion.value;
            var lastVersion = string.Empty;
            var versionFile = $"{buildPath}/{outputDir}/{versionFileName}";
            if (File.Exists(versionFile))
            {
                lastVersion = FileUtility.ReadAllText(versionFile);
            }

            if (lastVersion == nowVersion)
            {
                nowVersion = AddVersion(lastVersion);
                txtVersion.SetValueWithoutNotify(nowVersion);
                Log.Warning("�汾���޸��£��������Զ����£�");
            }

            BuildParameters buildParameters = null;
            IBuildPipeline pipeline = null;

            switch (packageSetting.PiplineOption)
            {
                case EBuildPipeline.BuiltinBuildPipeline:
                    buildParameters = new BuiltinBuildParameters()
                    {
                        CompressOption = packageSetting.CompressOption,
                    };
                    pipeline = new BuiltinBuildPipeline();
                    break;
                case EBuildPipeline.ScriptableBuildPipeline:
                    buildParameters = new ScriptableBuildParameters()
                    {                        
                        CompressOption = packageSetting.CompressOption,
                    };
                    pipeline = new ScriptableBuildPipeline();
                    break;
                case EBuildPipeline.RawFileBuildPipeline:
                    buildParameters = new RawFileBuildParameters();
                    pipeline = new RawFileBuildPipeline();
                    break;
                default:
                    Log.Error("δ֪�Ĺ���ģʽ");
                    return false;
            }            
            buildParameters.BuildOutputRoot = temOutputRoot;
            buildParameters.BuildinFileRoot = StreamingAssetsRoot;
            buildParameters.BuildPipeline = packageSetting.PiplineOption.ToString();
            buildParameters.BuildTarget = buildTarget;
            buildParameters.PackageName = packageName;
            buildParameters.PackageVersion = nowVersion;
            buildParameters.VerifyBuildingResult = true;
            buildParameters.EnableSharePackRule = true;            
            buildParameters.FileNameStyle = packageSetting.NameStyleOption;
            buildParameters.BuildinFileCopyOption = IsForceRebuild
                ? EBuildinFileCopyOption.OnlyCopyByTags : EBuildinFileCopyOption.None;
            buildParameters.BuildinFileCopyParams = packageSetting.BuildTags;
            buildParameters.EncryptionServices = CreateEncryptionServicesInstance(packageSetting.EncyptionClassName);
            buildParameters.ClearBuildCacheFiles = IsForceRebuild ; //�������²����������棬��������������������ߴ���ٶȣ�
            buildParameters.UseAssetDependencyDB = SelectItem.IsUseDb; //ʹ����Դ������ϵ���ݿ⣬������ߴ���ٶȣ�



            var result = pipeline.Run(buildParameters, true);
            if (!result.Success)
            {
                var temVersion = FileUtility.ReadAllText(versionFile);
                if (temVersion == nowVersion)//����ʧ���ˣ�����YooAssetȴ�Ѱ汾�Ÿ��޸���,��ʱ����Ҫ�Ѱ汾�Żع�һ��
                {
                    File.WriteAllText(versionFile, lastVersion, System.Text.Encoding.UTF8);
                }
                Log.Error($"{packageName}:������Դ��ʧ��");
                return false;
            }

            //������link.xml
            //if (IsExportExecutable)
            //{
            //    var tLinkPath = $"{buildPath}/{nowVersion}/link.xml";
            //    if (File.Exists(tLinkPath))
            //    {
            //        var linkPath = $"{Application.dataPath}/Main/YooAsset/{packageName}";
            //        if (!Directory.Exists(linkPath))
            //            Directory.CreateDirectory(linkPath);
            //        File.Copy(tLinkPath, $"{linkPath}/link.xml", true);
            //    }
            //}



            var temBuildPathVersion = $"{temBuildPath}/{nowVersion}";
            var buildPathVersion = $"{buildPath}/{nowVersion}";
            Directory.CreateDirectory(buildPathVersion);
            var temfiles = Directory.GetFiles(temBuildPathVersion).ToList();
            foreach (var file in temfiles)
            {
                var temFile = PathUtility.RegularPath(file);
                var index = temFile.LastIndexOf("/");
                var fileName = temFile.Substring(index + 1);
                File.Copy(file, Path.Combine(buildPathVersion, fileName), true);
            }

            var nowJsonFileName = YooAssetSettingsData.GetManifestJsonFileName(packageName, nowVersion);
            var nowHashFileName = YooAssetSettingsData.GetPackageHashFileName(packageName, nowVersion);
            var nowBinaryFileName = YooAssetSettingsData.GetManifestBinaryFileName(packageName, nowVersion);
            var lastBinaryFileName = YooAssetSettingsData.GetManifestBinaryFileName(packageName, lastVersion);

            List<string> files = null;
            if (!string.IsNullOrEmpty(lastVersion))
            {
                files = new List<string>();
                var path1 = $"{buildPath}/{lastVersion}/{lastBinaryFileName}";
                var path2 = $"{temBuildPath}/{nowVersion}/{nowBinaryFileName}";
                List<string> changedList = new List<string>();
                PackageCompare.CompareManifest(path1, path2, changedList);
                var diffPath = $"{buildPath}/Difference/{lastVersion}_{nowVersion}";
                if (Directory.Exists(diffPath))
                {
                    Directory.Delete(diffPath, true);
                }
                Directory.CreateDirectory(diffPath);

                foreach (var file in changedList)
                {
                    var temFile = $"{temBuildPathVersion}/{file}";
                    File.Copy(temFile, $"{diffPath}/{file}", true);
                    files.Add(temFile);
                }
                File.Copy($"{temBuildPathVersion}/{versionFileName}", $"{diffPath}/{versionFileName}", true);
                File.Copy($"{temBuildPathVersion}/{nowBinaryFileName}", $"{diffPath}/{nowBinaryFileName}", true);
                File.Copy($"{temBuildPathVersion}/{nowHashFileName}", $"{diffPath}/{nowHashFileName}", true);
                File.Copy($"{temBuildPathVersion}/{nowJsonFileName}", $"{diffPath}/{nowJsonFileName}", true);

                files.Add($"{temBuildPathVersion}/{versionFileName}");
                files.Add($"{temBuildPathVersion}/{nowBinaryFileName}");
                files.Add($"{temBuildPathVersion}/{nowHashFileName}");
                files.Add($"{temBuildPathVersion}/{nowJsonFileName}");
            }

            if (files == null)
            {
                files = temfiles;
            }

            List<string> desPathList = new List<string>();
            var tPath = $"{buildPath}/{outputDir}";
            if (!Directory.Exists(tPath))
            {
                Directory.CreateDirectory(tPath);
            }
            desPathList.Add(tPath);
            if (tgCopy.value && !string.IsNullOrEmpty(SelectItem.CopyPath))
            {
                tPath = $"{SelectItem.CopyPath}/{buildTarget}";
                if (!Directory.Exists(tPath))
                {
                    Directory.CreateDirectory(tPath);
                }
                desPathList.Add(tPath);
            }

            var reportFileName = YooAssetSettingsData.GetBuildReportFileName(packageName, nowVersion);
            foreach (var file in files)
            {
                var temFile = PathUtility.RegularPath(file);
                var index = temFile.LastIndexOf("/");
                var fileName = temFile.Substring(index + 1);
                if (fileName == "link.xml") continue;
                if (fileName == "buildlogtep.json") continue;
                if (fileName == reportFileName) continue;
                //if (fileName == nowJsonFileName) continue;
                foreach (var desPath in desPathList)
                {
                    File.Copy(file, Path.Combine(desPath, fileName), true);
                }
            }

            if (Directory.Exists(temOutputRoot))
            {
                Directory.Delete(temOutputRoot, true);
            }
            return true;
        }

        #region �����������
        // �����������
        private List<string> GetBuildPackageNames()
        {
            List<string> result = new List<string>();
            foreach (var package in AssetBundleCollectorSettingData.Setting.Packages)
            {
                result.Add(package.PackageName);
            }
            return result;
        }

        private IEncryptionServices CreateEncryptionServicesInstance(string encyptionClassName)
        {
            var encryptionClassTypes = EditorTools.GetAssignableTypes(typeof(IEncryptionServices));
            var classType = encryptionClassTypes.Find(x => x.FullName.Equals(encyptionClassName));
            if (classType != null)
                return (IEncryptionServices)Activator.CreateInstance(classType);
            else
                return null;
        }
        ////private ISharedPackRule CreateSharedPackRuleInstance()
        ////{
        ////    if (_sharedPackRule.index < 0)
        ////        return null;
        ////    var classType = _sharedPackRuleClassTypes[_sharedPackRule.index];
        ////    return (ISharedPackRule)Activator.CreateInstance(classType);
        ////}

        #endregion
    }
}
