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
        BuildPackageSetting PackageSetting;


        PopupField<string> encryption;        
        public VersionPackageViewer(VisualElement parent)
        {
            CreateChildren();
            root.style.flexGrow = 1f;
            parent.Add(root);
            _encryptionServicesClassTypes = GetEncryptionServicesClassTypes();
            _encryptionServicesClassNames = _encryptionServicesClassTypes.Select(t => t.FullName).ToList();
            if (_encryptionServicesClassNames.Count > 0)
            {
                encryption = new PopupField<string>(_encryptionServicesClassNames, 0);
                encryption.label = "加密方法";
                encryption.RegisterValueChangedCallback(evt =>
                {
                    PackageSetting.EncyptionClassName = evt.newValue;
                });
                encryptionContainer.Add(encryption);
            }
            else
            {
                encryption = new PopupField<string>();
                encryption.label = "加密方法";
                encryptionContainer.Add(encryption);
            }
        }
        partial void _OnTgCollectSVChanged(ChangeEvent<bool> e)
        {
            PackageSetting.IsCollectShaderVariant = e.newValue;
        }
        partial void _OnPipelineTypeChanged(ChangeEvent<Enum> e)
        {
            PackageSetting.PiplineOption = (EBuildPipeline)e.newValue;
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
            pipelineType.SetValueWithoutNotify(PackageSetting.PiplineOption);
            nameStyleType.SetValueWithoutNotify(PackageSetting.NameStyleOption);
            compressionType.SetValueWithoutNotify(PackageSetting.CompressOption);
            inputBuiltinTags.SetValueWithoutNotify(PackageSetting.BuildTags);

            if (string.IsNullOrEmpty(PackageSetting.EncyptionClassName) &&
                 _encryptionServicesClassNames.Count > 0)
            {
                PackageSetting.EncyptionClassName = _encryptionServicesClassNames[0];
            }
            encryption.SetValueWithoutNotify(PackageSetting.EncyptionClassName);


            //if (string.IsNullOrEmpty(SelectItem.PlatformConfig.SharedPackRule) &&
            //    _sharedPackRuleClassNames.Count > 0)
            //{
            //    SelectItem.PlatformConfig.SharedPackRule = _sharedPackRuleClassNames[0];
            //}
            //_sharedPackRule.SetValueWithoutNotify(SelectItem.PlatformConfig.SharedPackRule);        
        }

        public void RefreshElement(bool IsForceRebuild)
        {
            var b = IsForceRebuild || PackageSetting.New;
            pipelineType.SetEnabled(b);
            nameStyleType.SetEnabled(b);
            compressionType.SetEnabled(b);
            encryption.SetEnabled(b);
            //_sharedPackRule.SetEnabled(IsForceRebuild);
            inputBuiltinTags.SetEnabled(b);
            RefreshElement();
        }
        void RefreshElement()
        {
            compressionType.style.display =
                PackageSetting.PiplineOption == EBuildPipeline.RawFileBuildPipeline ? DisplayStyle.None : DisplayStyle.Flex;
        }
        #region 构建包裹相关
        // 构建包裹相关

        private IEncryptionServices CreateEncryptionServicesInstance()
        {
            if (encryption.index < 0)
                return null;
            var classType = _encryptionServicesClassTypes[encryption.index];
            return (IEncryptionServices)Activator.CreateInstance(classType);
        }
        //private ISharedPackRule CreateSharedPackRuleInstance()
        //{
        //    if (_sharedPackRule.index < 0)
        //        return null;
        //    var classType = _sharedPackRuleClassTypes[_sharedPackRule.index];
        //    return (ISharedPackRule)Activator.CreateInstance(classType);
        //}

        private static List<Type> GetEncryptionServicesClassTypes()
        {
            return EditorTools.GetAssignableTypes(typeof(IEncryptionServices));
        }
        //private static List<Type> GetSharedPackRuleClassTypes()
        //{
        //    return EditorTools.GetAssignableTypes(typeof(ISharedPackRule));
        //}
        #endregion
    }

}
