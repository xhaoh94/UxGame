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

        private List<Type> _manifestProcessServicesClassTypes;
        private List<string> _manifestProcessServicesClassNames;

        private List<Type> _manifestRestoreServicesClassTypes;
        private List<string> _manifestRestoreServicesClassNames;
        BuildPackageSetting PackageSetting;


        PopupField<string> _popupFieldEncryption;
        PopupField<string> _popupFieldManifestProess;
        PopupField<string> _popupFieldManifestRestore;

        public VersionPackageViewer(VisualElement parent)
        {
            CreateChildren();
            root.style.flexGrow = 1f;
            parent.Add(root);
            pipelineType.Init(EBuildPipeline.ScriptableBuildPipeline);
            nameStyleType.Init(EFileNameStyle.HashName);
            compressionType.Init(ECompressOption.LZ4);

            _encryptionServicesClassTypes = GetServicesClassTypes<IEncryptionServices>();
            _encryptionServicesClassNames = _encryptionServicesClassTypes.Select(t => t.FullName).ToList();
            if (_encryptionServicesClassNames.Count > 0)
            {
                _popupFieldEncryption = new PopupField<string>(_encryptionServicesClassNames, 0)
                {
                    label = "资源加密"
                };
                _popupFieldEncryption.RegisterValueChangedCallback(evt =>
                {
                    PackageSetting.EncyptionClassName = evt.newValue;
                });
                encryptionContainer.Add(_popupFieldEncryption);
            }
            else
            {
                _popupFieldEncryption = new PopupField<string>
                {
                    label = "资源加密"
                };
                encryptionContainer.Add(_popupFieldEncryption);
            }

            _manifestProcessServicesClassTypes = GetServicesClassTypes<IManifestProcessServices>();
            _manifestProcessServicesClassNames = _manifestProcessServicesClassTypes.Select(t => t.FullName).ToList();
            if (_manifestProcessServicesClassNames.Count > 0)
            {
                _popupFieldManifestProess = new PopupField<string>(_manifestProcessServicesClassNames, 0)
                {
                    label = "清单进程加密"
                };
                _popupFieldManifestProess.RegisterValueChangedCallback(evt =>
                {
                    PackageSetting.ManifestProcessServices = evt.newValue;
                });
                manifestContainer.Add(_popupFieldManifestProess);
            }
            else
            {
                _popupFieldManifestProess = new PopupField<string>
                {
                    label = "清单进程加密"
                };
                manifestContainer.Add(_popupFieldManifestProess);
            }

            _manifestRestoreServicesClassTypes = GetServicesClassTypes<IManifestRestoreServices>();
            _manifestRestoreServicesClassNames = _manifestRestoreServicesClassTypes.Select(t => t.FullName).ToList();
            if (_manifestRestoreServicesClassNames.Count > 0)
            {
                _popupFieldManifestRestore = new PopupField<string>(_manifestRestoreServicesClassNames, 0)
                {
                    label = "清单恢复解密"
                };
                _popupFieldManifestRestore.RegisterValueChangedCallback(evt =>
                {
                    PackageSetting.ManifestRestoreServices = evt.newValue;
                });
                manifestContainer.Add(_popupFieldManifestRestore);
            }
            else
            {
                _popupFieldManifestRestore = new PopupField<string>
                {
                    label = "清单恢复解密"
                };
                manifestContainer.Add(_popupFieldManifestRestore);
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

            if (string.IsNullOrEmpty(PackageSetting.ManifestProcessServices) &&
                _manifestProcessServicesClassNames.Count > 0)
            {
                PackageSetting.ManifestProcessServices = _manifestProcessServicesClassNames[0];
            }
            _popupFieldManifestProess.SetValueWithoutNotify(PackageSetting.ManifestProcessServices);

            if (string.IsNullOrEmpty(PackageSetting.ManifestRestoreServices) &&
    _manifestRestoreServicesClassNames.Count > 0)
            {
                PackageSetting.ManifestRestoreServices = _manifestRestoreServicesClassNames[0];
            }
            _popupFieldManifestRestore.SetValueWithoutNotify(PackageSetting.ManifestRestoreServices);

        }

        public void RefreshElement(bool IsForceRebuild)
        {
            var b = IsForceRebuild || PackageSetting.New;
            pipelineType.SetEnabled(b);
            nameStyleType.SetEnabled(b);
            compressionType.SetEnabled(b);
            _popupFieldEncryption.SetEnabled(b);
            _popupFieldManifestProess.SetEnabled(b);
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

        private static List<Type> GetServicesClassTypes<T>()
        {
            return EditorTools.GetAssignableTypes(typeof(T));
        }
        #endregion
    }

}
