//自动生成的代码，请勿修改!!!
//CodeGen By [Assets/UIElements/CodeGenByUxml]
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
namespace Ux.Editor.Timeline.Animation
{
	public partial class TLAnimTrackInspector
	{
		protected VisualElement root;
		public TextField txtName;
		public IntegerField inputLayer;
		public ObjectField ofAvatarMask;
		public Toggle tgAdditive;
		protected void CreateChildren()
		{
			var _visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Timeline/Uxml/Animation/TLAnimTrackInspector.uxml");
			if (_visualAsset == null) return;
			root = _visualAsset.CloneTree();
			root.style.flexGrow = 1f;
			txtName =root.Q<TextField>("txtName");
			txtName.RegisterValueChangedCallback(e => _OnTxtNameChanged(e));
			inputLayer =root.Q<IntegerField>("inputLayer");
			inputLayer.RegisterValueChangedCallback(e => _OnInputLayerChanged(e));
			ofAvatarMask =root.Q<ObjectField>("ofAvatarMask");
			ofAvatarMask.RegisterValueChangedCallback(e => _OnOfAvatarMaskChanged(e));
			tgAdditive =root.Q<Toggle>("tgAdditive");
			tgAdditive.RegisterValueChangedCallback(e => _OnTgAdditiveChanged(e));
		}
		partial void _OnTxtNameChanged(ChangeEvent<string> e);
		partial void _OnInputLayerChanged(ChangeEvent<int> e);
		partial void _OnOfAvatarMaskChanged(ChangeEvent<UnityEngine.Object> e);
		partial void _OnTgAdditiveChanged(ChangeEvent<bool> e);
	}
}
