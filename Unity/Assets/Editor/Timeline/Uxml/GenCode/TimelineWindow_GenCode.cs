//自动生成的代码，请勿修改!!!
//CodeGen By [Assets/UIElements/CodeGenByUxml]
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
namespace Ux.Editor.Timeline
{
	public partial class TimelineWindow
	{
		protected VisualElement root;
		public VisualElement VisualElement;
		public TimelineTrackView trackView;
		public TimelineClipView clipView;
		public ObjectField ofEntity;
		public ObjectField ofTimeline;
		public Button btnCreate;
		public VisualElement createView;
		public TextField inputPath;
		public Button btnPath;
		public TextField inputName;
		public Button btnOk;
		public Button btnPlay;
		public Button btnPause;
		protected void CreateChildren()
		{
			var _visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Timeline/Uxml/TimelineWindow.uxml");
			if (_visualAsset == null) return;
			root = _visualAsset.CloneTree();
			root.style.flexGrow = 1f;
			VisualElement =root.Q<VisualElement>("VisualElement");
			trackView =root.Q<TimelineTrackView>("trackView");
			clipView =root.Q<TimelineClipView>("clipView");
			ofEntity =root.Q<ObjectField>("ofEntity");
			ofEntity.RegisterValueChangedCallback(e => _OnOfEntityChanged(e));
			ofTimeline =root.Q<ObjectField>("ofTimeline");
			ofTimeline.RegisterValueChangedCallback(e => _OnOfTimelineChanged(e));
			btnCreate =root.Q<Button>("btnCreate");
			btnCreate.clicked += () => _OnBtnCreateClick();
			createView =root.Q<VisualElement>("createView");
			inputPath =root.Q<TextField>("inputPath");
			inputPath.RegisterValueChangedCallback(e => _OnInputPathChanged(e));
			btnPath =root.Q<Button>("btnPath");
			btnPath.clicked += () => _OnBtnPathClick();
			inputName =root.Q<TextField>("inputName");
			inputName.RegisterValueChangedCallback(e => _OnInputNameChanged(e));
			btnOk =root.Q<Button>("btnOk");
			btnOk.clicked += () => _OnBtnOkClick();
			btnPlay =root.Q<Button>("btnPlay");
			btnPlay.clicked += () => _OnBtnPlayClick();
			btnPause =root.Q<Button>("btnPause");
			btnPause.clicked += () => _OnBtnPauseClick();
		}
		partial void _OnOfEntityChanged(ChangeEvent<UnityEngine.Object> e);
		partial void _OnOfTimelineChanged(ChangeEvent<UnityEngine.Object> e);
		partial void _OnBtnCreateClick();
		partial void _OnInputPathChanged(ChangeEvent<string> e);
		partial void _OnBtnPathClick();
		partial void _OnInputNameChanged(ChangeEvent<string> e);
		partial void _OnBtnOkClick();
		partial void _OnBtnPlayClick();
		partial void _OnBtnPauseClick();
	}
}
