//自动生成的代码，请勿修改!!!
//CodeGen By [Assets/UIElements/CodeGenByUxml]
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
namespace Ux.Editor.Debugger.UI
{
	public partial class UIDebuggerItem
	{
		protected VisualElement root;
		public Label txtIDStr;
		public TextField txtID;
		public TextField txtType;
		public TextField txtPkgs;
		public TextField txtTags;
		public TextField txtChildrens;
		public TextField txtParID;
		public TextField txtParRedPoint;
		public TextField txtParTitle;
		protected void CreateChildren()
		{
			var _visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Debugger/UI/Uxml/UIDebuggerItem.uxml");
			if (_visualAsset == null) return;
			root = _visualAsset.CloneTree();
			root.style.flexGrow = 1f;
			txtIDStr = root.Q<Label>("txtIDStr");
			txtID = root.Q<TextField>("txtID");
			txtType = root.Q<TextField>("txtType");
			txtPkgs = root.Q<TextField>("txtPkgs");
			txtTags = root.Q<TextField>("txtTags");
			txtChildrens = root.Q<TextField>("txtChildrens");
			txtParID = root.Q<TextField>("txtParID");
			txtParRedPoint = root.Q<TextField>("txtParRedPoint");
			txtParTitle = root.Q<TextField>("txtParTitle");
		}
	}
}
