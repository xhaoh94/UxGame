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
    public PlatformType CurPlatform = PlatformType.Win64;
    public List<BuildVersionPlatform> PlatformMap = new List<BuildVersionPlatform>();
    public BuildVersionPlatform PlatformConfig => GetPlatformConf(CurPlatform);
    BuildVersionPlatform GetPlatformConf(PlatformType platform)
    {
        var conf = PlatformMap.Find(x => x.PlatformType == platform);
        if (conf == null)
        {
            conf = new BuildVersionPlatform();
            conf.PlatformType = platform;
            PlatformMap.Add(conf);
        }
        return conf;
    }
    public void Clear()
    {
        CurPlatform = PlatformType.Win64;
        PlatformMap.Clear();
    }
    public BuildExportSetting Copy()
    {
        var copy = new BuildExportSetting();
        copy.CurPlatform = CurPlatform;
        copy.PlatformMap = new List<BuildVersionPlatform>();
        foreach (var platform in PlatformMap)
        {
            var copyPlatform = new BuildVersionPlatform();
            copyPlatform.CompileType = platform.CompileType;
            copyPlatform.PlatformType = platform.PlatformType;
            copyPlatform.IsCompileDLL = platform.IsCompileDLL;
            copyPlatform.EncyptionClassName = platform.EncyptionClassName;
            copyPlatform.CompressOption = platform.CompressOption;
            copyPlatform.PiplineOption = platform.PiplineOption;
            copyPlatform.NameStyleOption = platform.NameStyleOption;
            copyPlatform.BuildTags = platform.BuildTags;
            copyPlatform.IsCopyTo = platform.IsCopyTo;
            copyPlatform.ExePath = platform.ExePath;
            copyPlatform.CopyPath = platform.CopyPath;
            copyPlatform.ResVersion = platform.ResVersion;
            copy.PlatformMap.Add(copyPlatform);
        }
        return copy;
    }
}
[Serializable]
public class BuildVersionPlatform
{
    public CompileType CompileType = CompileType.Development;
    public PlatformType PlatformType = PlatformType.Win64;
    public bool IsCompileDLL = true;
    public string EncyptionClassName = string.Empty;
    public string SharedPackRule = string.Empty;
    public ECompressOption CompressOption = ECompressOption.LZ4;
    public EBuildPipeline PiplineOption = EBuildPipeline.ScriptableBuildPipeline;
    public EOutputNameStyle NameStyleOption = EOutputNameStyle.HashName;
    public bool IsExportExecutable = true;
    public string ResVersion = string.Empty;
    public string BuildTags = "builtin";
    public bool IsCopyTo = false;
    public string BundlePath = "./Bundles";
    public string ExePath = "./Release";
    public string CopyPath = "./CDN";
    public bool IsClearSandBox = true;
}