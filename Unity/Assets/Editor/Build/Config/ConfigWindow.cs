using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Ux.Editor.Build.Version;
namespace Ux.Editor.Build.Config
{
    public enum ConfGenType
    {
        Bin,
        Json
    }
    public enum ConfServiceType
    {
        Client,
        Server
    }
    public partial class ConfigWindow : EditorWindow
    {
        [MenuItem("UxGame/工具/配置", false, 530)]
        public static void ShowConfigWindon()
        {
            var window = GetWindow<ConfigWindow>("ConfigWindow", true);
            window.minSize = new Vector2(800, 500);
        }


        private List<string> GenTypes = new List<string>() { "code_cs_unity_bin,data_bin", "code_cs_unity_json,data_json" };
        private List<string> ServiceTypes = new List<string>() { "client", "server" };
        

        PopupField<string> _genType;
        PopupField<string> _serviceType;
        ConfigSettingData Setting;


       
        public void CreateGUI()
        {
            try
            {
                Setting = ConfigSettingData.LoadConfig();
                CreateChildren();
                rootVisualElement.Add(root);
                
                txtDllFile.SetValueWithoutNotify(Setting.DllFile);                    
                txtDefineFile.SetValueWithoutNotify(Setting.DefineFile);                
                txtInputDataPath.SetValueWithoutNotify(Setting.InputDataPath);                
                txtOutCodePath.SetValueWithoutNotify(Setting.OutCodePath);                                     
                txtOutDataPath.SetValueWithoutNotify(Setting.OutDataPath);                

                var popContainer = root.Q("popContainer");
                var index = GenTypes.IndexOf(Setting.GenType);
                if (index < 0) index = 0;
                _genType = new PopupField<string>(GenTypes, index);
                _genType.label = "生成数类型";
                _genType.style.width = 500;
                _genType.RegisterValueChangedCallback(evt =>
                {
                    Setting.GenType = evt.newValue;
                });
                popContainer.Add(_genType);

                index = ServiceTypes.IndexOf(Setting.ServiceType);
                if (index < 0) index = 0;
                _serviceType = new PopupField<string>(ServiceTypes, index);
                _serviceType.label = "生成类型";
                _serviceType.style.width = 500;
                _serviceType.RegisterValueChangedCallback(evt =>
                {
                    Setting.ServiceType = evt.newValue;
                });
                popContainer.Add(_serviceType);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private void OnDestroy()
        {
            ConfigSettingData.SaveConfig();
            AssetDatabase.Refresh();
        }
        partial void _OnTxtDllFileChanged(ChangeEvent<string> e)
        {
            Setting.DllFile = e.newValue;
        }
        partial void _OnBtnDllFileClick()
        {
            BuildHelper.OpenFilePanel(Setting.DllFile, "Client&Server Dll", txtDllFile, "dll");
        }
        partial void _OnTxtDefineFileChanged(ChangeEvent<string> e)
        {
            Setting.DefineFile = e.newValue;
        }
        partial void _OnBtnDefineFileClick()
        {
            BuildHelper.OpenFilePanel(Setting.DefineFile, "Root.xml", txtDefineFile, "xml");
        }
        partial void _OnTxtInputDataPathChanged(ChangeEvent<string> e)
        {
            Setting.InputDataPath = e.newValue;
        }
        partial void _OnBtnInputDataPathClick()
        {
            BuildHelper.OpenFolderPanel(Setting.InputDataPath, "请选择配置目录", txtInputDataPath);
        }
        partial void _OnTxtOutCodePathChanged(ChangeEvent<string> e)
        {
            Setting.OutCodePath = e.newValue;
        }
        partial void _OnBtnOutCodePathClick()
        {
            BuildHelper.OpenFolderPanel(Setting.OutCodePath, "导出代码目录", txtOutCodePath);
        }
        partial void _OnTxtOutDataPathChanged(ChangeEvent<string> e)
        {
            Setting.OutDataPath = e.newValue;
        }
        partial void _OnBtnOutDataPathClick()
        {
            BuildHelper.OpenFolderPanel(Setting.OutDataPath, "导出数据目录", txtOutDataPath);
        }
        partial void _OnBtnExportClick()
        {
            Export().Forget();
        }


        public static async UniTask Export()
        {
            var Setting = ConfigSettingData.LoadConfig();
            if (Setting == null)
            {
                return;
            }
            Log.Debug("---------------------------------------->生成配置文件<---------------------------------------");
            UniTask ExportConfig()
            {
                var configTask = AutoResetUniTaskCompletionSource.Create();
                Command.Run(Command.DOTNET, Setting.GetCommand(), true, () =>
                {
                    configTask?.TrySetResult();
                });
                return configTask.Task;
            }
            await ExportConfig();
        }

    }
}
