//自动生成的代码，请勿修改!!!
//CodeGen By [Assets/UIElements/CodeGenByUxml]
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
namespace Ux.Editor.State.Item
{
	public partial class CustomItem
	{
		protected VisualElement root;
		public TextField txtName;
		public TextField txtValue;
		protected void CreateChildren()
		{
			var _visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/State/Item/Uxml/CustomItem.uxml");
			if (_visualAsset == null) return;
			root = _visualAsset.CloneTree();
			root.style.flexGrow = 1f;
			txtName =root.Q<TextField>("txtName");
			txtName.RegisterValueChangedCallback(e => _OnTxtNameChanged(e));
			txtValue =root.Q<TextField>("txtValue");
			txtValue.RegisterValueChangedCallback(e => _OnTxtValueChanged(e));
		}
		partial void _OnTxtNameChanged(ChangeEvent<string> e);
		partial void _OnTxtValueChanged(ChangeEvent<string> e);
	}
}
