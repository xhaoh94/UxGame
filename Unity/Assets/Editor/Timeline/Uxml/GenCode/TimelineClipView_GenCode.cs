//自动生成的代码，请勿修改!!!
//CodeGen By [Assets/UIElements/CodeGenByUxml]
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
namespace Ux.Editor.Timeline
{
	public partial class TimelineClipView
	{
		protected VisualElement root;
		public ScrollView scrClipView;
		public Toolbar Toolbar;
		public VisualElement veLineContent;
		public VisualElement veClipContent;
		public VisualElement veMarkerContent;
		public VisualElement veMarkerIcon;
		public Label lbMarker;
		public VisualElement veInspector;
		protected void CreateChildren()
		{
			var _visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Timeline/Uxml/TimelineClipView.uxml");
			if (_visualAsset == null) return;
			root = _visualAsset.CloneTree();
			root.style.flexGrow = 1f;
			scrClipView =root.Q<ScrollView>("scrClipView");
			Toolbar =root.Q<Toolbar>("Toolbar");
			veLineContent =root.Q<VisualElement>("veLineContent");
			veClipContent =root.Q<VisualElement>("veClipContent");
			veMarkerContent =root.Q<VisualElement>("veMarkerContent");
			veMarkerIcon =root.Q<VisualElement>("veMarkerIcon");
			lbMarker =root.Q<Label>("lbMarker");
            veInspector = root.Q<VisualElement>("veInspector");
		}
	}
}
