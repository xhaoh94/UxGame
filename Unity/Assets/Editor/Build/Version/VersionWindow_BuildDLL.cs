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

                    // 这几个生成依赖HotUpdateDlls
                    LinkGeneratorCommand.GenerateLinkXml(target);

                    // 生成裁剪后的aot dll
                    StripAOTDllCommand.GenerateStripedAOTDlls(target);

                    // 桥接函数生成依赖于AOT dll，必须保证已经build过，生成AOT dll
                    MethodBridgeGeneratorCommand.GenerateMethodBridgeAndReversePInvokeWrapper(target);
                    AOTReferenceGeneratorCommand.GenerateAOTGenericReference(target);
                    //PrebuildCommand.GenerateAll();

                    Log.Debug("---------------------------------------->将AOT元数据Dll拷贝到资源打包目录<---------------------------------------");
                    HybridCLRCommand.CopyAOTAssembliesToYooAssetPath(target);
                }
                else
                {
                    Log.Debug("---------------------------------------->生成热更DLL<---------------------------------------");
                    CompileDllCommand.CompileDll(target, compile == CompileType.Development);
                }

                Log.Debug("---------------------------------------->将热更DLL拷贝到资源打包目录<---------------------------------------");
                HybridCLRCommand.CopyHotUpdateAssembliesToYooAssetPath(target);

                Log.Debug("---------------------------------------->完成编译DLL<---------------------------------------");
            }
            return true;
        }
    }

}
