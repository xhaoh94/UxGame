using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

public class BuildWebServerEditor
{

    [MenuItem("Tools/Build/资源服务器", false, 301)]
    public static void OpenFileServer()
    {
        new Command("hfs.exe", "../HFS/");
    }

}
