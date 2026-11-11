using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 项目自用的编辑器小工具。YooAsset v3 删掉了自带的 EditorTools，这里补回项目实际用到的几个方法，
/// 避免改动大量调用点。
/// </summary>
public static class EditorTools
{
    /// <summary>删除目录（不存在返回 false）。</summary>
    public static bool DeleteDirectory(string directory)
    {
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            return false;
        Directory.Delete(directory, true);
        return true;
    }

    /// <summary>清空目录内容，保留目录本身。</summary>
    public static void ClearFolder(string folder)
    {
        if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            return;
        foreach (var file in Directory.GetFiles(folder))
            File.Delete(file);
        foreach (var dir in Directory.GetDirectories(folder))
            Directory.Delete(dir, true);
    }

    /// <summary>收集所有可赋值给指定类型的实现类（跳过抽象类与接口）。</summary>
    /// <summary>
    /// 按 baseType 缓存的可赋值类型列表。
    /// 全程序集扫描 + LINQ 去重的成本在万级类型下是几百毫秒，
    /// 而结果在编辑器域重载（重新编译）前不会变，所以缓存一次即可。
    /// 返回的 List 只读，调用方请不要修改。
    /// </summary>
    private static readonly Dictionary<Type, List<Type>> _assignableTypeCache = new();

    public static List<Type> GetAssignableTypes(Type baseType)
    {
        if (baseType == null) return new List<Type>();
        if (_assignableTypeCache.TryGetValue(baseType, out var cached)) return cached;

        var result = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(GetTypesSafe)
            .Where(t => t != null && t.IsClass && !t.IsAbstract && baseType.IsAssignableFrom(t))
            .Distinct().ToList();

        _assignableTypeCache[baseType] = result;
        return result;
    }

    static IEnumerable<Type> GetTypesSafe(Assembly assembly)
    {
        try { return assembly.GetTypes(); }
        catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null); }
    }

    /// <summary>当前有未保存的场景修改。</summary>
    public static bool HasDirtyScenes()
    {
        for (int i = 0; i < EditorSceneManager.sceneCount; i++)
        {
            var scene = EditorSceneManager.GetSceneAt(i);
            if (scene.isDirty)
                return true;
        }
        return false;
    }

    /// <summary>清空 Console。</summary>
    public static void ClearUnityConsole()
    {
        var logEntries = Type.GetType("UnityEditor.LogEntries, UnityEditor");
        logEntries?.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public)?.Invoke(null, null);
    }

    /// <summary>把焦点切到 Console 窗口。</summary>
    public static void FocusUnityConsoleWindow()
    {
        var consoleType = Type.GetType("UnityEditor.ConsoleWindow, UnityEditor");
        var window = consoleType?.GetField("ms_ConsoleWindow", BindingFlags.Static | BindingFlags.NonPublic)
            ?.GetValue(null) as EditorWindow;
        if (window != null)
            window.Focus();
    }
}
