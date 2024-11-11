//自动生成的代码，请勿修改!!!
//CodeGen By [Assets/UIElements/CodeGenByUxml]
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
namespace Ux.Editor.Build.UI
{
	public partial class UICodeMemberItem
	{
		protected VisualElement root;
		public TextField txtName;
		public TextField txtType;
		public TextField txtCustomType;
		public TextField txtRes;
		public VisualElement VisualElement;
		public Toggle tgExport;
		public Toggle tgCreate;
		public VisualElement evt;
		public VisualElement doubleEvt;
		public IntegerField dCnt;
		public FloatField dGapTime;
		public VisualElement longEvt;
		public FloatField lFirst;
		public FloatField lGapTime;
		public IntegerField lCnt;
		public FloatField lRadius;
		protected void CreateChildren()
		{
			var _visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Build/UI/Uxml/UICodeMemberItem.uxml");
			if (_visualAsset == null) return;
			root = _visualAsset.CloneTree();
			root.style.flexGrow = 1f;
			txtName = root.Q<TextField>("txtName");
			txtType = root.Q<TextField>("txtType");
			txtCustomType = root.Q<TextField>("txtCustomType");
			txtRes = root.Q<TextField>("txtRes");
			VisualElement = root.Q<VisualElement>("VisualElement");
			tgExport = root.Q<Toggle>("tgExport");
			tgExport.RegisterValueChangedCallback(e => _OnTgExportChanged(e));
			tgCreate = root.Q<Toggle>("tgCreate");
			tgCreate.RegisterValueChangedCallback(e => _OnTgCreateChanged(e));
			evt = root.Q<VisualElement>("evt");
			doubleEvt = root.Q<VisualElement>("doubleEvt");
			dCnt = root.Q<IntegerField>("dCnt");
			dCnt.RegisterValueChangedCallback(e => _OnDCntChanged(e));
			dGapTime = root.Q<FloatField>("dGapTime");
			dGapTime.RegisterValueChangedCallback(e => _OnDGapTimeChanged(e));
			longEvt = root.Q<VisualElement>("longEvt");
			lFirst = root.Q<FloatField>("lFirst");
			lFirst.RegisterValueChangedCallback(e => _OnLFirstChanged(e));
			lGapTime = root.Q<FloatField>("lGapTime");
			lGapTime.RegisterValueChangedCallback(e => _OnLGapTimeChanged(e));
			lCnt = root.Q<IntegerField>("lCnt");
			lCnt.RegisterValueChangedCallback(e => _OnLCntChanged(e));
			lRadius = root.Q<FloatField>("lRadius");
			lRadius.RegisterValueChangedCallback(e => _OnLRadiusChanged(e));
		}
		partial void _OnTgExportChanged(ChangeEvent<bool> e);
		partial void _OnTgCreateChanged(ChangeEvent<bool> e);
		partial void _OnDCntChanged(ChangeEvent<int> e);
		partial void _OnDGapTimeChanged(ChangeEvent<float> e);
		partial void _OnLFirstChanged(ChangeEvent<float> e);
		partial void _OnLGapTimeChanged(ChangeEvent<float> e);
		partial void _OnLCntChanged(ChangeEvent<int> e);
		partial void _OnLRadiusChanged(ChangeEvent<float> e);
	}
}
