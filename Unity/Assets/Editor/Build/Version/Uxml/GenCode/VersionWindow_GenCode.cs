//自动生成的代码，请勿修改!!!
//CodeGen By [Assets/UIElements/CodeGenByUxml]
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
namespace Ux.Editor.Build.Version
{
	public partial class VersionWindow
	{
		protected VisualElement root;
		public ListView listExport;
		public Button btnRemove;
		public Button btnAdd;
		public VisualElement exportElement;
		public TextField txtName;
		public EnumField platformType;
		public TextField txtVersion;
		public EnumField buildType;
		public TextField inputBundlePath;
		public Button btnBundlePath;
		public Toggle tgUseDb;
		public Toggle tgCopy;
		public TextField inputCopyPath;
		public Button btnCopyPath;
		public Toggle tgClearSandBox;
		public Toggle tgCompileDLL;
		public Toggle tgCompileAot;
		public Toggle tgCompileUI;
		public Toggle tgCompileConfig;
		public Toggle tgCompileProto;
		public MaskField buildPackage;
		public VisualElement encryptionContainer;
		public Toggle tgExe;
		public VisualElement exeElement;
		public TextField inputExePath;
		public Button btnExePath;
		public EnumField compileType;
		public Button build;
		public Button clear;
		public Toolbar Toolbar;
		public VisualElement Container;
		protected void CreateChildren()
		{
			var _visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Build/Version/Uxml/VersionWindow.uxml");
			if (_visualAsset == null) return;
			root = _visualAsset.CloneTree();
			root.style.flexGrow = 1f;
			listExport = root.Q<ListView>("listExport");
			listExport.makeItem = ()=> { var e = new VisualElement(); _OnMakeListExportItem(e); return e; };
			listExport.bindItem = (e,i)=> _OnBindListExportItem(e,i);
			#if UNITY_2022_1_OR_NEWER
			listExport.selectionChanged += e => _OnListExportItemClick(e);
			#else
			listExport.onSelectionChange += e => _OnListExportItemClick(e);
			#endif
			btnRemove = root.Q<Button>("btnRemove");
			btnRemove.clicked += () => _OnBtnRemoveClick();
			btnAdd = root.Q<Button>("btnAdd");
			btnAdd.clicked += () => _OnBtnAddClick();
			exportElement = root.Q<VisualElement>("exportElement");
			txtName = root.Q<TextField>("txtName");
			txtName.RegisterValueChangedCallback(e => _OnTxtNameChanged(e));
			platformType = root.Q<EnumField>("platformType");
			platformType.RegisterValueChangedCallback(e => _OnPlatformTypeChanged(e));
			txtVersion = root.Q<TextField>("txtVersion");
			buildType = root.Q<EnumField>("buildType");
			buildType.RegisterValueChangedCallback(e => _OnBuildTypeChanged(e));
			inputBundlePath = root.Q<TextField>("inputBundlePath");
			inputBundlePath.RegisterValueChangedCallback(e => _OnInputBundlePathChanged(e));
			btnBundlePath = root.Q<Button>("btnBundlePath");
			btnBundlePath.clicked += () => _OnBtnBundlePathClick();
			tgUseDb = root.Q<Toggle>("tgUseDb");
			tgUseDb.RegisterValueChangedCallback(e => _OnTgUseDbChanged(e));
			tgCopy = root.Q<Toggle>("tgCopy");
			tgCopy.RegisterValueChangedCallback(e => _OnTgCopyChanged(e));
			inputCopyPath = root.Q<TextField>("inputCopyPath");
			inputCopyPath.RegisterValueChangedCallback(e => _OnInputCopyPathChanged(e));
			btnCopyPath = root.Q<Button>("btnCopyPath");
			btnCopyPath.clicked += () => _OnBtnCopyPathClick();
			tgClearSandBox = root.Q<Toggle>("tgClearSandBox");
			tgClearSandBox.RegisterValueChangedCallback(e => _OnTgClearSandBoxChanged(e));
			tgCompileDLL = root.Q<Toggle>("tgCompileDLL");
			tgCompileDLL.RegisterValueChangedCallback(e => _OnTgCompileDLLChanged(e));
			tgCompileAot = root.Q<Toggle>("tgCompileAot");
			tgCompileAot.RegisterValueChangedCallback(e => _OnTgCompileAotChanged(e));
			tgCompileUI = root.Q<Toggle>("tgCompileUI");
			tgCompileUI.RegisterValueChangedCallback(e => _OnTgCompileUIChanged(e));
			tgCompileConfig = root.Q<Toggle>("tgCompileConfig");
			tgCompileConfig.RegisterValueChangedCallback(e => _OnTgCompileConfigChanged(e));
			tgCompileProto = root.Q<Toggle>("tgCompileProto");
			tgCompileProto.RegisterValueChangedCallback(e => _OnTgCompileProtoChanged(e));
			buildPackage = root.Q<MaskField>("buildPackage");
			encryptionContainer = root.Q<VisualElement>("encryptionContainer");
			tgExe = root.Q<Toggle>("tgExe");
			tgExe.RegisterValueChangedCallback(e => _OnTgExeChanged(e));
			exeElement = root.Q<VisualElement>("exeElement");
			inputExePath = root.Q<TextField>("inputExePath");
			inputExePath.RegisterValueChangedCallback(e => _OnInputExePathChanged(e));
			btnExePath = root.Q<Button>("btnExePath");
			btnExePath.clicked += () => _OnBtnExePathClick();
			compileType = root.Q<EnumField>("compileType");
			compileType.RegisterValueChangedCallback(e => _OnCompileTypeChanged(e));
			build = root.Q<Button>("build");
			build.clicked += () => _OnBuildClick();
			clear = root.Q<Button>("clear");
			clear.clicked += () => _OnClearClick();
			Toolbar = root.Q<Toolbar>("Toolbar");
			Container = root.Q<VisualElement>("Container");
		}
		partial void _OnMakeListExportItem(VisualElement e);
		partial void _OnBindListExportItem(VisualElement e,int index);
		partial void _OnListExportItemClick(IEnumerable<object> objs);
		partial void _OnBtnRemoveClick();
		partial void _OnBtnAddClick();
		partial void _OnTxtNameChanged(ChangeEvent<string> e);
		partial void _OnPlatformTypeChanged(ChangeEvent<Enum> e);
		partial void _OnBuildTypeChanged(ChangeEvent<Enum> e);
		partial void _OnInputBundlePathChanged(ChangeEvent<string> e);
		partial void _OnBtnBundlePathClick();
		partial void _OnTgUseDbChanged(ChangeEvent<bool> e);
		partial void _OnTgCopyChanged(ChangeEvent<bool> e);
		partial void _OnInputCopyPathChanged(ChangeEvent<string> e);
		partial void _OnBtnCopyPathClick();
		partial void _OnTgClearSandBoxChanged(ChangeEvent<bool> e);
		partial void _OnTgCompileDLLChanged(ChangeEvent<bool> e);
		partial void _OnTgCompileAotChanged(ChangeEvent<bool> e);
		partial void _OnTgCompileUIChanged(ChangeEvent<bool> e);
		partial void _OnTgCompileConfigChanged(ChangeEvent<bool> e);
		partial void _OnTgCompileProtoChanged(ChangeEvent<bool> e);
		partial void _OnTgExeChanged(ChangeEvent<bool> e);
		partial void _OnInputExePathChanged(ChangeEvent<string> e);
		partial void _OnBtnExePathClick();
		partial void _OnCompileTypeChanged(ChangeEvent<Enum> e);
		partial void _OnBuildClick();
		partial void _OnClearClick();
	}
}
