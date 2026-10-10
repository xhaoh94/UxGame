using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Ux.Editor.Build.Version;
namespace Ux.Editor.Build.Proto
{
    public partial class ProtoWindow : EditorWindow
    {
        [MenuItem("UxGame/工具/协议", false, 540)]
        public static void ShowConfigWindon()
        {
            var window = GetWindow<ProtoWindow>("ProtoWindow", true);
            window.minSize = new Vector2(800, 500);
        }

        private List<string> GenTypes = new List<string>() { "csharp_pbnet", "csharp" };
        
        PopupField<string> _type;
        ProtoSettingData Setting;
        
        protected VisualElement root;
        public TextField txtPbTool;
        public Button btnPbTool;
        public TextField txtConfig;
        public Button btnConfig;
        public TextField txtInPath;
        public Button btnInPath;
        public TextField txtOutPath;
        public Button btnOutPath;
        public TextField txtNamespace;
        public VisualElement popContainer;
        public Button btnExport;

        /// <summary>原 ProtoWindow.uxml 的手搭等价版本。</summary>
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

            var pbToolRow = PathRow("txtPbTool", "PbTool", "btnPbTool", _OnBtnPbToolClick);
            txtPbTool = pbToolRow.field;
            txtPbTool.RegisterValueChangedCallback(_OnTxtPbToolChanged);
            body.Add(pbToolRow.row);

            var configRow = PathRow("txtConfig", "Config.json", "btnConfig", _OnBtnConfigClick);
            txtConfig = configRow.field;
            txtConfig.RegisterValueChangedCallback(_OnTxtConfigChanged);
            body.Add(configRow.row);

            var inPathRow = PathRow("txtInPath", "Proto目录", "btnInPath", _OnBtnInPathClick);
            txtInPath = inPathRow.field;
            txtInPath.RegisterValueChangedCallback(_OnTxtInPathChanged);
            body.Add(inPathRow.row);

            var outPathRow = PathRow("txtOutPath", "导出目录", "btnOutPath", _OnBtnOutPathClick);
            txtOutPath = outPathRow.field;
            txtOutPath.RegisterValueChangedCallback(_OnTxtOutPathChanged);
            body.Add(outPathRow.row);

            var namespaceRow = new VisualElement();
            namespaceRow.style.flexDirection = FlexDirection.Row;
            namespaceRow.style.flexGrow = 0f;
            txtNamespace = new TextField("命名空间") { name = "txtNamespace", pickingMode = PickingMode.Ignore };
            txtNamespace.style.flexGrow = 1f;
            txtNamespace.RegisterValueChangedCallback(_OnTxtNamespaceChanged);
            namespaceRow.Add(txtNamespace);
            body.Add(namespaceRow);

            popContainer = new VisualElement { name = "popContainer" };
            body.Add(popContainer);

            btnExport = new Button(_OnBtnExportClick) { name = "btnExport", text = "导出" };
            root.Add(btnExport);
        }

        public void CreateGUI()
        {
            try
            {
                Setting = ProtoSettingData.LoadConfig();
                BuildUI();
                rootVisualElement.Add(root);  
                
                txtPbTool.SetValueWithoutNotify(Setting.PbTool);                                   
                txtConfig.SetValueWithoutNotify(Setting.Config);                     
                txtInPath.SetValueWithoutNotify(Setting.InPath);                    
                txtOutPath.SetValueWithoutNotify(Setting.OutPath);                                
                txtNamespace.SetValueWithoutNotify(Setting.NameSpace);                

                var popContainer = root.Q("popContainer");
                var index = GenTypes.IndexOf(Setting.Type);
                if (index < 0) index = 0;
                _type = new PopupField<string>(GenTypes, index);
                _type.label = "生成类型";
                _type.style.width = 500;
                _type.RegisterValueChangedCallback(evt =>
                {
                    Setting.Type = evt.newValue;
                });
                popContainer.Add(_type);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private void OnDestroy()
        {
            ProtoSettingData.SaveConfig();
            AssetDatabase.Refresh();
        }
        private void _OnTxtPbToolChanged(ChangeEvent<string> e)
        {
            Setting.PbTool = e.newValue;
        }
        private void _OnBtnPbToolClick()
        {
            BuildHelper.OpenFilePanel(Setting.PbTool, "PbTool", txtPbTool, "dll");
        }
        private void _OnTxtConfigChanged(ChangeEvent<string> e)
        {
            Setting.Config = e.newValue;
        }
        private void _OnBtnConfigClick()
        {
            BuildHelper.OpenFilePanel(Setting.Config, "Config.json", txtConfig, "json");
        }

        private void _OnTxtInPathChanged(ChangeEvent<string> e)
        {
            Setting.InPath = e.newValue;
        }
        private void _OnBtnInPathClick()
        {
            BuildHelper.OpenFolderPanel(Setting.InPath, "请选择Proto目录", txtInPath);
        }
        private void _OnTxtOutPathChanged(ChangeEvent<string> e)
        {
            Setting.OutPath = e.newValue;
        }
        private void _OnBtnOutPathClick()
        {
            BuildHelper.OpenFolderPanel(Setting.OutPath, "请选择导出目录", txtOutPath);
        }
        private void _OnTxtNamespaceChanged(ChangeEvent<string> e)
        {
            Setting.NameSpace = e.newValue;
        }
        private void _OnBtnExportClick()
        {
            Export().Forget();
        }

        public static async UniTask Export()
        {
            var Setting = ProtoSettingData.LoadConfig();
            if (Setting == null)
            {
                return;
            }
            Log.Debug("---------------------------------------->导出Proto文件<---------------------------------------");
            UniTask ExportProto()
            {
                var task = AutoResetUniTaskCompletionSource.Create();
                Command.Run(Command.DOTNET, Setting.GetCommand(), true, () =>
                {
                    task?.TrySetResult();
                });
                return task.Task;
            }
            await ExportProto();
        }

    }
}
