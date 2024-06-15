//自动生成的代码，请勿修改!!!
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
		protected ListView listExport;
		protected Button btnRemove;
		protected Button btnAdd;
		protected VisualElement exportElement;
		protected TextField txtName;
		protected EnumField platformType;
		protected TextField txtVersion;
		protected EnumField buildType;
		protected TextField inputBundlePath;
		protected Button btnBundlePath;
		protected Toggle tgCopy;
		protected TextField inputCopyPath;
		protected Button btnCopyPath;
		protected Toggle tgClearSandBox;
		protected Toggle tgCompileDLL;
		protected Toggle tgCompileAot;
		protected Toggle tgCompileUI;
		protected Toggle tgCompileConfig;
		protected Toggle tgCompileProto;
		protected MaskField buildPackage;
		protected VisualElement encryptionContainer;
		protected Toggle tgExe;
		protected VisualElement exeElement;
		protected TextField inputExePath;
		protected Button btnExePath;
		protected EnumField compileType;
		protected Button build;
		protected Button clear;
		protected Toolbar Toolbar;
		protected VisualElement Container;
		protected void CreateChildren()
		{
			var _visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Build/Version/VersionWindow.uxml");
			if (_visualAsset == null) return;
			root = _visualAsset.CloneTree();
			listExport =root.Q<ListView>("listExport");
			listExport.makeItem = ()=> { var e = new VisualElement(); _OnMakeListExportItem(e); return e; };
			listExport.bindItem = (e,i)=> _OnBindListExportItem(e,i);
			#if UNITY_2022_1_OR_NEWER
			listExport.selectionChanged += e => _OnListExportItemClick(e);
			#else
			listExport.onSelectionChange += e => _OnListExportItemClick(e);
			#endif
			btnRemove =root.Q<Button>("btnRemove");
			btnRemove.clicked += () => _OnBtnRemoveClick();
			btnAdd =root.Q<Button>("btnAdd");
			btnAdd.clicked += () => _OnBtnAddClick();
			exportElement =root.Q<VisualElement>("exportElement");
			txtName =root.Q<TextField>("txtName");
			txtName.RegisterValueChangedCallback(e => _OnTxtNameChanged(e));
			platformType =root.Q<EnumField>("platformType");
			platformType.RegisterValueChangedCallback(e => _OnPlatformTypeChanged(e));
			txtVersion =root.Q<TextField>("txtVersion");
			txtVersion.RegisterValueChangedCallback(e => _OnTxtVersionChanged(e));
			buildType =root.Q<EnumField>("buildType");
			buildType.RegisterValueChangedCallback(e => _OnBuildTypeChanged(e));
			inputBundlePath =root.Q<TextField>("inputBundlePath");
			inputBundlePath.RegisterValueChangedCallback(e => _OnInputBundlePathChanged(e));
			btnBundlePath =root.Q<Button>("btnBundlePath");
			btnBundlePath.clicked += () => _OnBtnBundlePathClick();
			tgCopy =root.Q<Toggle>("tgCopy");
			tgCopy.RegisterValueChangedCallback(e => _OnTgCopyChanged(e));
			inputCopyPath =root.Q<TextField>("inputCopyPath");
			inputCopyPath.RegisterValueChangedCallback(e => _OnInputCopyPathChanged(e));
			btnCopyPath =root.Q<Button>("btnCopyPath");
			btnCopyPath.clicked += () => _OnBtnCopyPathClick();
			tgClearSandBox =root.Q<Toggle>("tgClearSandBox");
			tgClearSandBox.RegisterValueChangedCallback(e => _OnTgClearSandBoxChanged(e));
			tgCompileDLL =root.Q<Toggle>("tgCompileDLL");
			tgCompileDLL.RegisterValueChangedCallback(e => _OnTgCompileDLLChanged(e));
			tgCompileAot =root.Q<Toggle>("tgCompileAot");
			tgCompileAot.RegisterValueChangedCallback(e => _OnTgCompileAotChanged(e));
			tgCompileUI =root.Q<Toggle>("tgCompileUI");
			tgCompileUI.RegisterValueChangedCallback(e => _OnTgCompileUIChanged(e));
			tgCompileConfig =root.Q<Toggle>("tgCompileConfig");
			tgCompileConfig.RegisterValueChangedCallback(e => _OnTgCompileConfigChanged(e));
			tgCompileProto =root.Q<Toggle>("tgCompileProto");
			tgCompileProto.RegisterValueChangedCallback(e => _OnTgCompileProtoChanged(e));
			buildPackage =root.Q<MaskField>("buildPackage");
			encryptionContainer =root.Q<VisualElement>("encryptionContainer");
			tgExe =root.Q<Toggle>("tgExe");
			tgExe.RegisterValueChangedCallback(e => _OnTgExeChanged(e));
			exeElement =root.Q<VisualElement>("exeElement");
			inputExePath =root.Q<TextField>("inputExePath");
			inputExePath.RegisterValueChangedCallback(e => _OnInputExePathChanged(e));
			btnExePath =root.Q<Button>("btnExePath");
			btnExePath.clicked += () => _OnBtnExePathClick();
			compileType =root.Q<EnumField>("compileType");
			compileType.RegisterValueChangedCallback(e => _OnCompileTypeChanged(e));
			build =root.Q<Button>("build");
			build.clicked += () => _OnBuildClick();
			clear =root.Q<Button>("clear");
			clear.clicked += () => _OnClearClick();
			Toolbar =root.Q<Toolbar>("Toolbar");
			Container =root.Q<VisualElement>("Container");
		}
		partial void _OnMakeListExportItem(VisualElement e);
		partial void _OnBindListExportItem(VisualElement e,int index);
		partial void _OnListExportItemClick(IEnumerable<object> objs);
		partial void _OnBtnRemoveClick();
		partial void _OnBtnAddClick();
		partial void _OnTxtNameChanged(ChangeEvent<string> e);
		partial void _OnPlatformTypeChanged(ChangeEvent<Enum> e);
		partial void _OnTxtVersionChanged(ChangeEvent<string> e);
		partial void _OnBuildTypeChanged(ChangeEvent<Enum> e);
		partial void _OnInputBundlePathChanged(ChangeEvent<string> e);
		partial void _OnBtnBundlePathClick();
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
