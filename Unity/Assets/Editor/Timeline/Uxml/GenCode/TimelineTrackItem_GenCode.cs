﻿//自动生成的代码，请勿修改!!!
//CodeGen By [Assets/UIElements/CodeGenByUxml]
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
namespace Ux.Editor.Timeline
{
	public partial class TimelineTrackItem
	{
		protected VisualElement root;
		public VisualElement content;
		public Label lbType;
		public TextField inputName;
		protected void CreateChildren()
		{
			var _visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Timeline/Uxml/TimelineTrackItem.uxml");
			if (_visualAsset == null) return;
			root = _visualAsset.CloneTree();
			root.style.flexGrow = 1f;
			content =root.Q<VisualElement>("content");
			lbType =root.Q<Label>("lbType");
			inputName =root.Q<TextField>("inputName");
			inputName.RegisterValueChangedCallback(e => _OnInputNameChanged(e));
		}
		partial void _OnInputNameChanged(ChangeEvent<string> e);
	}
}