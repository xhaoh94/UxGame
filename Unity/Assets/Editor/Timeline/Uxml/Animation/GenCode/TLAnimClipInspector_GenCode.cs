//自动生成的代码，请勿修改!!!
//CodeGen By [Assets/UIElements/CodeGenByUxml]
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
namespace Ux.Editor.Timeline.Animation
{
	public partial class TLAnimClipInspector
	{
		protected VisualElement root;
		public TextField txtName;
		public Toggle tgMove;
		public IntegerField txtStartFrame;
		public Label lbStartTime;
		public VisualElement VisualElement;
		public IntegerField txtEndFrame;
		public Label lbEndTime;
		public Label lbDurationFrame;
		public Label lbDurationTime;
		public Button btnDuration;
		public Label lbInFrame;
		public Label lbInTime;
		public Label lbOutFrame;
		public Label lbOutTime;
		public EnumField pre;
		public EnumField post;
		public ObjectField ofClip;
		protected void CreateChildren()
		{
			var _visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Timeline/Uxml/Animation/TLAnimClipInspector.uxml");
			if (_visualAsset == null) return;
			root = _visualAsset.CloneTree();
			root.style.flexGrow = 1f;
			txtName =root.Q<TextField>("txtName");
			txtName.RegisterValueChangedCallback(e => _OnTxtNameChanged(e));
			tgMove =root.Q<Toggle>("tgMove");
			tgMove.RegisterValueChangedCallback(e => _OnTgMoveChanged(e));
			txtStartFrame =root.Q<IntegerField>("txtStartFrame");
			txtStartFrame.RegisterValueChangedCallback(e => _OnTxtStartFrameChanged(e));
			lbStartTime =root.Q<Label>("lbStartTime");
			VisualElement =root.Q<VisualElement>("VisualElement");
			txtEndFrame =root.Q<IntegerField>("txtEndFrame");
			txtEndFrame.RegisterValueChangedCallback(e => _OnTxtEndFrameChanged(e));
			lbEndTime =root.Q<Label>("lbEndTime");
			lbDurationFrame =root.Q<Label>("lbDurationFrame");
			lbDurationTime =root.Q<Label>("lbDurationTime");
			btnDuration =root.Q<Button>("btnDuration");
			btnDuration.clicked += () => _OnBtnDurationClick();
			lbInFrame =root.Q<Label>("lbInFrame");
			lbInTime =root.Q<Label>("lbInTime");
			lbOutFrame =root.Q<Label>("lbOutFrame");
			lbOutTime =root.Q<Label>("lbOutTime");
			pre =root.Q<EnumField>("pre");
			pre.RegisterValueChangedCallback(e => _OnPreChanged(e));
			post =root.Q<EnumField>("post");
			post.RegisterValueChangedCallback(e => _OnPostChanged(e));
			ofClip =root.Q<ObjectField>("ofClip");
			ofClip.RegisterValueChangedCallback(e => _OnOfClipChanged(e));
		}
		partial void _OnTxtNameChanged(ChangeEvent<string> e);
		partial void _OnTgMoveChanged(ChangeEvent<bool> e);
		partial void _OnTxtStartFrameChanged(ChangeEvent<int> e);
		partial void _OnTxtEndFrameChanged(ChangeEvent<int> e);
		partial void _OnBtnDurationClick();
		partial void _OnPreChanged(ChangeEvent<Enum> e);
		partial void _OnPostChanged(ChangeEvent<Enum> e);
		partial void _OnOfClipChanged(ChangeEvent<UnityEngine.Object> e);
	}
}
