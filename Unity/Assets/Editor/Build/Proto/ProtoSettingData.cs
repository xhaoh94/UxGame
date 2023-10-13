using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;


[CreateAssetMenu(fileName = "ProtoSettingData", menuName = "Ux/PbTool/Create ProtoSetting")]
public class ProtoSettingData : ScriptableObject
{

    [CommandPathAttribute]
    [Command(false)]
    [InspectorLabel("PbTool")]
    public string PbTool = "../Proto/PbTool/PbTool.dll";

    [CommandPathAttribute]
    [Command("-config")]
    [InspectorLabel("����Ŀ¼")]
    public string Config = "../Proto/Config.json";   

    [CommandPathAttribute]
    [Command("-inpath")]
    [InspectorLabel("����Ŀ¼")]
    public string InPath = "../Proto/protofiles/protofile/client";

    [CommandPathAttribute]
    [Command("-outpath")]
    [InspectorLabel("��������Ŀ¼")]
    public string OutPath = "Assets/Hotfix/CodeGen/Proto";

    [Command("-type")]
    [InspectorLabel("Type")]
    public string Type = "csharp_pbnet";

    [Command("-namespace")]
    [InspectorLabel("�����ռ�")]
    public string NameSpace = "Ux.Pb";


    /// <summary>
    /// �洢�����ļ�
    /// </summary>
    public void SaveFile()
    {
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
        Debug.Log($"{nameof(ProtoSettingData)}.asset is saved!");
    }
    public static ProtoSettingData Setting;
    public string GetCommand()
    {
        //string line_end = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? " ^" : " \\";

        StringBuilder sb = new StringBuilder();

        var fields = GetType().GetFields();

        foreach (var field_info in fields)
        {
            var command = field_info.GetCustomAttribute<CommandAttribute>();

            if (command is null)
            {
                continue;
            }

            var value = field_info.GetValue(this)?.ToString();

            // ��ǰֵΪ�� ���� False, ���� None(Enum Ĭ��ֵ)
            // �����ѭ��
            if (string.IsNullOrEmpty(value) || string.Equals(value, "False") || string.Equals(value, "None"))
            {
                continue;
            }

            if (string.Equals(value, "True"))
            {
                value = string.Empty;
            }

            value = value.Replace(", ", ",");

            var isPath = field_info.GetCustomAttribute<CommandPathAttribute>();
            if (isPath != null)
            {
                value = Path.GetFullPath(value).Replace("\\", "/");
            }

            if (string.IsNullOrEmpty(command.option))
            {
                sb.Append($" {value} ");
            }
            else
            {
                sb.Append($" {command.option}={value} ");
            }


            //if (command.newLine)
            //{
            //    sb.Append($"{line_end} \n");
            //}
        }
        Log.Info(sb.ToString());
        return sb.ToString();
    }

    #region ��ʼ��   
    public static ProtoSettingData LoadConfig()
    {
        if (Setting == null)
        {
            Setting = SettingTools.GetSingletonAssets<ProtoSettingData>("Assets/Setting/Build/Proto");
        }
        return Setting;
    }
    public static void SaveConfig()
    {
        Setting?.SaveFile();
    }

    #endregion
}