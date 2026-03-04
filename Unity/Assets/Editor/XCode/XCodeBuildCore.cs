#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.iOS.Xcode;

// ========================================================
// 核心构建上下文：解决 Plist 和 Project 读写冲突
// ========================================================
public class BuildContext
{
    public string PathToBuiltProject { get; }
    public PBXProject Project { get; }
    public string ProjectPath { get; }
    public string MainTarget { get; }
    public string FrameworkTarget { get; }
    public PlistDocument InfoPlist { get; }
    public string InfoPlistPath { get; }

    public BuildContext(string pathToBuiltProject)
    {
        PathToBuiltProject = pathToBuiltProject;
        ProjectPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
        Project = new PBXProject();
        Project.ReadFromFile(ProjectPath);

        MainTarget = Project.GetUnityMainTargetGuid();
        FrameworkTarget = Project.GetUnityFrameworkTargetGuid();

        InfoPlistPath = Path.Combine(pathToBuiltProject, "Info.plist");
        InfoPlist = new PlistDocument();
        InfoPlist.ReadFromFile(InfoPlistPath);
    }

    public void Save()
    {
        Project.WriteToFile(ProjectPath);
        InfoPlist.WriteToFile(InfoPlistPath);
    }
}

public interface IXCodeProcessor
{
    int Priority { get; }
    void Process(BuildContext context);
}
#endif
