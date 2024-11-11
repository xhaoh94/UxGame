//自动生成的代码，请勿修改!!!
//CodeGen By [Assets/UIElements/CodeGenByUxml]
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
namespace Ux.Editor.Debugger.Time
{
	public partial class TimeSearchView
	{
		protected VisualElement root;
		public VisualElement veContent;
		public TextField inputSearch;
		public Button btnClear;
		public Toolbar TopBar;
		public ToolbarButton TopBar0;
		public ToolbarButton TopBar1;
		public ListView list;
		public VisualElement vePage;
		public Button btnLast;
		public IntegerField inputPage;
		public Label txtPage;
		public Button btnNext;
		protected void CreateChildren()
		{
			var _visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Debugger/Time/Uxml/TimeSearchView.uxml");
			if (_visualAsset == null) return;
			root = _visualAsset.CloneTree();
			root.style.flexGrow = 1f;
			veContent = root.Q<VisualElement>("veContent");
			inputSearch = root.Q<TextField>("inputSearch");
			inputSearch.RegisterValueChangedCallback(e => _OnInputSearchChanged(e));
			btnClear = root.Q<Button>("btnClear");
			btnClear.clicked += () => _OnBtnClearClick();
			TopBar = root.Q<Toolbar>("TopBar");
			TopBar0 = root.Q<ToolbarButton>("TopBar0");
			//TopBar0.clicked += () => _OnTopBar0Click();
			TopBar1 = root.Q<ToolbarButton>("TopBar1");
			//TopBar1.clicked += () => _OnTopBar1Click();
			list = root.Q<ListView>("list");
			list.makeItem = ()=> { var e = new VisualElement(); _OnMakeListItem(e); return e; };
			list.bindItem = (e,i)=> _OnBindListItem(e,i);
			#if UNITY_2022_1_OR_NEWER
			list.selectionChanged += e => _OnListItemClick(e);
			#else
			list.onSelectionChange += e => _OnListItemClick(e);
			#endif
			vePage = root.Q<VisualElement>("vePage");
			btnLast = root.Q<Button>("btnLast");
			btnLast.clicked += () => _OnBtnLastClick();
			inputPage = root.Q<IntegerField>("inputPage");
			inputPage.RegisterValueChangedCallback(e => _OnInputPageChanged(e));
			txtPage = root.Q<Label>("txtPage");
			btnNext = root.Q<Button>("btnNext");
			btnNext.clicked += () => _OnBtnNextClick();
		}
		partial void _OnInputSearchChanged(ChangeEvent<string> e);
		partial void _OnBtnClearClick();
		partial void _OnMakeListItem(VisualElement e);
		partial void _OnBindListItem(VisualElement e,int index);
		partial void _OnListItemClick(IEnumerable<object> objs);
		partial void _OnBtnLastClick();
		partial void _OnInputPageChanged(ChangeEvent<int> e);
		partial void _OnBtnNextClick();
	}
}
