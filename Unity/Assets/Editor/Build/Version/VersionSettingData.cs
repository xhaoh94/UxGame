using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using YooAsset.Editor;

[CreateAssetMenu(fileName = "VersionSettingData", menuName = "Ux/Build/Create VersionSetting")]
public class VersionSettingData : ScriptableObject
{
    public List<BuildExportSetting> ExportSettings = new List<BuildExportSetting>();

    /// <summary>
    /// 存储配置文件
    /// </summary>
    public void SaveFile()
    {
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
    public bool IsCompileDLL = true;
    public bool IsCopyTo = false;
    public string BundlePath = "./Bundles";
    public string ExePath = "./Release";
    public string CopyPath = "./CDN";
    public bool IsClearSandBox = true;
    public bool IsExportExecutable = true;
    public List<BuildPackageSetting> PackageBuilds = new List<BuildPackageSetting>();
    public string ResVersion = string.Empty;

    public BuildPackageSetting GetPackageSetting(string pkgName)
    {
        var setting = PackageBuilds.Find(x => x.PackageName == pkgName);
        if (setting == null)
        {
            setting = new BuildPackageSetting();
            setting.PackageName = pkgName;
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
    public string PackageName = string.Empty;
    public string EncyptionClassName = string.Empty;
    //public string SharedPackRule = string.Empty;
    public ECompressOption CompressOption = ECompressOption.LZ4;
    public EBuildPipeline PiplineOption = EBuildPipeline.ScriptableBuildPipeline;
    public EFileNameStyle NameStyleOption = EFileNameStyle.HashName;
    public string BuildTags = "builtin";

}