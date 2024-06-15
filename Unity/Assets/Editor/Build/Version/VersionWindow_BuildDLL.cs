using Cysharp.Threading.Tasks;
using HybridCLR.Editor.Commands;
using UnityEditor;
using Ux.Editor.HybridCLR;
using InstallerController = HybridCLR.Editor.Installer.InstallerController;
namespace Ux.Editor.Build.Version
{
    public partial class VersionWindow
    {
        private async UniTask<bool> BuildDLL(BuildTarget target)
        {
            if (tgCompileDLL.value)
            {
                Log.Debug("---------------------------------------->��ʼ����DLL<---------------------------------------");
                HybridCLRCommand.ClearHOTDll();
                await UxEditor.Export(tgCompileUI.value, tgCompileConfig.value, tgCompileProto.value, false);
                var compile = (CompileType)compileType.value;
                if (IsExportExecutable && tgCompileAot.value)
                {
                    if (target != EditorUserBuildSettings.activeBuildTarget &&
                        !EditorUserBuildSettings.SwitchActiveBuildTarget(
                            BuildPipeline.GetBuildTargetGroup(target), target))
                    {
                        Log.Debug("---------------------------------------->�л�����ƽ̨ʧ��<---------------------------------------");
                        return false;
                    }

                    var installer = new InstallerController();
                    if (!installer.HasInstalledHybridCLR())
                    {
                        Log.Debug("---------------------------------------->���Ȱ�װHybridCLR<---------------------------------------");
                        return false;
                    }

                    HybridCLRCommand.ClearAOTDll();
                    Log.Debug("---------------------------------------->ִ��HybridCLRԤ����<---------------------------------------");
                    CompileDllCommand.CompileDll(target, compile == CompileType.Development);
                    Il2CppDefGeneratorCommand.GenerateIl2CppDef();

                    // �⼸����������HotUpdateDlls
                    LinkGeneratorCommand.GenerateLinkXml(target);

                    // ���ɲü����aot dll
                    StripAOTDllCommand.GenerateStripedAOTDlls(target);

                    // �ŽӺ�������������AOT dll�����뱣֤�Ѿ�build��������AOT dll
                    MethodBridgeGeneratorCommand.GenerateMethodBridgeAndReversePInvokeWrapper(target);
                    AOTReferenceGeneratorCommand.GenerateAOTGenericReference(target);
                    //PrebuildCommand.GenerateAll();

                    Log.Debug("---------------------------------------->��AOTԪ����Dll��������Դ���Ŀ¼<---------------------------------------");
                    HybridCLRCommand.CopyAOTAssembliesToYooAssetPath(target);
                }
                else
                {
                    Log.Debug("---------------------------------------->�����ȸ�DLL<---------------------------------------");
                    CompileDllCommand.CompileDll(target, compile == CompileType.Development);
                }

                Log.Debug("---------------------------------------->���ȸ�DLL��������Դ���Ŀ¼<---------------------------------------");
                HybridCLRCommand.CopyHotUpdateAssembliesToYooAssetPath(target);

                Log.Debug("---------------------------------------->��ɱ���DLL<---------------------------------------");
            }
            return true;
        }
    }

}
