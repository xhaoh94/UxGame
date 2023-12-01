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
            Log.Debug("---------------------------------------->��ʼ����DLL<---------------------------------------");
            HybridCLRCommand.ClearHOTDll();
            await UxEditor.Export(_tgCompileUI.value, _tgCompileConfig.value, _tgCompileProto.value, false);
            var compileType = (CompileType)_compileType.value;
            if (IsExportExecutable)
            {
                if (target != EditorUserBuildSettings.activeBuildTarget &&
                    !EditorUserBuildSettings.SwitchActiveBuildTarget(
                        BuildPipeline.GetBuildTargetGroup(target), target))
                {
                    Log.Debug("---------------------------------------->�л�����ƽ̨ʧ��<---------------------------------------");
                    return false;
                }
                HybridCLRCommand.ClearAOTDll();
                Log.Debug("---------------------------------------->ִ��HybridCLRԤ����<---------------------------------------");
                CompileDllCommand.CompileDll(target, compileType == CompileType.Development);
                Il2CppDefGeneratorCommand.GenerateIl2CppDef();

                // �⼸����������HotUpdateDlls
                LinkGeneratorCommand.GenerateLinkXml(target);

                // ���ɲü����aot dll
                StripAOTDllCommand.GenerateStripedAOTDlls(target);

                // �ŽӺ�������������AOT dll�����뱣֤�Ѿ�build��������AOT dll
                MethodBridgeGeneratorCommand.GenerateMethodBridge(target);
                ReversePInvokeWrapperGeneratorCommand.GenerateReversePInvokeWrapper(target);
                AOTReferenceGeneratorCommand.GenerateAOTGenericReference(target);
                //PrebuildCommand.GenerateAll();

                Log.Debug("---------------------------------------->��AOTԪ����Dll��������Դ���Ŀ¼<---------------------------------------");
                HybridCLRCommand.CopyAOTAssembliesToYooAssetPath(target);
            }
            else
            {
                Log.Debug("---------------------------------------->�����ȸ�DLL<---------------------------------------");
                CompileDllCommand.CompileDll(target, compileType == CompileType.Development);
            }

            Log.Debug("---------------------------------------->���ȸ�DLL��������Դ���Ŀ¼<---------------------------------------");
            HybridCLRCommand.CopyHotUpdateAssembliesToYooAssetPath(target);

            Log.Debug("---------------------------------------->��ɱ���DLL<---------------------------------------");
        }
        return true;
    }
}