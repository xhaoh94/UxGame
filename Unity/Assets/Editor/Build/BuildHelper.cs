using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;

// [UnityEditor.InitializeOnLoad]
// public class ExcuteInEditorLoad
// {
//     /// <summary>
//     /// 在编辑器启动时，提前处理点啥，配置些工程需要的环境啥的。
//     /// </summary>
//     static ExcuteInEditorLoad()
//     {
//         UnityEditor.EditorApplication.delayCall += DoSomethingPrepare;
//     }
//
//     /// <summary>
//     /// 在编辑器启动时，提前处理点啥，配置些工程需要的环境啥的。
//     /// </summary>
//     static void DoSomethingPrepare()
//     {
//         var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(EditorBuildSettings.scenes[0].path);
//         EditorSceneManager.playModeStartScene = scene;
//     }
// }

public class BuildHelper
{
    public static BuildPlayerOptions GetBuildPlayerOptions(BuildTarget buildTarget, BuildOptions buildOptions, string exportPath, string name)
    {
        string ex = ".exe";
        switch (buildTarget)
        {
            case BuildTarget.Android:
                ex = ".apk";
                break;
        }
        if (!name.EndsWith(ex))
        {
            name += ex;
        }
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions()
        {
            scenes = new string[] { EditorBuildSettings.scenes[0].path },
            locationPathName = Path.Combine(exportPath, name),
            options = buildOptions,
            target = buildTarget,
            targetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget),
        };
        return buildPlayerOptions;
    }
}