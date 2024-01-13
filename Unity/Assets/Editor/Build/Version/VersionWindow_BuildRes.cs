using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using YooAsset;
using YooAsset.Editor;

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

    void OnClearClick()
    {
        if (EditorUtility.DisplayDialog("提示", $"是否重置构建！\n重置会删除所有构建相关目录！", "确定", "取消"))
        {
            if (Directory.Exists(SelectItem.ExePath))
            {
                Directory.Delete(SelectItem.ExePath, true);
                Log.Debug($"删除构建目录：{SelectItem.ExePath}");
            }
            if (Directory.Exists(SelectItem.CopyPath))
            {
                Directory.Delete(SelectItem.CopyPath, true);
                Log.Debug($"删除拷贝目录：{SelectItem.CopyPath}");
            }

            if (EditorTools.DeleteDirectory(SandboxRoot))
            {
                Log.Debug($"删除沙盒缓存：{SandboxRoot}");
            }

            // 删除平台总目录                        
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
            Log.Debug("重置成功");
        }
    }

    private async UniTask<bool> BuildRes(BuildTarget buildTarget)
    {
        var packageValue = _buildPackage.value;
        if (packageValue == 0)
        {
            Log.Error("没有选中要构建的资源包");
            return false;
        }

        if (IsForceRebuild)
        {
            EditorTools.ClearFolder(StreamingAssetsRoot);

            if (_tgClearSandBox.value)
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
            var array = Convert.ToString(_buildPackage.value, 2).ToCharArray().Reverse();
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
                Log.Debug($"---------------------------------------->{packageName}:收集着色器变种<---------------------------------------");
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
                    Log.Debug("------------------------------------>生成YooAsset 着色器配置<------------------------------");
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
                            group.GroupDesc = "着色器";
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
            Log.Debug("---------------------------------------->开始资源打包<---------------------------------------");
            foreach (var package in packages)
            {
                Log.Debug($"---------------------------------------->{package}:开始构建资源包<---------------------------------------");
                if (!BuildRes(buildTarget, package)) return false;
            }
            Log.Debug("---------------------------------------->完成资源打包<---------------------------------------");
        }
        return true;
    }

    private bool BuildRes(BuildTarget buildTarget, string packageName)
    {
        if (IsForceRebuild)
        {
            // 删除平台总目录            
            string platformDirectory = $"{BuildOutputRoot}/{packageName}/{buildTarget}";
            if (EditorTools.DeleteDirectory(platformDirectory))
            {
                Log.Debug($"删除平台总目录：{platformDirectory}");
            }
        }

        var packageSetting = SelectItem.GetPackageSetting(packageName);

        var buildOutputRoot = BuildOutputRoot;
        var temOutputRoot = TemBuildOutputRoot;
        var versionFileName = YooAssetSettingsData.GetPackageVersionFileName(packageName);
        var outputDir = "Output";
        var buildPath = $"{buildOutputRoot}/{buildTarget}/{packageName}";
        var temBuildPath = $"{temOutputRoot}/{buildTarget}/{packageName}";
        var nowVersion = _txtVersion.value;
        var lastVersion = string.Empty;
        var versionFile = $"{buildPath}/{outputDir}/{versionFileName}";
        if (File.Exists(versionFile))
        {
            lastVersion = FileUtility.ReadAllText(versionFile);
        }

        if (lastVersion == nowVersion)
        {
            nowVersion = AddVersion(lastVersion);
            _txtVersion.SetValueWithoutNotify(nowVersion);
            Log.Warning("版本号无更新，将进行自动更新！");
        }

        BuildParameters buildParameters = null;
        IBuildPipeline pipeline = null;

        switch (packageSetting.PiplineOption)
        {
            case EBuildPipeline.BuiltinBuildPipeline:
                buildParameters = new BuiltinBuildParameters()
                {
                    BuildMode = IsForceRebuild ? EBuildMode.ForceRebuild : EBuildMode.IncrementalBuild,
                    CompressOption = packageSetting.CompressOption,

                };
                pipeline = new BuiltinBuildPipeline();
                break;
            case EBuildPipeline.ScriptableBuildPipeline:
                buildParameters = new ScriptableBuildParameters()
                {
                    BuildMode = EBuildMode.IncrementalBuild,//SBP构建只能用热更模式
                    CompressOption = packageSetting.CompressOption,
                };
                pipeline = new ScriptableBuildPipeline();
                break;
            case EBuildPipeline.RawFileBuildPipeline:
                buildParameters = new RawFileBuildParameters()
                {
                    BuildMode = EBuildMode.ForceRebuild,//RawFile构建只能用强更模式                    
                };
                pipeline = new RawFileBuildPipeline();
                break;
            default:
                Log.Error("未知的构建模式");
                return false;
        }

        buildParameters.BuildOutputRoot = temOutputRoot;
        buildParameters.BuildinFileRoot = StreamingAssetsRoot;
        buildParameters.BuildPipeline = packageSetting.PiplineOption.ToString();
        buildParameters.BuildTarget = buildTarget;
        buildParameters.PackageName = packageName;
        buildParameters.PackageVersion = nowVersion;
        buildParameters.VerifyBuildingResult = true;
        //buildParameters.SharedPackRule = CreateSharedPackRuleInstance();        
        buildParameters.FileNameStyle = packageSetting.NameStyleOption;
        buildParameters.BuildinFileCopyOption = IsForceRebuild
            ? EBuildinFileCopyOption.OnlyCopyByTags : EBuildinFileCopyOption.None;
        buildParameters.BuildinFileCopyParams = packageSetting.BuildTags;
        buildParameters.EncryptionServices = CreateEncryptionServicesInstance(packageSetting.EncyptionClassName);



        var result = pipeline.Run(buildParameters, true);
        if (!result.Success)
        {
            var temVersion = FileUtility.ReadAllText(versionFile);
            if (temVersion == nowVersion)//构建失败了，但是YooAsset却把版本号给修改了,这时候需要把版本号回滚一下
            {
                File.WriteAllText(versionFile, lastVersion, System.Text.Encoding.UTF8);
            }
            Log.Error($"{packageName}:构建资源包失败");
            return false;
        }

        //不拷贝link.xml
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
        if (_tgCopy.value && !string.IsNullOrEmpty(SelectItem.CopyPath))
        {
            tPath = $"{SelectItem.CopyPath}/{buildTarget}";
            if (!Directory.Exists(tPath))
            {
                Directory.CreateDirectory(tPath);
            }
            desPathList.Add(tPath);
        }

        var reportFileName = YooAssetSettingsData.GetReportFileName(packageName, nowVersion);
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

    #region 构建包裹相关
    // 构建包裹相关
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