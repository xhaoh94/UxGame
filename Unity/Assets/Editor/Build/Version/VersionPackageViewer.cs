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

        protected VisualElement root;
        public VisualElement exportElement;
        public Toggle tgCollectSV;
        public EnumField pipelineType;
        public EnumField nameStyleType;
        public EnumField compressionType;
        public TextField inputBuiltinTags;
        public VisualElement encryptionContainer;
        public VisualElement manifestContainer;

        /// <summary>原 VersionPackageViewer.uxml 的手搭等价版本；uxml 根元素本身就是 exportElement。</summary>
        private void BuildUI()
        {
            exportElement = new VisualElement { name = "exportElement" };
            exportElement.style.flexGrow = 1f;
            root = exportElement;

            tgCollectSV = new Toggle("收集着色体变体") { name = "tgCollectSV" };
            tgCollectSV.RegisterValueChangedCallback(_OnTgCollectSVChanged);
            root.Add(tgCollectSV);

            pipelineType = new EnumField { name = "pipelineType", label = "构建管线" };
            pipelineType.style.flexShrink = 1f;
            pipelineType.RegisterValueChangedCallback(_OnPipelineTypeChanged);
            root.Add(pipelineType);

            nameStyleType = new EnumField { name = "nameStyleType", label = "资源命名格式" };
            nameStyleType.style.flexShrink = 1f;
            nameStyleType.RegisterValueChangedCallback(_OnNameStyleTypeChanged);
            root.Add(nameStyleType);

            compressionType = new EnumField { name = "compressionType", label = "压缩方式" };
            compressionType.style.flexShrink = 1f;
            compressionType.RegisterValueChangedCallback(_OnCompressionTypeChanged);
            root.Add(compressionType);

            inputBuiltinTags = new TextField("首包资源标签") { name = "inputBuiltinTags", pickingMode = PickingMode.Ignore };
            inputBuiltinTags.RegisterValueChangedCallback(_OnInputBuiltinTagsChanged);
            root.Add(inputBuiltinTags);

            encryptionContainer = new VisualElement { name = "encryptionContainer" };
            encryptionContainer.style.height = StyleKeyword.Auto;
            root.Add(encryptionContainer);

            manifestContainer = new VisualElement { name = "manifestContainer" };
            manifestContainer.style.height = StyleKeyword.Auto;
            root.Add(manifestContainer);
        }

        public VersionPackageViewer(VisualElement parent)
        {
            BuildUI();
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
                    label = "资源清单加密"
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
                    label = "资源清单加密"
                };
                manifestContainer.Add(_popupFieldManifestProess);
            }

            _manifestRestoreServicesClassTypes = GetServicesClassTypes<IManifestRestoreServices>();
            _manifestRestoreServicesClassNames = _manifestRestoreServicesClassTypes.Select(t => t.FullName).ToList();
            if (_manifestRestoreServicesClassNames.Count > 0)
            {
                _popupFieldManifestRestore = new PopupField<string>(_manifestRestoreServicesClassNames, 0)
                {
                    label = "资源清单解密"
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
                    label = "资源清单解密"
                };
                manifestContainer.Add(_popupFieldManifestRestore);
            }
        }
        private void _OnTgCollectSVChanged(ChangeEvent<bool> e)
        {
            PackageSetting.IsCollectShaderVariant = e.newValue;
        }
        private void _OnPipelineTypeChanged(ChangeEvent<Enum> e)
        {
            PackageSetting.PiplineOption = e.newValue.ToString();
            RefreshElement();
        }
        private void _OnCompressionTypeChanged(ChangeEvent<Enum> e)
        {
            PackageSetting.CompressOption = (ECompressOption)e.newValue;
        }
        private void _OnNameStyleTypeChanged(ChangeEvent<Enum> e)
        {
            PackageSetting.NameStyleOption = (EFileNameStyle)e.newValue;
        }
        private void _OnInputBuiltinTagsChanged(ChangeEvent<string> e)
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

        private static List<Type> GetServicesClassTypes<T>()
        {
            return EditorTools.GetAssignableTypes(typeof(T));
        }
        #endregion
    }

}
