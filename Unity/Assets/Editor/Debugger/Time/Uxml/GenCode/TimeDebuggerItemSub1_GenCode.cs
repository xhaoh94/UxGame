//自动生成的代码，请勿修改!!!
//CodeGen By [Assets/UIElements/CodeGenByUxml]
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
namespace Ux.Editor.Debugger.Time
{
	public partial class TimeDebuggerItemSub1
	{
		protected VisualElement root;
		public TextField txtKey;
		public Toggle tgLoop;
		public TextField txtTotaCnt;
		public TextField txtExeCnt;
		public TextField txtNext;
		public TextField txtGap;
		public Label lbType;
		protected void CreateChildren()
		{
			var _visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Debugger/Time/Uxml/TimeDebuggerItemSub1.uxml");
			if (_visualAsset == null) return;
			root = _visualAsset.CloneTree();
			root.style.flexGrow = 1f;
			txtKey = root.Q<TextField>("txtKey");
			tgLoop = root.Q<Toggle>("tgLoop");
			tgLoop.RegisterValueChangedCallback(e => _OnTgLoopChanged(e));
			txtTotaCnt = root.Q<TextField>("txtTotaCnt");
			txtExeCnt = root.Q<TextField>("txtExeCnt");
			txtNext = root.Q<TextField>("txtNext");
			txtGap = root.Q<TextField>("txtGap");
			lbType = root.Q<Label>("lbType");
		}
		partial void _OnTgLoopChanged(ChangeEvent<bool> e);
	}
}
