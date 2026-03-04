#if UNITY_IOS
using System.IO;
using UnityEditor.iOS.Xcode;
using UnityEditor.iOS.Xcode.Extensions;
using UnityEngine;

public static class BuildUtils
{
    public static void AddSystemFrameworks(BuildContext context, string[] frameworks, bool weak = false)
    {
        foreach (var fm in frameworks)
            if (!context.Project.ContainsFramework(context.FrameworkTarget, fm))
                context.Project.AddFrameworkToProject(context.FrameworkTarget, fm, weak);
    }

    public static void AddSystemLibraries(BuildContext context, string[] libs)
    {
        foreach (var lib in libs)
        {
            string guid = context.Project.AddFile("usr/lib/" + lib, "Libraries/" + lib, PBXSourceTree.Sdk);
            context.Project.AddFileToBuild(context.FrameworkTarget, guid);
        }
    }

    public static string AddFileSafely(BuildContext context, string srcPath, string projectPath, PBXSourceTree tree = PBXSourceTree.Source)
    {
        return context.Project.AddFile(srcPath, projectPath, tree);
    }

    public static void CopyAndAddDirectory(BuildContext context, string assetSubPath, string xcodeSubPath, string targetGuid, bool recursiveDir = false, bool curDirFiles = false)
    {
        string srcPath = Path.Combine(Application.dataPath, assetSubPath);
        string destPath = Path.Combine(context.PathToBuiltProject, xcodeSubPath);

        if (!Directory.Exists(srcPath) && !File.Exists(srcPath)) return;

        if (File.Exists(srcPath)) {
            Directory.CreateDirectory(Path.GetDirectoryName(destPath));
            File.Copy(srcPath, destPath, true);
        } else {
            RecursiveCopy(srcPath, destPath);
        }

        if (!recursiveDir && !curDirFiles) return;
        
        DirectoryInfo info = new DirectoryInfo(destPath);
        if (!info.Exists) return;

        if (recursiveDir) {
            foreach (var dir in info.GetDirectories()) {
                string relPath = Path.Combine(xcodeSubPath, dir.Name);
                if (!context.Project.ContainsFileByProjectPath(relPath)) {
                    string guid = context.Project.AddFile(relPath, relPath, PBXSourceTree.Source);
                    context.Project.AddFileToBuild(targetGuid, guid);
                }
            }
        }
        if (curDirFiles) {
            foreach (var file in info.GetFiles()) {
                if (file.Name.EndsWith(".meta") || file.Name.EndsWith(".DS_Store")) continue;
                string relPath = Path.Combine(xcodeSubPath, file.Name);
                if (!context.Project.ContainsFileByProjectPath(relPath)) {
                    string guid = context.Project.AddFile(relPath, relPath, PBXSourceTree.Source);
                    context.Project.AddFileToBuild(targetGuid, guid);
                }
            }
        }
    }

    private static void RecursiveCopy(string src, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (var f in Directory.GetFiles(src)) {
            if (f.EndsWith(".meta")) continue;
            File.Copy(f, Path.Combine(dest, Path.GetFileName(f)), true);
        }
        foreach (var d in Directory.GetDirectories(src)) {
            RecursiveCopy(d, Path.Combine(dest, Path.GetFileName(d)));
        }
    }

    public static void AddUrlScheme(PlistElementArray array, string scheme, string name = null, string role = "Editor")
    {
        var dict = array.AddDict();
        dict.SetString("CFBundleTypeRole", role);
        if (!string.IsNullOrEmpty(name)) dict.SetString("CFBundleURLName", name);
        dict.CreateArray("CFBundleURLSchemes").AddString(scheme);
    }

    public static void AddLSApplicationQueriesSchemes(PlistDocument plist, string[] schemes)
    {
        var root = plist.root;
        PlistElementArray array = root["LSApplicationQueriesSchemes"] as PlistElementArray ?? root.CreateArray("LSApplicationQueriesSchemes");
        foreach(var s in schemes) array.AddString(s);
    }
}
#endif
