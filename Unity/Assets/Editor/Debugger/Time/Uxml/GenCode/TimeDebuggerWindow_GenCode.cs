//自动生成的代码，请勿修改!!!
//CodeGen By [Assets/UIElements/CodeGenByUxml]
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
namespace Ux.Editor.Debugger.Time
{
	public partial class TimeDebuggerWindow
	{
		protected VisualElement root;
		public TextField txtLocalTime;
		public TextField txtServerTime;
		public TextField txtTime;
		public Label Label;
		public TextField txtFrame;
		public ToolbarButton tbBtnTime;
		public ToolbarButton tbBtnFrame;
		public ToolbarButton tbBtnTimeStamp;
		public ToolbarButton tbBtnCron;
		public ScrollView scr;
		protected void CreateChildren()
		{
			var _visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Debugger/Time/Uxml/TimeDebuggerWindow.uxml");
			if (_visualAsset == null) return;
			root = _visualAsset.CloneTree();
			root.style.flexGrow = 1f;
			txtLocalTime = root.Q<TextField>("txtLocalTime");
			txtServerTime = root.Q<TextField>("txtServerTime");
			txtTime = root.Q<TextField>("txtTime");
			Label = root.Q<Label>("Label");
			txtFrame = root.Q<TextField>("txtFrame");
			tbBtnTime = root.Q<ToolbarButton>("tbBtnTime");
			//tbBtnTime.clicked += () => _OnTbBtnTimeClick();
			tbBtnFrame = root.Q<ToolbarButton>("tbBtnFrame");
			//tbBtnFrame.clicked += () => _OnTbBtnFrameClick();
			tbBtnTimeStamp = root.Q<ToolbarButton>("tbBtnTimeStamp");
			//tbBtnTimeStamp.clicked += () => _OnTbBtnTimeStampClick();
			tbBtnCron = root.Q<ToolbarButton>("tbBtnCron");
			//tbBtnCron.clicked += () => _OnTbBtnCronClick();
			scr = root.Q<ScrollView>("scr");
		}
	}
}
