using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Ux.Editor.Build.Version;
namespace Ux.Editor.Build.Proto
{
    public class ProtoWindow : EditorWindow
    {
        [MenuItem("UxGame/����/Э��", false, 540)]
        public static void ShowConfigWindon()
        {
            var window = GetWindow<ProtoWindow>("ProtoWindow", true);
            window.minSize = new Vector2(800, 500);
        }

        [SerializeField]
        private VisualTreeAsset m_VisualTreeAsset = default;

        private List<string> GenTypes = new List<string>() { "csharp_pbnet", "csharp" };


        TextField _txtPbTool;
        Button _btnPbTool;

        TextField _txtConfig;
        Button _btnConfig;

        TextField _txtInPath;
        Button _btnInPath;

        TextField _txtOutPath;
        Button _btnOutPath;

        TextField _txtNamespace;
        PopupField<string> _type;

        Button _btnExport;

        public void CreateGUI()
        {
            try
            {
                var Setting = ProtoSettingData.LoadConfig();
                VisualElement root = rootVisualElement;
                m_VisualTreeAsset.CloneTree(root);

                _txtPbTool = root.Q<TextField>("txtPbTool");
                _txtPbTool.SetValueWithoutNotify(Setting.PbTool);
                _txtPbTool.RegisterValueChangedCallback(evt =>
                {
                    Setting.PbTool = evt.newValue;
                });
                _btnPbTool = root.Q<Button>("btnPbTool");
                _btnPbTool.clicked += () =>
                {
                    BuildHelper.OpenFilePanel(Setting.PbTool, "PbTool", _txtPbTool, "dll");
                };

                _txtConfig = root.Q<TextField>("txtConfig");
                _txtConfig.SetValueWithoutNotify(Setting.Config);
                _txtConfig.RegisterValueChangedCallback(evt =>
                {
                    Setting.Config = evt.newValue;
                });
                _btnConfig = root.Q<Button>("btnConfig");
                _btnConfig.clicked += () =>
                {
                    BuildHelper.OpenFilePanel(Setting.Config, "Config.json", _txtConfig, "json");
                };


                _txtInPath = root.Q<TextField>("txtInPath");
                _txtInPath.SetValueWithoutNotify(Setting.InPath);
                _txtInPath.RegisterValueChangedCallback(evt =>
                {
                    Setting.InPath = evt.newValue;
                });
                _btnInPath = root.Q<Button>("btnInPath");
                _btnInPath.clicked += () =>
                {
                    BuildHelper.OpenFolderPanel(Setting.InPath, "��ѡ��ProtoĿ¼", _txtInPath);
                };

                _txtOutPath = root.Q<TextField>("txtOutPath");
                _txtOutPath.SetValueWithoutNotify(Setting.OutPath);
                _txtOutPath.RegisterValueChangedCallback(evt =>
                {
                    Setting.OutPath = evt.newValue;
                });
                _btnOutPath = root.Q<Button>("btnOutPath");
                _btnOutPath.clicked += () =>
                {
                    BuildHelper.OpenFolderPanel(Setting.OutPath, "��ѡ�񵼳�Ŀ¼", _txtOutPath);
                };

                _txtNamespace = root.Q<TextField>("txtNamespace");
                _txtNamespace.SetValueWithoutNotify(Setting.NameSpace);
                _txtNamespace.RegisterValueChangedCallback(evt =>
                {
                    Setting.NameSpace = evt.newValue;
                });



                var popContainer = root.Q("popContainer");
                var index = GenTypes.IndexOf(Setting.Type);
                if (index < 0) index = 0;
                _type = new PopupField<string>(GenTypes, index);
                _type.label = "����������";
                _type.style.width = 500;
                _type.RegisterValueChangedCallback(evt =>
                {
                    Setting.Type = evt.newValue;
                });
                popContainer.Add(_type);

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
            ProtoSettingData.SaveConfig();
            AssetDatabase.Refresh();
        }

        void OnBtnExportClick()
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
            Log.Debug("---------------------------------------->����Proto�ļ�<---------------------------------------");
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
