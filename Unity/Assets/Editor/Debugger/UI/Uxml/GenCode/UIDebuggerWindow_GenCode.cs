//自动生成的代码，请勿修改!!!
//CodeGen By [Assets/UIElements/CodeGenByUxml]
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
namespace Ux.Editor.Debugger.UI
{
	public partial class UIDebuggerWindow
	{
		protected VisualElement root;
		public ListView listStack;
		public ListView listShowed;
		public ListView listShowing;
		public ListView listCacel;
		public ListView listTemCacel;
		public ListView listWaitDel;
		public VisualElement veList;
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
			var _visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Debugger/UI/Uxml/UIDebuggerWindow.uxml");
			if (_visualAsset == null) return;
			root = _visualAsset.CloneTree();
			root.style.flexGrow = 1f;
			listStack = root.Q<ListView>("listStack");
			listStack.makeItem = ()=> { var e = new VisualElement(); _OnMakeListStackItem(e); return e; };
			listStack.bindItem = (e,i)=> _OnBindListStackItem(e,i);
			#if UNITY_2022_1_OR_NEWER
			listStack.selectionChanged += e => _OnListStackItemClick(e);
			#else
			listStack.onSelectionChange += e => _OnListStackItemClick(e);
			#endif
			listShowed = root.Q<ListView>("listShowed");
			listShowed.makeItem = ()=> { var e = new VisualElement(); _OnMakeListShowedItem(e); return e; };
			listShowed.bindItem = (e,i)=> _OnBindListShowedItem(e,i);
			#if UNITY_2022_1_OR_NEWER
			listShowed.selectionChanged += e => _OnListShowedItemClick(e);
			#else
			listShowed.onSelectionChange += e => _OnListShowedItemClick(e);
			#endif
			listShowing = root.Q<ListView>("listShowing");
			listShowing.makeItem = ()=> { var e = new VisualElement(); _OnMakeListShowingItem(e); return e; };
			listShowing.bindItem = (e,i)=> _OnBindListShowingItem(e,i);
			#if UNITY_2022_1_OR_NEWER
			listShowing.selectionChanged += e => _OnListShowingItemClick(e);
			#else
			listShowing.onSelectionChange += e => _OnListShowingItemClick(e);
			#endif
			listCacel = root.Q<ListView>("listCacel");
			listCacel.makeItem = ()=> { var e = new VisualElement(); _OnMakeListCacelItem(e); return e; };
			listCacel.bindItem = (e,i)=> _OnBindListCacelItem(e,i);
			#if UNITY_2022_1_OR_NEWER
			listCacel.selectionChanged += e => _OnListCacelItemClick(e);
			#else
			listCacel.onSelectionChange += e => _OnListCacelItemClick(e);
			#endif
			listTemCacel = root.Q<ListView>("listTemCacel");
			listTemCacel.makeItem = ()=> { var e = new VisualElement(); _OnMakeListTemCacelItem(e); return e; };
			listTemCacel.bindItem = (e,i)=> _OnBindListTemCacelItem(e,i);
			#if UNITY_2022_1_OR_NEWER
			listTemCacel.selectionChanged += e => _OnListTemCacelItemClick(e);
			#else
			listTemCacel.onSelectionChange += e => _OnListTemCacelItemClick(e);
			#endif
			listWaitDel = root.Q<ListView>("listWaitDel");
			listWaitDel.makeItem = ()=> { var e = new VisualElement(); _OnMakeListWaitDelItem(e); return e; };
			listWaitDel.bindItem = (e,i)=> _OnBindListWaitDelItem(e,i);
			#if UNITY_2022_1_OR_NEWER
			listWaitDel.selectionChanged += e => _OnListWaitDelItemClick(e);
			#else
			listWaitDel.onSelectionChange += e => _OnListWaitDelItemClick(e);
			#endif
			veList = root.Q<VisualElement>("veList");
			inputSearch = root.Q<TextField>("inputSearch");
			inputSearch.RegisterValueChangedCallback(e => _OnInputSearchChanged(e));
			btnClear = root.Q<Button>("btnClear");
			btnClear.clicked += () => _OnBtnClearClick();
			TopBar = root.Q<Toolbar>("TopBar");
			TopBar0 = root.Q<ToolbarButton>("TopBar0");
			TopBar1 = root.Q<ToolbarButton>("TopBar1");
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
		partial void _OnMakeListStackItem(VisualElement e);
		partial void _OnBindListStackItem(VisualElement e,int index);
		partial void _OnListStackItemClick(IEnumerable<object> objs);
		partial void _OnMakeListShowedItem(VisualElement e);
		partial void _OnBindListShowedItem(VisualElement e,int index);
		partial void _OnListShowedItemClick(IEnumerable<object> objs);
		partial void _OnMakeListShowingItem(VisualElement e);
		partial void _OnBindListShowingItem(VisualElement e,int index);
		partial void _OnListShowingItemClick(IEnumerable<object> objs);
		partial void _OnMakeListCacelItem(VisualElement e);
		partial void _OnBindListCacelItem(VisualElement e,int index);
		partial void _OnListCacelItemClick(IEnumerable<object> objs);
		partial void _OnMakeListTemCacelItem(VisualElement e);
		partial void _OnBindListTemCacelItem(VisualElement e,int index);
		partial void _OnListTemCacelItemClick(IEnumerable<object> objs);
		partial void _OnMakeListWaitDelItem(VisualElement e);
		partial void _OnBindListWaitDelItem(VisualElement e,int index);
		partial void _OnListWaitDelItemClick(IEnumerable<object> objs);
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
