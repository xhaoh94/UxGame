#if UNITY_IOS
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

public static class XCodePostProcessBuild
{
    [PostProcessBuild(int.MaxValue)]
    public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.iOS)
        {
            Debug.LogWarning("Target is not iOS. XCodePostProcessBuild will not run");
            return;
        }

        Debug.Log("[XCodeBuild] Starting modular post-process flow...");
        var context = new BuildContext(pathToBuiltProject);

        ApplyGlobalSettings(context);

        foreach (var processor in GetActiveProcessors())
        {
            Debug.Log($"[XCodeBuild] Processing: {processor.GetType().Name}");
            processor.Process(context);
        }

        context.Save();
        Debug.Log("[XCodeBuild] Post-process complete successfully.");
    }

    private static void ApplyGlobalSettings(BuildContext context)
    {
        // Bitcode & Search Paths
        context.Project.SetBuildProperty(context.MainTarget, "ENABLE_BITCODE", "NO");
        context.Project.SetBuildProperty(context.FrameworkTarget, "ENABLE_BITCODE", "NO");
        context.Project.SetBuildProperty(context.FrameworkTarget, "FRAMEWORK_SEARCH_PATHS", "$(inherited)");
        context.Project.AddBuildProperty(context.FrameworkTarget, "FRAMEWORK_SEARCH_PATHS", "$(PROJECT_DIR)/Frameworks");
        context.Project.AddBuildProperty(context.FrameworkTarget, "FRAMEWORK_SEARCH_PATHS", "$(PROJECT_DIR)/SDK");
        context.Project.AddBuildProperty(context.FrameworkTarget, "LIBRARY_SEARCH_PATHS", "$(PROJECT_DIR)/SDK");

        // Linker Flags
        context.Project.AddBuildProperty(context.MainTarget, "OTHER_LDFLAGS", "-ObjC");
        context.Project.AddBuildProperty(context.FrameworkTarget, "OTHER_LDFLAGS", "-ObjC");
    }

    private static List<IXCodeProcessor> GetActiveProcessors()
    {
        var list = new List<IXCodeProcessor>();
        var interfaceType = typeof(IXCodeProcessor);
        var allTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(s=>s.GetTypes()).where(p = > interfaceType.IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract;
        foreach(var type in allTypes)
        {
            var processor = Activator.CreateInstance(type) as IXCodeProcessor;
            list.Add(processor);
        }       
        list.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        return list;
    }
}
#endif
