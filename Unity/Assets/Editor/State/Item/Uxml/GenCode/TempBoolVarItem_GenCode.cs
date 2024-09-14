//自动生成的代码，请勿修改!!!
//CodeGen By [Assets/UIElements/CodeGenByUxml]
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
namespace Ux.Editor.State.Item
{
	public partial class TempBoolVarItem
	{
		protected VisualElement root;
		public TextField txtVar;
		protected void CreateChildren()
		{
			var _visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/State/Item/Uxml/TempBoolVarItem.uxml");
			if (_visualAsset == null) return;
			root = _visualAsset.CloneTree();
			root.style.flexGrow = 1f;
			txtVar =root.Q<TextField>("txtVar");
			txtVar.RegisterValueChangedCallback(e => _OnTxtVarChanged(e));
		}
		partial void _OnTxtVarChanged(ChangeEvent<string> e);
	}
}
