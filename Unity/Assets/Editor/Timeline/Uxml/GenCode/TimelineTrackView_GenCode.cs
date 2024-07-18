//自动生成的代码，请勿修改!!!
//CodeGen By [Assets/UIElements/CodeGenByUxml]
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
namespace Ux.Editor.Timeline
{
	public partial class TimelineTrackView
	{
		protected VisualElement root;
		public ToolbarMenu btnAddTrack;
		public VisualElement trackContent;
		protected void CreateChildren()
		{
			var _visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Timeline/Uxml/TimelineTrackView.uxml");
			if (_visualAsset == null) return;
			root = _visualAsset.CloneTree();
			root.style.flexGrow = 1f;
			btnAddTrack =root.Q<ToolbarMenu>("btnAddTrack");
			trackContent =root.Q<VisualElement>("trackContent");
		}
	}
}
