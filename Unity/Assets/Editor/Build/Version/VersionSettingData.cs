using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using YooAsset;
using YooAsset.Editor;
namespace Ux.Editor.Build.Version
{
    [CreateAssetMenu(fileName = "VersionSettingData", menuName = "Ux/Build/Create VersionSetting")]
    public class VersionSettingData : ScriptableObject
    {
        public List<BuildExportSetting> ExportSettings = new List<BuildExportSetting>();

        /// <summary>
        /// 存储配置文件
        /// </summary>
        public void SaveFile()
        {
            foreach (var setting in ExportSettings)
            {
                foreach (var pb in setting.PackageBuilds)
                {
                    pb.New = false;
                }
            }
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            Debug.Log($"{nameof(VersionSettingData)}.asset is saved!");
        }
    }

    [Serializable]
    public class BuildExportSetting
    {
        public string Name;
        public CompileType CompileType = CompileType.Development;
        public PlatformType PlatformType = PlatformType.Win64;
        public bool IsCopyTo = false;
        public string BundlePath = "./Bundles";
        public string ExePath = "./bin";
        public string CopyPath = "./CDN";
        public bool IsUseDb = true;
        public bool IsClearSandBox = true;
        public bool IsExportExecutable = true;
        public string ResVersion = string.Empty;
        public List<BuildPackageSetting> PackageBuilds = new List<BuildPackageSetting>();

        public BuildPackageSetting GetPackageSetting(string pkgName)
        {
            var setting = PackageBuilds.Find(x => x.PackageName == pkgName);
            if (setting == null)
            {
                setting = new BuildPackageSetting();
                setting.PackageName = pkgName;
                setting.PiplineOption = YooAsset.Editor.AssetBundleBuilderSetting.GetPackageBuildPipeline(pkgName);
                setting.CompressOption = YooAsset.Editor.AssetBundleBuilderSetting.GetPackageCompressOption(pkgName, setting.PiplineOption);
                setting.NameStyleOption = YooAsset.Editor.AssetBundleBuilderSetting.GetPackageFileNameStyle(pkgName, setting.PiplineOption);
                setting.EncyptionClassName = YooAsset.Editor.AssetBundleBuilderSetting.GetPackageEncyptionServicesClassName(pkgName, setting.PiplineOption);
                PackageBuilds.Add(setting);
            }
            return setting;
        }

        public void Clear()
        {
            PackageBuilds.Clear();
        }
    }
    [Serializable]
    public class BuildPackageSetting
    {
        public bool New = true;
        public bool IsCollectShaderVariant = false;
        public string PackageName = string.Empty;
        public string EncyptionClassName = string.Empty;
        public string ManifestClassName = string.Empty;
        public ECompressOption CompressOption = ECompressOption.LZ4;
        public string PiplineOption = EBuildPipeline.ScriptableBuildPipeline.ToString();
        public EFileNameStyle NameStyleOption = EFileNameStyle.HashName;
        public string BuildTags = "builtin";

    }
}

