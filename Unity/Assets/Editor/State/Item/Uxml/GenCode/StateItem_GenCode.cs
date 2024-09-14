//自动生成的代码，请勿修改!!!
//CodeGen By [Assets/UIElements/CodeGenByUxml]
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
namespace Ux.Editor.State.Item
{
	public partial class StateItem
	{
		protected VisualElement root;
		public EnumField validType;
		public Button btnAdd;
		public VisualElement content;
		protected void CreateChildren()
		{
			var _visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/State/Item/Uxml/StateItem.uxml");
			if (_visualAsset == null) return;
			root = _visualAsset.CloneTree();
			root.style.flexGrow = 1f;
			validType =root.Q<EnumField>("validType");
			validType.RegisterValueChangedCallback(e => _OnValidTypeChanged(e));
			btnAdd =root.Q<Button>("btnAdd");
			btnAdd.clicked += () => _OnBtnAddClick();
			content =root.Q<VisualElement>("content");
		}
		partial void _OnValidTypeChanged(ChangeEvent<Enum> e);
		partial void _OnBtnAddClick();
	}
}
