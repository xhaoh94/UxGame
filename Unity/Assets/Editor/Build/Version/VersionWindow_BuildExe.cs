using System.IO;
using UnityEditor;
namespace Ux.Editor.Build.Version
{
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
                Log.Debug("---------------------------------------->��ʼ������<---------------------------------------");
                var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
                if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                {
                    Log.Debug("---------------------------------------->���ʧ��<---------------------------------------");
                    return false;
                }
                Log.Debug("---------------------------------------->��ɳ�����<---------------------------------------");
                EditorUtility.RevealInFinder(Path.GetFullPath(path));
            }
            return true;
        }
    }
}
