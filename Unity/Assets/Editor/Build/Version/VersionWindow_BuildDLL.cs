using Cysharp.Threading.Tasks;
using HybridCLR.Commands;
using HybridCLR.Editor.Commands;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public partial class VersionWindow
{
    private async UniTask<bool> BuildDLL(BuildTarget target)
    {
        if (_tgCompileDLL.value)
        {
            Log.Debug("---------------------------------------->开始编译DLL<---------------------------------------");
            HybridCLRCommand.ClearHOTDll();
            await UxEditor.Export(_tgCompileUI.value, _tgCompileConfig.value, _tgCompileProto.value, false);
            var compileType = (CompileType)_compileType.value;
            if (IsExportExecutable)
            {
                if (target != EditorUserBuildSettings.activeBuildTarget &&
                    !EditorUserBuildSettings.SwitchActiveBuildTarget(
                        BuildPipeline.GetBuildTargetGroup(target), target))
                {
                    Log.Debug("---------------------------------------->切换编译平台失败<---------------------------------------");
                    return false;
                }
                HybridCLRCommand.ClearAOTDll();
                Log.Debug("---------------------------------------->执行HybridCLR预编译<---------------------------------------");
                CompileDllCommand.CompileDll(target, compileType == CompileType.Development);
                Il2CppDefGeneratorCommand.GenerateIl2CppDef();

                // 这几个生成依赖HotUpdateDlls
                LinkGeneratorCommand.GenerateLinkXml(target);

                // 生成裁剪后的aot dll
                StripAOTDllCommand.GenerateStripedAOTDlls(target);

                // 桥接函数生成依赖于AOT dll，必须保证已经build过，生成AOT dll
                MethodBridgeGeneratorCommand.GenerateMethodBridge(target);
                ReversePInvokeWrapperGeneratorCommand.GenerateReversePInvokeWrapper(target);
                AOTReferenceGeneratorCommand.GenerateAOTGenericReference(target);
                //PrebuildCommand.GenerateAll();

                Log.Debug("---------------------------------------->将AOT元数据Dll拷贝到资源打包目录<---------------------------------------");
                HybridCLRCommand.CopyAOTAssembliesToYooAssetPath(target);
            }
            else
            {
                Log.Debug("---------------------------------------->生成热更DLL<---------------------------------------");
                CompileDllCommand.CompileDll(target, compileType == CompileType.Development);
            }

            Log.Debug("---------------------------------------->将热更DLL拷贝到资源打包目录<---------------------------------------");
            HybridCLRCommand.CopyHotUpdateAssembliesToYooAssetPath(target);

            Log.Debug("---------------------------------------->完成编译DLL<---------------------------------------");
        }
        return true;
    }
}
