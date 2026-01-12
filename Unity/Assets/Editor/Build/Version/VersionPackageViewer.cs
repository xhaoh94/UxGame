using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;
using YooAsset;
using YooAsset.Editor;
namespace Ux.Editor.Build.Version
{
    partial class VersionPackageViewer
    {
        private List<Type> _encryptionServicesClassTypes;
        private List<string> _encryptionServicesClassNames;

        private List<Type> _manifestServicesClassTypes;
        private List<string> _manifestServicesClassNames;
        BuildPackageSetting PackageSetting;


        PopupField<string> _popupFieldEncryption;
        PopupField<string> _popupFieldManifest;
        public VersionPackageViewer(VisualElement parent)
        {
            CreateChildren();
            root.style.flexGrow = 1f;
            parent.Add(root);
            pipelineType.Init(EBuildPipeline.ScriptableBuildPipeline);
            nameStyleType.Init(EFileNameStyle.HashName);
            compressionType.Init(ECompressOption.LZ4);

            _encryptionServicesClassTypes = GetEncryptionServicesClassTypes();
            _encryptionServicesClassNames = _encryptionServicesClassTypes.Select(t => t.FullName).ToList();
            if (_encryptionServicesClassNames.Count > 0)
            {
                _popupFieldEncryption = new PopupField<string>(_encryptionServicesClassNames, 0);
                _popupFieldEncryption.label = "资源加密";
                _popupFieldEncryption.RegisterValueChangedCallback(evt =>
                {
                    PackageSetting.EncyptionClassName = evt.newValue;
                });
                encryptionContainer.Add(_popupFieldEncryption);
            }
            else
            {
                _popupFieldEncryption = new PopupField<string>();
                _popupFieldEncryption.label = "资源加密";
                encryptionContainer.Add(_popupFieldEncryption);
            }

            _manifestServicesClassTypes = GetManifestServicesClassTypes();
            _manifestServicesClassNames = _manifestServicesClassTypes.Select(t => t.FullName).ToList();
            if (_manifestServicesClassNames.Count > 0)
            {
                _popupFieldManifest = new PopupField<string>(_manifestServicesClassNames, 0);
                _popupFieldManifest.label = "清单加密";
                _popupFieldManifest.RegisterValueChangedCallback(evt =>
                {
                    PackageSetting.ManifestClassName = evt.newValue;
                });
                manifestContainer.Add(_popupFieldManifest);
            }
            else
            {
                _popupFieldManifest = new PopupField<string>();
                _popupFieldManifest.label = "清单加密";
                manifestContainer.Add(_popupFieldManifest);
            }
        }
        partial void _OnTgCollectSVChanged(ChangeEvent<bool> e)
        {
            PackageSetting.IsCollectShaderVariant = e.newValue;
        }
        partial void _OnPipelineTypeChanged(ChangeEvent<Enum> e)
        {
            PackageSetting.PiplineOption = e.newValue.ToString();
            RefreshElement();
        }
        partial void _OnCompressionTypeChanged(ChangeEvent<Enum> e)
        {
            PackageSetting.CompressOption = (ECompressOption)e.newValue;
        }       
        partial void _OnNameStyleTypeChanged(ChangeEvent<Enum> e)
        {
            PackageSetting.NameStyleOption = (EFileNameStyle)e.newValue;
        }
        partial void _OnInputBuiltinTagsChanged(ChangeEvent<string> e)
        {
            PackageSetting.BuildTags = e.newValue;
        }
     
        public void RefreshView(BuildPackageSetting packageSetting)
        {
            PackageSetting = packageSetting;
            tgCollectSV.SetValueWithoutNotify(packageSetting.IsCollectShaderVariant);
            pipelineType.SetValueWithoutNotify((EBuildPipeline)Enum.Parse(typeof(EBuildPipeline), PackageSetting.PiplineOption));
            nameStyleType.SetValueWithoutNotify(PackageSetting.NameStyleOption);
            compressionType.SetValueWithoutNotify(PackageSetting.CompressOption);
            inputBuiltinTags.SetValueWithoutNotify(PackageSetting.BuildTags);

            if (string.IsNullOrEmpty(PackageSetting.EncyptionClassName) &&
                 _encryptionServicesClassNames.Count > 0)
            {
                PackageSetting.EncyptionClassName = _encryptionServicesClassNames[0];
            }
            _popupFieldEncryption.SetValueWithoutNotify(PackageSetting.EncyptionClassName);

            if (string.IsNullOrEmpty(PackageSetting.ManifestClassName) &&
                _manifestServicesClassNames.Count > 0)
            {
                PackageSetting.ManifestClassName = _manifestServicesClassNames[0];
            }
            _popupFieldManifest.SetValueWithoutNotify(PackageSetting.ManifestClassName);

        }

        public void RefreshElement(bool IsForceRebuild)
        {
            var b = IsForceRebuild || PackageSetting.New;
            pipelineType.SetEnabled(b);
            nameStyleType.SetEnabled(b);
            compressionType.SetEnabled(b);
            _popupFieldEncryption.SetEnabled(b);
            _popupFieldManifest.SetEnabled(b);            
            inputBuiltinTags.SetEnabled(b);
            RefreshElement();
        }
        void RefreshElement()
        {
            compressionType.style.display =
                PackageSetting.PiplineOption == EBuildPipeline.RawFileBuildPipeline.ToString() ? DisplayStyle.None : DisplayStyle.Flex;
        }
        #region 辅助方法
        // 辅助方法

        private IEncryptionServices CreateEncryptionServicesInstance()
        {
            if (_popupFieldEncryption.index < 0)
                return null;
            var classType = _encryptionServicesClassTypes[_popupFieldEncryption.index];
            return (IEncryptionServices)Activator.CreateInstance(classType);
        }

        private static List<Type> GetEncryptionServicesClassTypes()
        {
            return EditorTools.GetAssignableTypes(typeof(IEncryptionServices));
        }
        private static List<Type> GetManifestServicesClassTypes()
        {
            return EditorTools.GetAssignableTypes(typeof(IManifestServices));
        }
        #endregion
    }

}
