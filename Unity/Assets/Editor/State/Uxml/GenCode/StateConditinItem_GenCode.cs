﻿//自动生成的代码，请勿修改!!!
//CodeGen By [Assets/UIElements/CodeGenByUxml]
using System;
using UnityEditor;
using UnityEngine.UIElements;
namespace Ux.Editor.State
{
    public partial class StateConditinItem
	{
		protected VisualElement root;
		public EnumField conditionType;
		public VisualElement content;
		protected void CreateChildren()
		{
			var _visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/State/Uxml/StateConditinItem.uxml");
			if (_visualAsset == null) return;
			root = _visualAsset.CloneTree();
			root.style.flexGrow = 1f;
			conditionType =root.Q<EnumField>("conditionType");
			conditionType.RegisterValueChangedCallback(e => _OnConditionTypeChanged(e));
			content =root.Q<VisualElement>("content");
		}
		partial void _OnConditionTypeChanged(ChangeEvent<Enum> e);
	}
}
