//自动生成的代码，请勿修改!!!
//CodeGen By [Assets/UIElements/CodeGenByUxml]
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
namespace Ux.Editor.Build.Config
{
	public partial class ConfigWindow
	{
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
		protected void CreateChildren()
		{
			var _visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Build/Config/Uxml/ConfigWindow.uxml");
			if (_visualAsset == null) return;
			root = _visualAsset.CloneTree();
			root.style.flexGrow = 1f;
			txtDllFile = root.Q<TextField>("txtDllFile");
			txtDllFile.RegisterValueChangedCallback(e => _OnTxtDllFileChanged(e));
			btnDllFile = root.Q<Button>("btnDllFile");
			btnDllFile.clicked += () => _OnBtnDllFileClick();
			txtDefineFile = root.Q<TextField>("txtDefineFile");
			txtDefineFile.RegisterValueChangedCallback(e => _OnTxtDefineFileChanged(e));
			btnDefineFile = root.Q<Button>("btnDefineFile");
			btnDefineFile.clicked += () => _OnBtnDefineFileClick();
			txtOutDataPath = root.Q<TextField>("txtOutDataPath");
			txtOutDataPath.RegisterValueChangedCallback(e => _OnTxtOutDataPathChanged(e));
			btnOutDataPath = root.Q<Button>("btnOutDataPath");
			btnOutDataPath.clicked += () => _OnBtnOutDataPathClick();
			txtOutCodePath = root.Q<TextField>("txtOutCodePath");
			txtOutCodePath.RegisterValueChangedCallback(e => _OnTxtOutCodePathChanged(e));
			btnOutCodePath = root.Q<Button>("btnOutCodePath");
			btnOutCodePath.clicked += () => _OnBtnOutCodePathClick();
			popContainer = root.Q<VisualElement>("popContainer");
			btnExport = root.Q<Button>("btnExport");
			btnExport.clicked += () => _OnBtnExportClick();
		}
		partial void _OnTxtDllFileChanged(ChangeEvent<string> e);
		partial void _OnBtnDllFileClick();
		partial void _OnTxtDefineFileChanged(ChangeEvent<string> e);
		partial void _OnBtnDefineFileClick();
		partial void _OnTxtOutDataPathChanged(ChangeEvent<string> e);
		partial void _OnBtnOutDataPathClick();
		partial void _OnTxtOutCodePathChanged(ChangeEvent<string> e);
		partial void _OnBtnOutCodePathClick();
		partial void _OnBtnExportClick();
	}
}
