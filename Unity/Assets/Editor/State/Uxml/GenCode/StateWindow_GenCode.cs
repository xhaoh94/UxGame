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
		public Button btnAddGroup;
		public VisualElement groupContent;
		public Button btnExport;
		public TextField inputSearch;
		public Button btnSearchClear;
		public ListView listView;
		public Button btnRemove;
		public Button btnAdd;
		public TextField txtPath;
		public Button btnCodePath;
		public TextField txtNs;
		public VisualElement infoView;
		public TextField txtGroup;
		public Toggle tgMute;
		public IntegerField txtPri;
		public TextField txtClass;
		public TextField txtName;
		public TextField txtDesc;
		public EnumField viewType;
		public ObjectField viewAnim;
		public ObjectField viewTimeline;
		public Button btnAddCondition;
		public VisualElement conditionContent;
		public VisualElement veStateCreate;
		public TextField txtStateCreateName;
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
			btnAddGroup =root.Q<Button>("btnAddGroup");
			btnAddGroup.clicked += () => _OnBtnAddGroupClick();
			groupContent =root.Q<VisualElement>("groupContent");
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
			txtNs =root.Q<TextField>("txtNs");
			txtNs.RegisterValueChangedCallback(e => _OnTxtNsChanged(e));
			infoView =root.Q<VisualElement>("infoView");
			txtGroup =root.Q<TextField>("txtGroup");
			txtGroup.RegisterValueChangedCallback(e => _OnTxtGroupChanged(e));
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
			viewType =root.Q<EnumField>("viewType");
			viewType.RegisterValueChangedCallback(e => _OnViewTypeChanged(e));
			viewAnim =root.Q<ObjectField>("viewAnim");
			viewAnim.RegisterValueChangedCallback(e => _OnViewAnimChanged(e));
			viewTimeline =root.Q<ObjectField>("viewTimeline");
			viewTimeline.RegisterValueChangedCallback(e => _OnViewTimelineChanged(e));
			btnAddCondition =root.Q<Button>("btnAddCondition");
			btnAddCondition.clicked += () => _OnBtnAddConditionClick();
			conditionContent =root.Q<VisualElement>("conditionContent");
			veStateCreate =root.Q<VisualElement>("veStateCreate");
			txtStateCreateName =root.Q<TextField>("txtStateCreateName");
			txtStateCreateName.RegisterValueChangedCallback(e => _OnTxtStateCreateNameChanged(e));
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
		partial void _OnBtnAddGroupClick();
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
		partial void _OnTxtNsChanged(ChangeEvent<string> e);
		partial void _OnTxtGroupChanged(ChangeEvent<string> e);
		partial void _OnTgMuteChanged(ChangeEvent<bool> e);
		partial void _OnTxtPriChanged(ChangeEvent<int> e);
		partial void _OnTxtClassChanged(ChangeEvent<string> e);
		partial void _OnTxtNameChanged(ChangeEvent<string> e);
		partial void _OnTxtDescChanged(ChangeEvent<string> e);
		partial void _OnViewTypeChanged(ChangeEvent<Enum> e);
		partial void _OnViewAnimChanged(ChangeEvent<UnityEngine.Object> e);
		partial void _OnViewTimelineChanged(ChangeEvent<UnityEngine.Object> e);
		partial void _OnBtnAddConditionClick();
		partial void _OnTxtStateCreateNameChanged(ChangeEvent<string> e);
		partial void _OnBtnStateCreateClick();
		partial void _OnBtnStateCancelClick();
		partial void _OnEnumConditionChanged(ChangeEvent<Enum> e);
		partial void _OnBtnConditionCreateClick();
		partial void _OnBtnConditionCancelClick();
	}
}
