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
                Log.Debug("---------------------------------------->开始编译DLL<---------------------------------------");
                HybridCLRCommand.ClearHOTDll();
                await UxEditor.Export(tgCompileUI.value, tgCompileConfig.value, tgCompileProto.value, false);
                var compile = (CompileType)compileType.value;
                if (IsExportExecutable && tgCompileAot.value)
                {
                    if (target != EditorUserBuildSettings.activeBuildTarget &&
                        !EditorUserBuildSettings.SwitchActiveBuildTarget(
                            BuildPipeline.GetBuildTargetGroup(target), target))
                    {
                        Log.Debug("---------------------------------------->切换编译平台失败<---------------------------------------");
                        return false;
                    }

                    var installer = new InstallerController();
                    if (!installer.HasInstalledHybridCLR())
                    {
                        Log.Debug("---------------------------------------->请先安装HybridCLR<---------------------------------------");
                        return false;
                    }

                    HybridCLRCommand.ClearAOTDll();
                    Log.Debug("---------------------------------------->执行HybridCLR预编译<---------------------------------------");
                    CompileDllCommand.CompileDll(target, compile == CompileType.Development);
                    Il2CppDefGeneratorCommand.GenerateIl2CppDef();

                    // 这几个dll在HotUpdateDlls
                    LinkGeneratorCommand.GenerateLinkXml(target);

                    // 生成裁剪后的aot dll
                    StripAOTDllCommand.GenerateStripedAOTDlls(target);

                    // 补充泛型约束到AOT dll，并保证已经build过的泛型AOT dll
                    MethodBridgeGeneratorCommand.GenerateMethodBridgeAndReversePInvokeWrapper(target);
                    AOTReferenceGeneratorCommand.GenerateAOTGenericReference(target);
                    //PrebuildCommand.GenerateAll();

                    Log.Debug("---------------------------------------->将AOT元数据Dll复制到资源目录<---------------------------------------");
                    HybridCLRCommand.CopyAOTAssembliesToYooAssetPath(target);
                }
                else
                {
                    Log.Debug("---------------------------------------->编译热更新DLL<---------------------------------------");
                    CompileDllCommand.CompileDll(target, compile == CompileType.Development);
                }

                Log.Debug("---------------------------------------->将热更新Dll复制到资源目录<---------------------------------------");
                HybridCLRCommand.CopyHotUpdateAssembliesToYooAssetPath(target);

                Log.Debug("---------------------------------------->完成编译DLL<---------------------------------------");
            }
            return true;
        }
    }

}
