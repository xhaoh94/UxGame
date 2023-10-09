using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using YooAsset;
using YooAsset.Editor;

internal class VersionPackageViewer
{
    private List<Type> _encryptionServicesClassTypes;
    private List<string> _encryptionServicesClassNames;
    //private List<Type> _sharedPackRuleClassTypes;
    //private List<string> _sharedPackRuleClassNames;

    protected TemplateContainer Root;
    BuildPackageSetting PackageSetting;
    EnumField _pipelineType;
    EnumField _nameStyleType;
    EnumField _compressionType;
    TextField _inputBuiltinTags;

    PopupField<string> _encryption;
    //PopupField<string> _sharedPackRule;
    public VersionPackageViewer(VisualElement parent)
    {
        CreateView(parent);
    }
    private void CreateView(VisualElement parent)
    {
        // 加载布局文件
        var visualAsset = UxmlLoader.LoadWindowUXML<VersionPackageViewer>();
        if (visualAsset == null)
            return;

        Root = visualAsset.CloneTree();
        Root.style.flexGrow = 1f;
        parent.Add(Root);

        _encryptionServicesClassTypes = GetEncryptionServicesClassTypes();
        _encryptionServicesClassNames = _encryptionServicesClassTypes.Select(t => t.FullName).ToList();
        //_sharedPackRuleClassTypes = GetSharedPackRuleClassTypes();
        //_sharedPackRuleClassNames = _sharedPackRuleClassTypes.Select(t => t.FullName).ToList();

        // 构建管线
        _pipelineType = Root.Q<EnumField>("pipelineType");
        _pipelineType.Init(EBuildPipeline.ScriptableBuildPipeline);
        _pipelineType.style.width = 500;
        _pipelineType.RegisterValueChangedCallback(evt =>
        {
            PackageSetting.PiplineOption = (EBuildPipeline)evt.newValue;
            RefreshElement();
        });

        // 资源命名格式
        _nameStyleType = Root.Q<EnumField>("nameStyleType");
        _nameStyleType.Init(EFileNameStyle.HashName);
        _nameStyleType.style.width = 500;
        _nameStyleType.RegisterValueChangedCallback(evt =>
        {
            PackageSetting.NameStyleOption = (EFileNameStyle)evt.newValue;
        });

        // 压缩方式
        _compressionType = Root.Q<EnumField>("compressionType");
        _compressionType.Init(ECompressOption.LZ4);
        _compressionType.style.width = 500;
        _compressionType.RegisterValueChangedCallback(evt =>
        {
            PackageSetting.CompressOption = (ECompressOption)evt.newValue;
        });

        // 首包资源标签
        _inputBuiltinTags = Root.Q<TextField>("inputBuiltinTags");
        _inputBuiltinTags.RegisterValueChangedCallback(evt =>
        {
            PackageSetting.BuildTags = evt.newValue;
        });


        // 加密方法
        var encryptionContainer = Root.Q("encryptionContainer");
        if (_encryptionServicesClassNames.Count > 0)
        {
            _encryption = new PopupField<string>(_encryptionServicesClassNames, 0);
            _encryption.label = "加密方法";
            _encryption.style.width = 500;
            _encryption.RegisterValueChangedCallback(evt =>
            {
                PackageSetting.EncyptionClassName = evt.newValue;
            });
            encryptionContainer.Add(_encryption);
        }
        else
        {
            _encryption = new PopupField<string>();
            _encryption.label = "加密方法";
            _encryption.style.width = 500;
            encryptionContainer.Add(_encryption);
        }
    }
    public void RefreshView(BuildPackageSetting packageSetting)
    {
        PackageSetting = packageSetting;
        _pipelineType.SetValueWithoutNotify(PackageSetting.PiplineOption);
        _nameStyleType.SetValueWithoutNotify(PackageSetting.NameStyleOption);
        _compressionType.SetValueWithoutNotify(PackageSetting.CompressOption);
        _inputBuiltinTags.SetValueWithoutNotify(PackageSetting.BuildTags);

        if (string.IsNullOrEmpty(PackageSetting.EncyptionClassName) &&
             _encryptionServicesClassNames.Count > 0)
        {
            PackageSetting.EncyptionClassName = _encryptionServicesClassNames[0];
        }
        _encryption.SetValueWithoutNotify(PackageSetting.EncyptionClassName);


        //if (string.IsNullOrEmpty(SelectItem.PlatformConfig.SharedPackRule) &&
        //    _sharedPackRuleClassNames.Count > 0)
        //{
        //    SelectItem.PlatformConfig.SharedPackRule = _sharedPackRuleClassNames[0];
        //}
        //_sharedPackRule.SetValueWithoutNotify(SelectItem.PlatformConfig.SharedPackRule);
    }

    public void RefreshElement(bool IsForceRebuild)
    {
        _pipelineType.SetEnabled(IsForceRebuild);
        _nameStyleType.SetEnabled(IsForceRebuild);
        _compressionType.SetEnabled(IsForceRebuild);
        _encryption.SetEnabled(IsForceRebuild);
        //_sharedPackRule.SetEnabled(IsForceRebuild);
        _inputBuiltinTags.SetEnabled(IsForceRebuild);
        RefreshElement();
    }
    void RefreshElement()
    {
        _compressionType.style.display =
            PackageSetting.PiplineOption == EBuildPipeline.RawFileBuildPipeline ? DisplayStyle.None : DisplayStyle.Flex;
    }
    #region 构建包裹相关
    // 构建包裹相关

    private IEncryptionServices CreateEncryptionServicesInstance()
    {
        if (_encryption.index < 0)
            return null;
        var classType = _encryptionServicesClassTypes[_encryption.index];
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
