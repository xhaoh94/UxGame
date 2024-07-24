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
    public class ConfigWindow : EditorWindow
    {
        [MenuItem("UxGame/工具/配置", false, 530)]
        public static void ShowConfigWindon()
        {
            var window = GetWindow<ConfigWindow>("ConfigWindow", true);
            window.minSize = new Vector2(800, 500);
        }

        [SerializeField]
        private VisualTreeAsset m_VisualTreeAsset = default;

        private List<string> GenTypes = new List<string>() { "code_cs_unity_bin,data_bin", "code_cs_unity_json,data_json" };
        private List<string> ServiceTypes = new List<string>() { "client", "server" };


        TextField _txtDllFile;
        Button _btnDllFile;

        TextField _txtDefineFile;
        Button _btnDefineFile;

        TextField _txtInputDataPath;
        Button _btnInputDataPath;

        TextField _txtOutCodePath;
        Button _btnOutCodePath;

        TextField _txtOutDataPath;
        Button _btnOutDataPath;

        PopupField<string> _genType;
        PopupField<string> _serviceType;

        Button _btnExport;

        public void CreateGUI()
        {
            try
            {
                var Setting = ConfigSettingData.LoadConfig();
                VisualElement root = rootVisualElement;

                m_VisualTreeAsset.CloneTree(root);

                _txtDllFile = root.Q<TextField>("txtDllFile");
                _txtDllFile.SetValueWithoutNotify(Setting.DllFile);
                _txtDllFile.RegisterValueChangedCallback(evt =>
                {
                    Setting.DllFile = evt.newValue;
                });
                _btnDllFile = root.Q<Button>("btnDllFile");
                _btnDllFile.clicked += () =>
                {
                    BuildHelper.OpenFilePanel(Setting.DllFile, "Client&Server Dll", _txtDllFile, "dll");
                };

                _txtDefineFile = root.Q<TextField>("txtDefineFile");
                _txtDefineFile.SetValueWithoutNotify(Setting.DefineFile);
                _txtDefineFile.RegisterValueChangedCallback(evt =>
                {
                    Setting.DefineFile = evt.newValue;
                });
                _btnDefineFile = root.Q<Button>("btnDefineFile");
                _btnDefineFile.clicked += () =>
                {
                    BuildHelper.OpenFilePanel(Setting.DefineFile, "Root.xml", _txtDefineFile, "xml");
                };


                _txtInputDataPath = root.Q<TextField>("txtInputDataPath");
                _txtInputDataPath.SetValueWithoutNotify(Setting.InputDataPath);
                _txtInputDataPath.RegisterValueChangedCallback(evt =>
                {
                    Setting.InputDataPath = evt.newValue;
                });
                _btnInputDataPath = root.Q<Button>("btnInputDataPath");
                _btnInputDataPath.clicked += () =>
                {
                    BuildHelper.OpenFolderPanel(Setting.InputDataPath, "请选择配置目录", _txtInputDataPath);
                };

                _txtOutCodePath = root.Q<TextField>("txtOutCodePath");
                _txtOutCodePath.SetValueWithoutNotify(Setting.OutCodePath);
                _txtOutCodePath.RegisterValueChangedCallback(evt =>
                {
                    Setting.OutCodePath = evt.newValue;
                });
                _btnOutCodePath = root.Q<Button>("btnOutCodePath");
                _btnOutCodePath.clicked += () =>
                {
                    BuildHelper.OpenFolderPanel(Setting.OutCodePath, "导出代码目录", _txtOutCodePath);
                };

                _txtOutDataPath = root.Q<TextField>("txtOutDataPath");
                _txtOutDataPath.SetValueWithoutNotify(Setting.OutDataPath);
                _txtOutDataPath.RegisterValueChangedCallback(evt =>
                {
                    Setting.OutDataPath = evt.newValue;
                });
                _btnOutDataPath = root.Q<Button>("btnOutDataPath");
                _btnOutDataPath.clicked += () =>
                {
                    BuildHelper.OpenFolderPanel(Setting.OutDataPath, "导出数据目录", _txtOutDataPath);
                };


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

                _btnExport = root.Q<Button>("btnExport");
                _btnExport.clicked += OnBtnExportClick;
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

        void OnBtnExportClick()
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
