using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class UxEditor
{
    [MenuItem("UxGame/切换到/Boot", false, 1000)]
    public static void ChangeBoot()
    {
        EditorSceneManager.OpenScene(EditorBuildSettings.scenes[0].path, OpenSceneMode.Single);        
    }
    [MenuItem("UxGame/切换到/Boot并启动", false, 1001)]
    public static void ChangeBootRun()
    {
        ChangeBoot();
        UnityEditor.EditorApplication.isPlaying = true;
    }
}
