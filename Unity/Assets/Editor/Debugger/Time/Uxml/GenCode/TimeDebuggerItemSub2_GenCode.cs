//自动生成的代码，请勿修改!!!
//CodeGen By [Assets/UIElements/CodeGenByUxml]
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
namespace Ux.Editor.Debugger.Time
{
	public partial class TimeDebuggerItemSub2
	{
		protected VisualElement root;
		public TextField txtKey;
		public TextField txtCorn;
		public TextField txtTimeDesc;
		public TextField txtTimeStamp;
		protected void CreateChildren()
		{
			var _visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Debugger/Time/Uxml/TimeDebuggerItemSub2.uxml");
			if (_visualAsset == null) return;
			root = _visualAsset.CloneTree();
			root.style.flexGrow = 1f;
			txtKey = root.Q<TextField>("txtKey");
			txtKey.RegisterValueChangedCallback(e => _OnTxtKeyChanged(e));
			txtCorn = root.Q<TextField>("txtCorn");
			txtCorn.RegisterValueChangedCallback(e => _OnTxtCornChanged(e));
			txtTimeDesc = root.Q<TextField>("txtTimeDesc");
			txtTimeDesc.RegisterValueChangedCallback(e => _OnTxtTimeDescChanged(e));
			txtTimeStamp = root.Q<TextField>("txtTimeStamp");
			txtTimeStamp.RegisterValueChangedCallback(e => _OnTxtTimeStampChanged(e));
		}
		partial void _OnTxtKeyChanged(ChangeEvent<string> e);
		partial void _OnTxtCornChanged(ChangeEvent<string> e);
		partial void _OnTxtTimeDescChanged(ChangeEvent<string> e);
		partial void _OnTxtTimeStampChanged(ChangeEvent<string> e);
	}
}
