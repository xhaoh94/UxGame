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
		public FloatField txtStartTime;
		public IntegerField txtEndFrame;
		public FloatField txtEndTime;
		public Label lbDurationTime;
		public Label lbDurationFrame;
		public Button btnDuration;
		public Label lbInTime;
		public Label lbInFrame;
		public Label lbOutTime;
		public Label lbOutFrame;
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
			txtStartTime =root.Q<FloatField>("txtStartTime");
			txtEndFrame =root.Q<IntegerField>("txtEndFrame");
			txtEndFrame.RegisterValueChangedCallback(e => _OnTxtEndFrameChanged(e));
			txtEndTime =root.Q<FloatField>("txtEndTime");
			lbDurationTime =root.Q<Label>("lbDurationTime");
			lbDurationFrame =root.Q<Label>("lbDurationFrame");
			btnDuration =root.Q<Button>("btnDuration");
			btnDuration.clicked += () => _OnBtnDurationClick();
			lbInTime =root.Q<Label>("lbInTime");
			lbInFrame =root.Q<Label>("lbInFrame");
			lbOutTime =root.Q<Label>("lbOutTime");
			lbOutFrame =root.Q<Label>("lbOutFrame");
			ofClip =root.Q<ObjectField>("ofClip");
			ofClip.RegisterValueChangedCallback(e => _OnOfClipChanged(e));
		}
		partial void _OnTxtNameChanged(ChangeEvent<string> e);
		partial void _OnTgMoveChanged(ChangeEvent<bool> e);
		partial void _OnTxtStartFrameChanged(ChangeEvent<int> e);
		partial void _OnTxtEndFrameChanged(ChangeEvent<int> e);
		partial void _OnBtnDurationClick();
		partial void _OnOfClipChanged(ChangeEvent<UnityEngine.Object> e);
	}
}
