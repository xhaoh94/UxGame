//自动生成的代码，请勿修改!!!
//CodeGen By [Assets/UIElements/CodeGenByUxml]
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
namespace Ux.Editor.Build.Proto
{
	public partial class ProtoWindow
	{
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
		protected void CreateChildren()
		{
			var _visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Build/Proto/Uxml/ProtoWindow.uxml");
			if (_visualAsset == null) return;
			root = _visualAsset.CloneTree();
			root.style.flexGrow = 1f;
			txtPbTool = root.Q<TextField>("txtPbTool");
			txtPbTool.RegisterValueChangedCallback(e => _OnTxtPbToolChanged(e));
			btnPbTool = root.Q<Button>("btnPbTool");
			btnPbTool.clicked += () => _OnBtnPbToolClick();
			txtConfig = root.Q<TextField>("txtConfig");
			txtConfig.RegisterValueChangedCallback(e => _OnTxtConfigChanged(e));
			btnConfig = root.Q<Button>("btnConfig");
			btnConfig.clicked += () => _OnBtnConfigClick();
			txtInPath = root.Q<TextField>("txtInPath");
			txtInPath.RegisterValueChangedCallback(e => _OnTxtInPathChanged(e));
			btnInPath = root.Q<Button>("btnInPath");
			btnInPath.clicked += () => _OnBtnInPathClick();
			txtOutPath = root.Q<TextField>("txtOutPath");
			txtOutPath.RegisterValueChangedCallback(e => _OnTxtOutPathChanged(e));
			btnOutPath = root.Q<Button>("btnOutPath");
			btnOutPath.clicked += () => _OnBtnOutPathClick();
			txtNamespace = root.Q<TextField>("txtNamespace");
			txtNamespace.RegisterValueChangedCallback(e => _OnTxtNamespaceChanged(e));
			popContainer = root.Q<VisualElement>("popContainer");
			btnExport = root.Q<Button>("btnExport");
			btnExport.clicked += () => _OnBtnExportClick();
		}
		partial void _OnTxtPbToolChanged(ChangeEvent<string> e);
		partial void _OnBtnPbToolClick();
		partial void _OnTxtConfigChanged(ChangeEvent<string> e);
		partial void _OnBtnConfigClick();
		partial void _OnTxtInPathChanged(ChangeEvent<string> e);
		partial void _OnBtnInPathClick();
		partial void _OnTxtOutPathChanged(ChangeEvent<string> e);
		partial void _OnBtnOutPathClick();
		partial void _OnTxtNamespaceChanged(ChangeEvent<string> e);
		partial void _OnBtnExportClick();
	}
}
