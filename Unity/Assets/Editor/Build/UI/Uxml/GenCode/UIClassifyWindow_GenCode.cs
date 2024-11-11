//自动生成的代码，请勿修改!!!
//CodeGen By [Assets/UIElements/CodeGenByUxml]
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
namespace Ux.Editor.Build.UI
{
	public partial class UIClassifyWindow
	{
		protected VisualElement root;
		public ObjectField pathField;
		public Button btnLoad;
		public Button btnBuildinAdd;
		public VisualElement buildin;
		public Button btnLazyloadAdd;
		public VisualElement lazyload;
		public Button btnApply;
		protected void CreateChildren()
		{
			var _visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Build/UI/Uxml/UIClassifyWindow.uxml");
			if (_visualAsset == null) return;
			root = _visualAsset.CloneTree();
			root.style.flexGrow = 1f;
			pathField = root.Q<ObjectField>("pathField");
			pathField.RegisterValueChangedCallback(e => _OnPathFieldChanged(e));
			btnLoad = root.Q<Button>("btnLoad");
			btnLoad.clicked += () => _OnBtnLoadClick();
			btnBuildinAdd = root.Q<Button>("btnBuildinAdd");
			btnBuildinAdd.clicked += () => _OnBtnBuildinAddClick();
			buildin = root.Q<VisualElement>("buildin");
			btnLazyloadAdd = root.Q<Button>("btnLazyloadAdd");
			btnLazyloadAdd.clicked += () => _OnBtnLazyloadAddClick();
			lazyload = root.Q<VisualElement>("lazyload");
			btnApply = root.Q<Button>("btnApply");
			btnApply.clicked += () => _OnBtnApplyClick();
		}
		partial void _OnPathFieldChanged(ChangeEvent<UnityEngine.Object> e);
		partial void _OnBtnLoadClick();
		partial void _OnBtnBuildinAddClick();
		partial void _OnBtnLazyloadAddClick();
		partial void _OnBtnApplyClick();
	}
}
