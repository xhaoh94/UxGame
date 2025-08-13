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


        private List<string> GenCodeTypes = new List<string>() {
            "cs-newtonsoft-json", "cs-bin"
        };
        private List<string> GenDataTypes = new List<string>() {
            "json","bin",
        };
        private List<string> ServiceTypes = new List<string>() { "client", "server" };
        

        PopupField<string> _genCodeType;
        PopupField<string> _genDataType;
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
                txtDefineFile.SetValueWithoutNotify(Setting.ConfFile);                                     
                txtOutCodePath.SetValueWithoutNotify(Setting.OutCodePath);                                     
                txtOutDataPath.SetValueWithoutNotify(Setting.OutDataPath);                

                var popContainer = root.Q("popContainer");
                var index = GenCodeTypes.IndexOf(Setting.GenCodeType);
                if (index < 0) index = 0;
                _genCodeType = new PopupField<string>(GenCodeTypes, index);
                _genCodeType.label = "生成代码类型";
                _genCodeType.style.width = 500;
                _genCodeType.RegisterValueChangedCallback(evt =>
                {
                    Setting.GenCodeType = evt.newValue;
                });
                popContainer.Add(_genCodeType);

                index = GenDataTypes.IndexOf(Setting.GenDataType);
                if (index < 0) index = 0;
                _genDataType = new PopupField<string>(GenDataTypes, index);
                _genDataType.label = "生成数据类型";
                _genDataType.style.width = 500;
                _genDataType.RegisterValueChangedCallback(evt =>
                {
                    Setting.GenDataType = evt.newValue;
                });
                popContainer.Add(_genDataType);

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
            BuildHelper.OpenFilePanel(Setting.DllFile, "Luban.Dll", txtDllFile, "dll");
        }
        partial void _OnTxtDefineFileChanged(ChangeEvent<string> e)
        {
            Setting.ConfFile = e.newValue;
        }
        partial void _OnBtnDefineFileClick()
        {
            BuildHelper.OpenFilePanel(Setting.ConfFile, "luban.conf", txtDefineFile, "conf");
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
