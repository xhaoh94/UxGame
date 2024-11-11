//自动生成的代码，请勿修改!!!
//CodeGen By [Assets/UIElements/CodeGenByUxml]
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
namespace Ux.Editor.Build.Version
{
	public partial class VersionPackageViewer
	{
		protected VisualElement root;
		public VisualElement exportElement;
		public Toggle tgCollectSV;
		public EnumField pipelineType;
		public EnumField nameStyleType;
		public EnumField compressionType;
		public TextField inputBuiltinTags;
		public VisualElement encryptionContainer;
		protected void CreateChildren()
		{
			var _visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Build/Version/Uxml/VersionPackageViewer.uxml");
			if (_visualAsset == null) return;
			root = _visualAsset.CloneTree();
			root.style.flexGrow = 1f;
			exportElement = root.Q<VisualElement>("exportElement");
			tgCollectSV = root.Q<Toggle>("tgCollectSV");
			tgCollectSV.RegisterValueChangedCallback(e => _OnTgCollectSVChanged(e));
			pipelineType = root.Q<EnumField>("pipelineType");
			pipelineType.RegisterValueChangedCallback(e => _OnPipelineTypeChanged(e));
			nameStyleType = root.Q<EnumField>("nameStyleType");
			nameStyleType.RegisterValueChangedCallback(e => _OnNameStyleTypeChanged(e));
			compressionType = root.Q<EnumField>("compressionType");
			compressionType.RegisterValueChangedCallback(e => _OnCompressionTypeChanged(e));
			inputBuiltinTags = root.Q<TextField>("inputBuiltinTags");
			inputBuiltinTags.RegisterValueChangedCallback(e => _OnInputBuiltinTagsChanged(e));
			encryptionContainer = root.Q<VisualElement>("encryptionContainer");
		}
		partial void _OnTgCollectSVChanged(ChangeEvent<bool> e);
		partial void _OnPipelineTypeChanged(ChangeEvent<Enum> e);
		partial void _OnNameStyleTypeChanged(ChangeEvent<Enum> e);
		partial void _OnCompressionTypeChanged(ChangeEvent<Enum> e);
		partial void _OnInputBuiltinTagsChanged(ChangeEvent<string> e);
	}
}
