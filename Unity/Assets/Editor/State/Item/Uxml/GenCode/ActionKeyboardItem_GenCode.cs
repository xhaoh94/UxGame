//自动生成的代码，请勿修改!!!
//CodeGen By [Assets/UIElements/CodeGenByUxml]
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
namespace Ux.Editor.State.Item
{
	public partial class ActionKeyboardItem
	{
		protected VisualElement root;
		public EnumField enumKey;
		public EnumField enumType;
		protected void CreateChildren()
		{
			var _visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/State/Item/Uxml/ActionKeyboardItem.uxml");
			if (_visualAsset == null) return;
			root = _visualAsset.CloneTree();
			root.style.flexGrow = 1f;
			enumKey =root.Q<EnumField>("enumKey");
			enumKey.RegisterValueChangedCallback(e => _OnEnumKeyChanged(e));
			enumType =root.Q<EnumField>("enumType");
			enumType.RegisterValueChangedCallback(e => _OnEnumTypeChanged(e));
		}
		partial void _OnEnumKeyChanged(ChangeEvent<Enum> e);
		partial void _OnEnumTypeChanged(ChangeEvent<Enum> e);
	}
}
