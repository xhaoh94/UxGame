//自动生成的代码，请勿修改!!!
//CodeGen By [Assets/UIElements/CodeGenByUxml]
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
namespace Ux.Editor.Build.UI
{
	public partial class UICodeGenWindow
	{
		protected VisualElement root;
		public TextField inputCodePath;
		public Button btnCodePath;
		public TextField inputNS;
		public Toggle tgIgnore;
		public IMGUIContainer top;
		public Button btnFresh;
		public TextField inputSearch;
		public Button btnSearch;
		public IMGUIContainer mid;
		public IMGUIContainer left;
		public Button btnHide;
		public Button btnShow;
		public ScrollView svPkg;
		public ScrollView svMember;
		public IMGUIContainer right;
		public IMGUIContainer comContaine;
		public DropdownField ddExt;
		public Toggle tgExport;
		public VisualElement elementExport;
		public Toggle tgUseGlobal;
		public IMGUIContainer settContainer;
		public Toggle tgIgnore_select;
		public TextField inputNS_select;
		public TextField inputCodePath_select;
		public Button btnCodePath_select;
		public TextField inputClsName;
		public VisualElement elementContent;
		public Button btnGenSelectItem;
		public Button btnGenAll;
		protected void CreateChildren()
		{
			var _visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Build/UI/Uxml/UICodeGenWindow.uxml");
			if (_visualAsset == null) return;
			root = _visualAsset.CloneTree();
			root.style.flexGrow = 1f;
			inputCodePath = root.Q<TextField>("inputCodePath");
			inputCodePath.RegisterValueChangedCallback(e => _OnInputCodePathChanged(e));
			btnCodePath = root.Q<Button>("btnCodePath");
			btnCodePath.clicked += () => _OnBtnCodePathClick();
			inputNS = root.Q<TextField>("inputNS");
			inputNS.RegisterValueChangedCallback(e => _OnInputNSChanged(e));
			tgIgnore = root.Q<Toggle>("tgIgnore");
			tgIgnore.RegisterValueChangedCallback(e => _OnTgIgnoreChanged(e));
			top = root.Q<IMGUIContainer>("top");
			btnFresh = root.Q<Button>("btnFresh");
			btnFresh.clicked += () => _OnBtnFreshClick();
			inputSearch = root.Q<TextField>("inputSearch");
			inputSearch.RegisterValueChangedCallback(e => _OnInputSearchChanged(e));
			btnSearch = root.Q<Button>("btnSearch");
			btnSearch.clicked += () => _OnBtnSearchClick();
			mid = root.Q<IMGUIContainer>("mid");
			left = root.Q<IMGUIContainer>("left");
			btnHide = root.Q<Button>("btnHide");
			btnHide.clicked += () => _OnBtnHideClick();
			btnShow = root.Q<Button>("btnShow");
			btnShow.clicked += () => _OnBtnShowClick();
			svPkg = root.Q<ScrollView>("svPkg");
			svMember = root.Q<ScrollView>("svMember");
			right = root.Q<IMGUIContainer>("right");
			comContaine = root.Q<IMGUIContainer>("comContaine");
			ddExt = root.Q<DropdownField>("ddExt");
			ddExt.RegisterValueChangedCallback(e => _OnDdExtChanged(e));
			tgExport = root.Q<Toggle>("tgExport");
			tgExport.RegisterValueChangedCallback(e => _OnTgExportChanged(e));
			elementExport = root.Q<VisualElement>("elementExport");
			tgUseGlobal = root.Q<Toggle>("tgUseGlobal");
			tgUseGlobal.RegisterValueChangedCallback(e => _OnTgUseGlobalChanged(e));
			settContainer = root.Q<IMGUIContainer>("settContainer");
			tgIgnore_select = root.Q<Toggle>("tgIgnore_select");
			tgIgnore_select.RegisterValueChangedCallback(e => _OnTgIgnore_selectChanged(e));
			inputNS_select = root.Q<TextField>("inputNS_select");
			inputNS_select.RegisterValueChangedCallback(e => _OnInputNS_selectChanged(e));
			inputCodePath_select = root.Q<TextField>("inputCodePath_select");
			inputCodePath_select.RegisterValueChangedCallback(e => _OnInputCodePath_selectChanged(e));
			btnCodePath_select = root.Q<Button>("btnCodePath_select");
			btnCodePath_select.clicked += () => _OnBtnCodePath_selectClick();
			inputClsName = root.Q<TextField>("inputClsName");
			inputClsName.RegisterValueChangedCallback(e => _OnInputClsNameChanged(e));
			elementContent = root.Q<VisualElement>("elementContent");
			btnGenSelectItem = root.Q<Button>("btnGenSelectItem");
			btnGenSelectItem.clicked += () => _OnBtnGenSelectItemClick();
			btnGenAll = root.Q<Button>("btnGenAll");
			btnGenAll.clicked += () => _OnBtnGenAllClick();
		}
		partial void _OnInputCodePathChanged(ChangeEvent<string> e);
		partial void _OnBtnCodePathClick();
		partial void _OnInputNSChanged(ChangeEvent<string> e);
		partial void _OnTgIgnoreChanged(ChangeEvent<bool> e);
		partial void _OnBtnFreshClick();
		partial void _OnInputSearchChanged(ChangeEvent<string> e);
		partial void _OnBtnSearchClick();
		partial void _OnBtnHideClick();
		partial void _OnBtnShowClick();
		partial void _OnDdExtChanged(ChangeEvent<string> e);
		partial void _OnTgExportChanged(ChangeEvent<bool> e);
		partial void _OnTgUseGlobalChanged(ChangeEvent<bool> e);
		partial void _OnTgIgnore_selectChanged(ChangeEvent<bool> e);
		partial void _OnInputNS_selectChanged(ChangeEvent<string> e);
		partial void _OnInputCodePath_selectChanged(ChangeEvent<string> e);
		partial void _OnBtnCodePath_selectClick();
		partial void _OnInputClsNameChanged(ChangeEvent<string> e);
		partial void _OnBtnGenSelectItemClick();
		partial void _OnBtnGenAllClick();
	}
}
