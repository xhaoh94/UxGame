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
        
        public void CreateGUI()
        {
            try
            {
                Setting = ProtoSettingData.LoadConfig();
                CreateChildren();
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
                _type.label = "生成数类型";
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
        partial void _OnTxtPbToolChanged(ChangeEvent<string> e)
        {
            Setting.PbTool = e.newValue;
        }
        partial void _OnBtnPbToolClick()
        {
            BuildHelper.OpenFilePanel(Setting.PbTool, "PbTool", txtPbTool, "dll");
        }
        partial void _OnTxtConfigChanged(ChangeEvent<string> e)
        {
            Setting.Config = e.newValue;
        }
        partial void _OnBtnConfigClick()
        {
            BuildHelper.OpenFilePanel(Setting.Config, "Config.json", txtConfig, "json");
        }

        partial void _OnTxtInPathChanged(ChangeEvent<string> e)
        {
            Setting.InPath = e.newValue;
        }
        partial void _OnBtnInPathClick()
        {
            BuildHelper.OpenFolderPanel(Setting.InPath, "请选择Proto目录", txtInPath);
        }
        partial void _OnTxtOutPathChanged(ChangeEvent<string> e)
        {
            Setting.OutPath = e.newValue;
        }
        partial void _OnBtnOutPathClick()
        {
            BuildHelper.OpenFolderPanel(Setting.OutPath, "请选择导出目录", txtOutPath);
        }
        partial void _OnTxtNamespaceChanged(ChangeEvent<string> e)
        {
            Setting.NameSpace = e.newValue;
        }
        partial void _OnBtnExportClick()
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
            Log.Debug("---------------------------------------->生成Proto文件<---------------------------------------");
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
