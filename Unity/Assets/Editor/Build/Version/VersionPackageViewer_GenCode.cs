//自动生成的代码，请勿修改!!!
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
		protected VisualElement exportElement;
		protected Toggle tgCollectSV;
		protected EnumField pipelineType;
		protected EnumField nameStyleType;
		protected EnumField compressionType;
		protected TextField inputBuiltinTags;
		protected VisualElement encryptionContainer;
		protected void CreateChildren()
		{
			var _visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Build/Version/VersionPackageViewer.uxml");
			if (_visualAsset == null) return;
			root = _visualAsset.CloneTree();
			exportElement =root.Q<VisualElement>("exportElement");
			tgCollectSV =root.Q<Toggle>("tgCollectSV");
			tgCollectSV.RegisterValueChangedCallback(e => _OnTgCollectSVChanged(e));
			pipelineType =root.Q<EnumField>("pipelineType");
			pipelineType.RegisterValueChangedCallback(e => _OnPipelineTypeChanged(e));
			nameStyleType =root.Q<EnumField>("nameStyleType");
			nameStyleType.RegisterValueChangedCallback(e => _OnNameStyleTypeChanged(e));
			compressionType =root.Q<EnumField>("compressionType");
			compressionType.RegisterValueChangedCallback(e => _OnCompressionTypeChanged(e));
			inputBuiltinTags =root.Q<TextField>("inputBuiltinTags");
			inputBuiltinTags.RegisterValueChangedCallback(e => _OnInputBuiltinTagsChanged(e));
			encryptionContainer =root.Q<VisualElement>("encryptionContainer");
		}
		partial void _OnTgCollectSVChanged(ChangeEvent<bool> e);
		partial void _OnPipelineTypeChanged(ChangeEvent<Enum> e);
		partial void _OnNameStyleTypeChanged(ChangeEvent<Enum> e);
		partial void _OnCompressionTypeChanged(ChangeEvent<Enum> e);
		partial void _OnInputBuiltinTagsChanged(ChangeEvent<string> e);
	}
}
