using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using UnityEditor;
using UnityEngine;


[CreateAssetMenu(fileName = "ConfigSettingData", menuName = "Ux/Config/Create ConfigSetting")]
public class ConfigSettingData : ScriptableObject
{

    [CommandPathAttribute]
    [Command(false)]   
    public string DllFile = "../Luban/Tools/Luban.ClientServer/Luban.ClientServer.dll";

    [Command("--job")]
    [HideInInspector]
    public string Job = "cfg --";

    [CommandPathAttribute]
    [Command("--define_file")]    
    public string DefineFile = "../Luban/Defines/__root__.xml";

    [CommandPathAttribute]
    [Command("--input_data_dir")]    
    public string InputDataPath = "../Luban/Datas";

    [CommandPathAttribute]
    [Command("--output_code_dir")]    
    public string OutCodePath = "Assets/Hotfix/CodeGen/Config";

    [CommandPathAttribute]    
    [Command("--output_data_dir")]
    public string OutDataPath = "Assets/Data/Res/Config";

    [Command("--gen_types")]    
    public string GenType = "code_cs_unity_bin,data_bin";

    [Command("--service")]    
    public string ServiceType = "client";



    /// <summary>
    /// 存储配置文件
    /// </summary>
    public void SaveFile()
    {
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
        Debug.Log($"{nameof(ConfigSettingData)}.asset is saved!");
    }
    public static ConfigSettingData Setting;
    public string GetCommand()
    {
        string line_end = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? " ^" : " \\";

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

            // 当前值为空 或者 False, 或者 None(Enum 默认值)
            // 则继续循环
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
                sb.Append($" {command.option} {value} ");
            }


            if (command.newLine)
            {
                sb.Append($"{line_end} \n");
            }
        }

        return sb.ToString();
    }

    #region 初始化   
    public static ConfigSettingData LoadConfig()
    {
        if (Setting == null)
        {
            Setting = SettingTools.GetSingletonAssets<ConfigSettingData>("Assets/Setting/Build/Config");
        }
        return Setting;
    }
    public static void SaveConfig()
    {
        Setting?.SaveFile();
    }

    #endregion
}
