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
                return BundleBuilderHelper.GetDefaultBuildOutputRoot();
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
                return BundleBuilderHelper.GetStreamingAssetsRoot();
            }
        }

        string SandboxRoot
        {
            get
            {
                string projectPath = Path.GetDirectoryName(UnityEngine.Application.dataPath);
                projectPath = EditorPathUtility.GetRegularPath(projectPath);
                return PathUtility.Combine(projectPath, YooAssetConfiguration.GetYooFolderName());
            }
        }

        private void _OnClearClick()
        {
            if (EditorUtility.DisplayDialog("提示", $"是否清空构建缓存\n将删除所有构建相关的目录？", "确定", "取消"))
            {
                if (Directory.Exists(SelectItem.ExePath))
                {
                    Directory.Delete(SelectItem.ExePath, true);
                    Log.Debug($"删除构建目录：{SelectItem.ExePath}");
                }
                if (Directory.Exists(SelectItem.CopyPath))
                {
                    Directory.Delete(SelectItem.CopyPath, true);
                    Log.Debug($"删除复制目录：{SelectItem.CopyPath}");
                }

                if (EditorTools.DeleteDirectory(SandboxRoot))
                {
                    Log.Debug($"删除沙盒缓存：{SandboxRoot}");
                }

                // 删除平台目录                        
                if (EditorTools.DeleteDirectory(BuildOutputRoot))
                {
                    Log.Debug($"删除资源目录：{BuildOutputRoot}");
                }
                if (EditorTools.DeleteDirectory(TemBuildOutputRoot))
                {
                    Log.Debug($"删除临时资源目录：{TemBuildOutputRoot}");
                }

                EditorTools.ClearFolder(StreamingAssetsRoot);

                SelectItem.Clear();
                RefreshView();
                AssetDatabase.Refresh();
                Log.Debug("清除成功");
            }
        }

        private UniTask<bool> BuildRes(BuildTarget buildTarget)
        {
            var packageValue = buildPackage.value;
            if (packageValue == 0)
            {
                Log.Error("没有选择要构建的资源包");
                return UniTask.FromResult(false);
            }

            if (IsForceRebuild)
            {
                EditorTools.ClearFolder(StreamingAssetsRoot);

                if (tgClearSandBox.value)
                {
                    if (EditorTools.DeleteDirectory(SandboxRoot))
                    {
                        Log.Debug("---------------------------------------->删除沙盒缓存<---------------------------------------");
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
                // v3 删除了 ShaderVariantCollector 示例；着色器变体收集改由收集器里的
                // PackShaderVariants + CollectShaderVariants 规则处理，构建前不再自动生成 .shadervariants。
                Log.Debug("---------------------------------------->开始构建资源<---------------------------------------");
                foreach (var package in packages)
                {
                    Log.Debug($"---------------------------------------->{package}:开始构建资源<---------------------------------------");
                    if (!BuildRes(buildTarget, package)) return UniTask.FromResult(false);
                }
                Log.Debug("---------------------------------------->完成资源构建<---------------------------------------");
            }
            return UniTask.FromResult(true);
        }

        void CompareManifest(string path1, string path2, List<PackageBundle> changeList, List<PackageBundle> newList)
        {
            changeList.Clear();
            newList.Clear();

            // 加载补丁清单1
            byte[] bytesData1 = FileUtility.ReadAllBytes(path1);
            PackageManifest manifest1 = PackageManifestHelper.DeserializeManifestFromBinary(bytesData1, null); //TODO 自行处理解密

            // 加载补丁清单1
            byte[] bytesData2 = FileUtility.ReadAllBytes(path2);
            PackageManifest manifest2 = PackageManifestHelper.DeserializeManifestFromBinary(bytesData2, null); //TODO 自行处理解密

            // 拷贝文件列表
            foreach (var bundle2 in manifest2.BundleList)
            {
                if (manifest1.TryGetPackageBundleByBundleName(bundle2.BundleName, out PackageBundle bundle1))
                {
                    if (bundle2.FileHash != bundle1.FileHash)
                    {
                        changeList.Add(bundle2);
                    }
                }
                else
                {
                    newList.Add(bundle2);
                }
            }

            // 按字母重新排序
            changeList.Sort((x, y) => string.Compare(x.BundleName, y.BundleName));
            newList.Sort((x, y) => string.Compare(x.BundleName, y.BundleName));

        }

        private bool BuildRes(BuildTarget buildTarget, string packageName)
        {
            if (IsForceRebuild)
            {
                // 删除平台目录            
                string platformDirectory = $"{BuildOutputRoot}/{packageName}/{buildTarget}";
                if (EditorTools.DeleteDirectory(platformDirectory))
                {
                    Log.Debug($"删除平台目录：{platformDirectory}");
                }
            }

            var packageSetting = SelectItem.GetPackageSetting(packageName);

            var buildOutputRoot = BuildOutputRoot;
            var temOutputRoot = TemBuildOutputRoot;
            var versionFileName = YooAssetConfiguration.GetPackageVersionFileName(packageName);
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
                Log.Warning("版本号未修改，系统将自动递增");
            }

            BuildParameters buildParameters = null;
            IBuildPipeline pipeline = null;
            var pipelineOption = (EBuildPipeline)Enum.Parse(typeof(EBuildPipeline), packageSetting.PiplineOption);
            switch (pipelineOption)
            {
                case EBuildPipeline.LegacyBuildPipeline:
                    buildParameters = new LegacyBuildParameters()
                    {
                        CompressOption = packageSetting.CompressOption,
                    };
                    pipeline = new LegacyBuildPipeline();
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
                    Log.Error("未知的构建模式");
                    return false;
            }
            buildParameters.BuildOutputRoot = temOutputRoot;
            buildParameters.BundledFileRoot = StreamingAssetsRoot;
            buildParameters.BuildPipeline = packageSetting.PiplineOption.ToString();
            buildParameters.BuildTarget = buildTarget;
            buildParameters.PackageName = packageName;
            buildParameters.PackageVersion = nowVersion;
            buildParameters.VerifyBuildingResult = true;
            buildParameters.EnableSharePackRule = true;
            buildParameters.FileNameStyle = packageSetting.NameStyleOption;
            buildParameters.BundledCopyOption = IsForceRebuild
                ? EBundledCopyOption.OnlyCopyByTags : EBundledCopyOption.None;
            buildParameters.BundledCopyParams = packageSetting.BuildTags;
            buildParameters.BundleEncryptor = CreateServicesInstance<IBundleEncryptor>(packageSetting.EncyptionClassName);
            buildParameters.ManifestEncryptor = CreateServicesInstance<IManifestEncryptor>(packageSetting.ManifestProcessServices);
            buildParameters.ManifestDecryptor = CreateServicesInstance<IManifestDecryptor>(packageSetting.ManifestRestoreServices);
            buildParameters.ClearBuildCacheFiles = IsForceRebuild; //增量构建时需要清理构建缓存，否则可能导致构建失败或构建速度变慢，
            buildParameters.UseAssetDependencyDB = SelectItem.IsUseDb; //使用资源依赖关系数据库，提升构建速度，



            var result = pipeline.Run(buildParameters, true);
            if (!result.Success)
            {
                var temVersion = FileUtility.ReadAllText(versionFile);
                if (temVersion == nowVersion)//构建失败了，但YooAsset却把版本号给修改了，这时需要把版本号改回去
                {
                    File.WriteAllText(versionFile, lastVersion, System.Text.Encoding.UTF8);
                }
                Log.Error($"{packageName}:构建资源失败");
                return false;
            }

            // 处理link.xml
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
                var temFile = EditorPathUtility.GetRegularPath(file);
                var index = temFile.LastIndexOf("/");
                var fileName = temFile.Substring(index + 1);
                File.Copy(file, Path.Combine(buildPathVersion, fileName), true);
            }

            var nowHashFileName = YooAssetConfiguration.GetPackageHashFileName(packageName, nowVersion);
            var nowBinaryFileName = YooAssetConfiguration.GetManifestBinaryFileName(packageName, nowVersion);
            var lastBinaryFileName = YooAssetConfiguration.GetManifestBinaryFileName(packageName, lastVersion);

            List<string> files = null;
            if (!string.IsNullOrEmpty(lastVersion))
            {
                files = new List<string>();
                var path1 = $"{buildPath}/{lastVersion}/{lastBinaryFileName}";
                var path2 = $"{temBuildPath}/{nowVersion}/{nowBinaryFileName}";
                List<PackageBundle> changedList = new List<PackageBundle>();
                List<PackageBundle> newList = new List<PackageBundle>();
                CompareManifest(path1, path2, changedList, newList);
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

                files.Add($"{temBuildPathVersion}/{versionFileName}");
                files.Add($"{temBuildPathVersion}/{nowBinaryFileName}");
                files.Add($"{temBuildPathVersion}/{nowHashFileName}");
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

            var reportFileName = YooAssetConfiguration.GetBuildReportFileName(packageName, nowVersion);
            foreach (var file in files)
            {
                var temFile = EditorPathUtility.GetRegularPath(file);
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

        #region 辅助方法
        // 辅助方法
        private List<string> GetBuildPackageNames()
        {
            List<string> result = new List<string>();
            foreach (var package in BundleCollectorSettingData.Setting.Packages)
            {
                result.Add(package.PackageName);
            }
            return result;
        }

        private T CreateServicesInstance<T>(string manifestClassName)
        {
            var manifestClassTypes = EditorTools.GetAssignableTypes(typeof(T));
            var classType = manifestClassTypes.Find(x => x.FullName.Equals(manifestClassName));
            if (classType != null)
                return (T)Activator.CreateInstance(classType);
            else
                return default(T);
        }

        #endregion
    }
}
