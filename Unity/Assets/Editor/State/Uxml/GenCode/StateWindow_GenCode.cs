//自动生成的代码，请勿修改!!!
//CodeGen By [Assets/UIElements/CodeGenByUxml]
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
namespace Ux.Editor.State
{
	public partial class StateWindow
	{
		protected VisualElement root;
		public VisualElement groupContent;
		public Button btnAllSelect;
		public Button btnNoSelect;
		public Button btnExport;
		public TextField inputSearch;
		public Button btnSearchClear;
		public ListView listView;
		public Button btnRemove;
		public Button btnAdd;
		public TextField txtPath;
		public Button btnCodePath;
		public VisualElement infoView;
		public TextField txtGroup;
		public Button btnChangeGroup;
		public Toggle tgMute;
		public IntegerField txtPri;
		public TextField txtClass;
		public TextField txtName;
		public TextField txtDesc;
		public Button btnAddCondition;
		public VisualElement conditionContent;
		public VisualElement veStateCreate;
		public TextField txtStateCreateName;
		public Button btnStateAdd;
		public TextField txtStateGroup;
		public Button btnStateCreate;
		public Button btnStateCancel;
		public VisualElement veConditionCreate;
		public EnumField enumCondition;
		public Button btnConditionCreate;
		public Button btnConditionCancel;
		protected void CreateChildren()
		{
			var _visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/State/Uxml/StateWindow.uxml");
			if (_visualAsset == null) return;
			root = _visualAsset.CloneTree();
			root.style.flexGrow = 1f;
			groupContent =root.Q<VisualElement>("groupContent");
			btnAllSelect =root.Q<Button>("btnAllSelect");
			btnAllSelect.clicked += () => _OnBtnAllSelectClick();
			btnNoSelect =root.Q<Button>("btnNoSelect");
			btnNoSelect.clicked += () => _OnBtnNoSelectClick();
			btnExport =root.Q<Button>("btnExport");
			btnExport.clicked += () => _OnBtnExportClick();
			inputSearch =root.Q<TextField>("inputSearch");
			inputSearch.RegisterValueChangedCallback(e => _OnInputSearchChanged(e));
			btnSearchClear =root.Q<Button>("btnSearchClear");
			btnSearchClear.clicked += () => _OnBtnSearchClearClick();
			listView =root.Q<ListView>("listView");
			listView.makeItem = ()=> { var e = new VisualElement(); _OnMakeListViewItem(e); return e; };
			listView.bindItem = (e,i)=> _OnBindListViewItem(e,i);
			#if UNITY_2022_1_OR_NEWER
			listView.selectionChanged += e => _OnListViewItemClick(e);
			#else
			listView.onSelectionChange += e => _OnListViewItemClick(e);
			#endif
			btnRemove =root.Q<Button>("btnRemove");
			btnRemove.clicked += () => _OnBtnRemoveClick();
			btnAdd =root.Q<Button>("btnAdd");
			btnAdd.clicked += () => _OnBtnAddClick();
			txtPath =root.Q<TextField>("txtPath");
			txtPath.RegisterValueChangedCallback(e => _OnTxtPathChanged(e));
			btnCodePath =root.Q<Button>("btnCodePath");
			btnCodePath.clicked += () => _OnBtnCodePathClick();
			infoView =root.Q<VisualElement>("infoView");
			txtGroup =root.Q<TextField>("txtGroup");
			btnChangeGroup =root.Q<Button>("btnChangeGroup");
			btnChangeGroup.clicked += () => _OnBtnChangeGroupClick();
			tgMute =root.Q<Toggle>("tgMute");
			tgMute.RegisterValueChangedCallback(e => _OnTgMuteChanged(e));
			txtPri =root.Q<IntegerField>("txtPri");
			txtPri.RegisterValueChangedCallback(e => _OnTxtPriChanged(e));
			txtClass =root.Q<TextField>("txtClass");
			txtClass.RegisterValueChangedCallback(e => _OnTxtClassChanged(e));
			txtName =root.Q<TextField>("txtName");
			txtName.RegisterValueChangedCallback(e => _OnTxtNameChanged(e));
			txtDesc =root.Q<TextField>("txtDesc");
			txtDesc.RegisterValueChangedCallback(e => _OnTxtDescChanged(e));
			btnAddCondition =root.Q<Button>("btnAddCondition");
			btnAddCondition.clicked += () => _OnBtnAddConditionClick();
			conditionContent =root.Q<VisualElement>("conditionContent");
			veStateCreate =root.Q<VisualElement>("veStateCreate");
			txtStateCreateName =root.Q<TextField>("txtStateCreateName");
			txtStateCreateName.RegisterValueChangedCallback(e => _OnTxtStateCreateNameChanged(e));
			btnStateAdd =root.Q<Button>("btnStateAdd");
			btnStateAdd.clicked += () => _OnBtnStateAddClick();
			txtStateGroup =root.Q<TextField>("txtStateGroup");
			txtStateGroup.RegisterValueChangedCallback(e => _OnTxtStateGroupChanged(e));
			btnStateCreate =root.Q<Button>("btnStateCreate");
			btnStateCreate.clicked += () => _OnBtnStateCreateClick();
			btnStateCancel =root.Q<Button>("btnStateCancel");
			btnStateCancel.clicked += () => _OnBtnStateCancelClick();
			veConditionCreate =root.Q<VisualElement>("veConditionCreate");
			enumCondition =root.Q<EnumField>("enumCondition");
			enumCondition.RegisterValueChangedCallback(e => _OnEnumConditionChanged(e));
			btnConditionCreate =root.Q<Button>("btnConditionCreate");
			btnConditionCreate.clicked += () => _OnBtnConditionCreateClick();
			btnConditionCancel =root.Q<Button>("btnConditionCancel");
			btnConditionCancel.clicked += () => _OnBtnConditionCancelClick();
		}
		partial void _OnBtnAllSelectClick();
		partial void _OnBtnNoSelectClick();
		partial void _OnBtnExportClick();
		partial void _OnInputSearchChanged(ChangeEvent<string> e);
		partial void _OnBtnSearchClearClick();
		partial void _OnMakeListViewItem(VisualElement e);
		partial void _OnBindListViewItem(VisualElement e,int index);
		partial void _OnListViewItemClick(IEnumerable<object> objs);
		partial void _OnBtnRemoveClick();
		partial void _OnBtnAddClick();
		partial void _OnTxtPathChanged(ChangeEvent<string> e);
		partial void _OnBtnCodePathClick();
		partial void _OnBtnChangeGroupClick();
		partial void _OnTgMuteChanged(ChangeEvent<bool> e);
		partial void _OnTxtPriChanged(ChangeEvent<int> e);
		partial void _OnTxtClassChanged(ChangeEvent<string> e);
		partial void _OnTxtNameChanged(ChangeEvent<string> e);
		partial void _OnTxtDescChanged(ChangeEvent<string> e);
		partial void _OnBtnAddConditionClick();
		partial void _OnTxtStateCreateNameChanged(ChangeEvent<string> e);
		partial void _OnBtnStateAddClick();
		partial void _OnTxtStateGroupChanged(ChangeEvent<string> e);
		partial void _OnBtnStateCreateClick();
		partial void _OnBtnStateCancelClick();
		partial void _OnEnumConditionChanged(ChangeEvent<Enum> e);
		partial void _OnBtnConditionCreateClick();
		partial void _OnBtnConditionCancelClick();
	}
}
