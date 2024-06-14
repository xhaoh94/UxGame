using HybridCLR.Editor;
using HybridCLR.Editor.AOT;
using HybridCLR.Editor.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ux.Editor.HybridCLR
{
    public class HybridCLRCommand
    {
        private const string HotDir = "Assets/Data/Res/Code";
        private const string AotDir = "Assets/Resources/Code";
        private const string ComplileAOTTempPath = "./Release_Temp";

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            var dfs = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone).Split(';');
            var v = dfs.Contains("HOTFIX_CODE");
            if (v != SettingsUtil.Enable)
            {
                Log.Error("热更设置与热更宏对应不上，已把热更设置强制于热更宏同步");
                SettingsUtil.Enable = v;
            }
        }
        [MenuItem("HybridCLR/切换热更模式/开", false, 2000)]
        public static void OpenHotfixCode()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }
            if (EditorApplication.isCompiling)
            {
                EditorUtility.DisplayDialog("错误", "编译中，请稍后再尝试！", "ok");
                return;
            }
            var dfs = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone).Split(';');
            var symbols = dfs.ToList();
            if (symbols.Contains("HOTFIX_CODE"))
            {
                EditorUtility.DisplayDialog("提示", $"当前已是热更模式", "确定");
                return;
            }
            SettingsUtil.Enable = true;
            symbols.Add("HOTFIX_CODE");
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, string.Join(";", symbols));
        }

        [MenuItem("HybridCLR/切换热更模式/开", true, 2000)]
        public static bool ValidateOpenHotfixCode()
        {
            return !SettingsUtil.Enable;
        }

        [MenuItem("HybridCLR/切换热更模式/关", false, 2001)]
        public static void CloseHotfixCode()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }
            if (EditorApplication.isCompiling)
            {
                EditorUtility.DisplayDialog("错误", "编译中，请稍后再尝试！", "ok");
                return;
            }
            var dfs = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone).Split(';');
            //PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone, out var dfs);
            var symbols = dfs.ToList();
            if (!symbols.Contains("HOTFIX_CODE"))
            {
                EditorUtility.DisplayDialog("提示", $"当前已是非热更模式", "确定");
                return;
            }
            SettingsUtil.Enable = false;
            symbols.Remove("HOTFIX_CODE");
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, string.Join(";", symbols));
            //PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, symbols.ToArray());
        }
        [MenuItem("HybridCLR/切换热更模式/关", true, 2001)]
        public static bool ValidateCloseHotfixCode()
        {
            return SettingsUtil.Enable;
        }

        public static void ClearHOTDll()
        {
            _Clear(HotDir);
        }
        public static void ClearAOTDll()
        {
            _Clear(AotDir);
        }
        static void _Clear(string path)
        {
            if (!Directory.Exists(path))
            {
                return;
            }
            var directoryInfo = new DirectoryInfo(path);
            foreach (var file in directoryInfo.GetFiles())
            {
                file.Delete();
            }
        }


        /// <summary>
        /// 将AOT元数据DLL拷贝到资源打包目录
        /// </summary>
        /// <param name="target"></param>       
        public static void CopyAOTAssembliesToYooAssetPath(BuildTarget target)
        {
            string aotDllDir = SettingsUtil.GetAssembliesPostIl2CppStripDir(target);
            if (!Directory.Exists(AotDir))
            {
                Directory.CreateDirectory(AotDir);
            }
            var gs = SettingsUtil.HybridCLRSettings;
            var hotUpdateDllNames = SettingsUtil.HotUpdateAssemblyNamesExcludePreserved;
            List<string> codes = new List<string>();
            AssemblyReferenceDeepCollector collector = new AssemblyReferenceDeepCollector(MetaUtil.CreateHotUpdateAndAOTAssemblyResolver(target, hotUpdateDllNames), hotUpdateDllNames);
            var analyzer = new Analyzer(new Analyzer.Options
            {
                MaxIterationCount = Math.Min(20, gs.maxGenericReferenceIteration),
                Collector = collector,
            });

            analyzer.Run();
            var types = analyzer.AotGenericTypes.ToList();
            var methods = analyzer.AotGenericMethods.ToList();
            var modules = new HashSet<dnlib.DotNet.ModuleDef>(
                types.Select(t => t.Type.Module).Concat(methods.Select(m => m.Method.Module))).ToList();
            foreach (var module in modules)
            {
                codes.Add(module.Name);
            }

            foreach (var dll in codes)
            {
                var dllName = dll;
                if (!dllName.EndsWith(".dll"))
                {
                    dllName += ".dll";
                }
                string dllPath = $"{aotDllDir}/{dllName}";
                if (!File.Exists(dllPath))
                {
                    Debug.LogError($"添加AOT补充元数据dll:{dllPath} 时发生错误,文件不存在。裁剪后的AOT dll在BuildPlayer时才能生成，因此需要你先构建一次游戏App后再打包。");
                    continue;
                }
                string dllBytesPath = $"{AotDir}/{dllName}.bytes";
                File.Copy(dllPath, dllBytesPath, true);
                Log.Info($"复制{dllPath}到{AotDir}完成");
            }
            AssetDatabase.Refresh();
        }
        /// <summary>
        /// 将热更DLL拷贝到资源打包目录
        /// </summary>
        /// <param name="target"></param>

        public static void CopyHotUpdateAssembliesToYooAssetPath(BuildTarget target)
        {
            if (!Directory.Exists(HotDir))
            {
                Directory.CreateDirectory(HotDir);
            }
            string hotfixDllSrcDir = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target);
            foreach (var dll in SettingsUtil.HotUpdateAssemblyFilesExcludePreserved)
            {
                var dllName = dll;
                if (!dllName.EndsWith(".dll"))
                {
                    dllName += ".dll";
                }
                string dllPath = $"{hotfixDllSrcDir}/{dllName}";
                string dllBytesPath = $"{HotDir}/{dllName}.bytes";
                File.Copy(dllPath, dllBytesPath, true);
                Log.Info($"复制{dllPath}到{HotDir}完成");
            }
            AssetDatabase.Refresh();
        }


    }

}
