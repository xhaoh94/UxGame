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


       
        protected VisualElement root;
        public TextField txtDllFile;
        public Button btnDllFile;
        public TextField txtDefineFile;
        public Button btnDefineFile;
        public TextField txtOutDataPath;
        public Button btnOutDataPath;
        public TextField txtOutCodePath;
        public Button btnOutCodePath;
        public VisualElement popContainer;
        public Button btnExport;

        /// <summary>原 ConfigWindow.uxml 的手搭等价版本。</summary>
        private void BuildUI()
        {
            root = new VisualElement();
            root.style.flexGrow = 1f;

            var body = new VisualElement();
            root.Add(body);

            (VisualElement row, TextField field) PathRow(string fieldName, string label, string buttonName, Action onPick)
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.flexGrow = 0f;
                var field = new TextField(label) { name = fieldName, pickingMode = PickingMode.Ignore };
                field.style.flexGrow = 1f;
                row.Add(field);
                var button = new Button(onPick) { name = buttonName, text = "选择" };
                button.style.flexGrow = 0f;
                row.Add(button);
                return (row, field);
            }

            var dllRow = PathRow("txtDllFile", "Luban.dll", "btnDllFile", _OnBtnDllFileClick);
            txtDllFile = dllRow.field;
            txtDllFile.RegisterValueChangedCallback(_OnTxtDllFileChanged);
            body.Add(dllRow.row);

            var defineRow = PathRow("txtDefineFile", "Lunban.conf", "btnDefineFile", _OnBtnDefineFileClick);
            txtDefineFile = defineRow.field;
            txtDefineFile.RegisterValueChangedCallback(_OnTxtDefineFileChanged);
            body.Add(defineRow.row);

            var foldout = new Foldout { text = "导出设置" };
            body.Add(foldout);

            var dataRow = PathRow("txtOutDataPath", "导出数据目录", "btnOutDataPath", _OnBtnOutDataPathClick);
            txtOutDataPath = dataRow.field;
            txtOutDataPath.RegisterValueChangedCallback(_OnTxtOutDataPathChanged);
            foldout.Add(dataRow.row);

            var codeRow = PathRow("txtOutCodePath", "导出代码目录", "btnOutCodePath", _OnBtnOutCodePathClick);
            txtOutCodePath = codeRow.field;
            txtOutCodePath.RegisterValueChangedCallback(_OnTxtOutCodePathChanged);
            foldout.Add(codeRow.row);

            popContainer = new VisualElement { name = "popContainer" };
            foldout.Add(popContainer);

            var updateButton = new Button { text = "更新" };
            updateButton.style.display = DisplayStyle.None;
            root.Add(updateButton);

            btnExport = new Button(_OnBtnExportClick) { name = "btnExport", text = "导出" };
            root.Add(btnExport);
        }

        public void CreateGUI()
        {
            try
            {
                Setting = ConfigSettingData.LoadConfig();
                BuildUI();
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
                _serviceType.label = "服务类型";
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
        private void _OnTxtDllFileChanged(ChangeEvent<string> e)
        {
            Setting.DllFile = e.newValue;
        }
        private void _OnBtnDllFileClick()
        {
            BuildHelper.OpenFilePanel(Setting.DllFile, "Luban.Dll", txtDllFile, "dll");
        }
        private void _OnTxtDefineFileChanged(ChangeEvent<string> e)
        {
            Setting.ConfFile = e.newValue;
        }
        private void _OnBtnDefineFileClick()
        {
            BuildHelper.OpenFilePanel(Setting.ConfFile, "luban.conf", txtDefineFile, "conf");
        }
        private void _OnTxtOutCodePathChanged(ChangeEvent<string> e)
        {
            Setting.OutCodePath = e.newValue;
        }
        private void _OnBtnOutCodePathClick()
        {
            BuildHelper.OpenFolderPanel(Setting.OutCodePath, "选择输出目录", txtOutCodePath);
        }
        private void _OnTxtOutDataPathChanged(ChangeEvent<string> e)
        {
            Setting.OutDataPath = e.newValue;
        }
        private void _OnBtnOutDataPathClick()
        {
            BuildHelper.OpenFolderPanel(Setting.OutDataPath, "选择输出目录", txtOutDataPath);
        }
        private void _OnBtnExportClick()
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
