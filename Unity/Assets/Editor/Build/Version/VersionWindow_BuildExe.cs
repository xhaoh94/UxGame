using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public partial class VersionWindow
{
    private bool BuildExe(BuildTarget buildTarget)
    {
        if (IsExportExecutable)
        {
            var compileType = (CompileType)_compileType.value;
            BuildOptions options = BuildHelper.GetBuildOptions(buildTarget, compileType == CompileType.Development);

            var path = Path.Combine(SelectItem.ExePath, compileType.ToString(), buildTarget.ToString());
            BuildPlayerOptions buildPlayerOptions = BuildHelper.GetBuildPlayerOptions(buildTarget, options, path, "Game");
            Log.Debug("---------------------------------------->开始程序打包<---------------------------------------");
            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Log.Debug("---------------------------------------->打包失败<---------------------------------------");
                return false;
            }
            Log.Debug("---------------------------------------->完成程序打包<---------------------------------------");
            EditorUtility.RevealInFinder(Path.GetFullPath(path));
        }
        return true;
    }
}