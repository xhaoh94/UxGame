using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using YooAsset.Editor;
using static Ux.Editor.Build.UI.UIClassifySettingData;

namespace Ux.Editor.Build.UI
{
    public partial class UIClassifyWindow : EditorWindow
    {
        [MenuItem("UxGame/工具/UI/资源分类", false, 511)]
        public static void ShowExample()
        {
            var window = GetWindow<UIClassifyWindow>("UIClassifyWindow", true);
            window.minSize = new Vector2(800, 600);
        }
        public static void CreateYooAssetUIGroup()
        {
            Log.Debug("------------------------------------>生成YooAsset UI收集器配置<------------------------------");
            var packages = AssetBundleCollectorSettingData.Setting.Packages;
            var package = packages.Find(x => x.PackageName == "MainPackage");
            if (package == null)
            {
                package = new AssetBundleCollectorPackage();
                package.PackageDesc = "主包";
                package.PackageName = "MainPackage";
                packages.Add(package);
            }
            var group = package.Groups.Find(x => x.GroupName == "UI");
            if (group == null)
            {
                group = new AssetBundleCollectorGroup();
                group.AssetTags = string.Empty;
                group.GroupDesc = "UI界面";
                group.GroupName = "UI";
                package.Groups.Add(group);
            }
            group.Collectors.Clear();

            #region Builtin                       
            var collectorBuiltin = new AssetBundleCollector();
            collectorBuiltin.AssetTags = "builtin";
            collectorBuiltin.CollectPath = ResClassifySettings.path;
            collectorBuiltin.PackRuleName = typeof(PackTopDirectory).Name;
            collectorBuiltin.FilterRuleName = typeof(CollectBuiltinUI).Name;
            collectorBuiltin.AddressRuleName = nameof(AddressByFolderAndFileName);
            group.Collectors.Add(collectorBuiltin);
            #endregion

            #region Preload
            var collectorPreload = new AssetBundleCollector();
            collectorPreload.AssetTags = "preload";
            collectorPreload.CollectPath = ResClassifySettings.path;
            collectorPreload.PackRuleName = typeof(PackTopDirectory).Name;
            collectorPreload.FilterRuleName = typeof(CollectPreloadUI).Name;
            collectorPreload.AddressRuleName = nameof(AddressByFolderAndFileName);
            group.Collectors.Add(collectorPreload);
            #endregion

            #region Lazyload
            var lazyloads = ResClassifySettings.lazyloads;
            foreach (var lazyload in lazyloads)
            {
                var dir = $"{ResClassifySettings.path}/{lazyload.key}";
                if (!Directory.Exists(dir)) continue;
                var collector = new AssetBundleCollector();
                collector.CollectPath = dir;
                collector.AssetTags = lazyload.value;
                collector.AddressRuleName = nameof(AddressByFolderAndFileName);
                group.Collectors.Add(collector);
            }
            #endregion

            EditorUtility.SetDirty(ResClassifySettings);
            AssetBundleCollectorSettingData.SaveFile();
        }
        static UIClassifySettingData _ResClassifySettings;
        public static UIClassifySettingData ResClassifySettings
        {
            get
            {
                if (_ResClassifySettings == null)
                {
                    _ResClassifySettings = SettingTools.GetSingletonAssets<UIClassifySettingData>("Assets/Setting/Build/UI");
                }
                return _ResClassifySettings;
            }
        }


        public void CreateGUI()
        {
            _ResClassifySettings = null;
            CreateChildren();
            rootVisualElement.Add(root);
            var pathObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ResClassifySettings.path);
            if (pathObject != null)
                pathObject.name = ResClassifySettings.path;
            pathField.SetValueWithoutNotify(pathObject);

            var helpBox = new HelpBox("不在预加载资源或懒加载资源的，都是内置资源，出包的时候会打进包体里的", HelpBoxMessageType.Info);
            helpBox.style.fontSize = 30;
            root.Insert(0, helpBox);
            buildin.style.unityParagraphSpacing = 10;
            lazyload.style.unityParagraphSpacing = 10;
            UpdateBuildIn();
            UpdateLazyload();
        }

        partial void _OnPathFieldChanged(ChangeEvent<Object> e)
        {
            ResClassifySettings.path = AssetDatabase.GetAssetPath(e.newValue);
            pathField.value.name = ResClassifySettings.path;
        }
        partial void _OnBtnBuildinAddClick()
        {
            var element = MakeBuildinItem();
            BindBuildinItem(element, buildin.childCount);
            buildin.Add(element);
        }
        partial void _OnBtnLazyloadAddClick()
        {
            var element = MakeLazyloadItem();
            BindLazyloadItem(element, lazyload.childCount);
            lazyload.Add(element);
        }
        partial void _OnBtnApplyClick()
        {
            List<string> builtins = new List<string>();
            for (int i = 0; i < buildin.childCount; i++)
            {
                var ele = buildin.ElementAt(i);
                var tf = ele.Q<TextField>("Label");
                if (string.IsNullOrEmpty(tf.value)) continue;
                builtins.Add(tf.value);
            }
            List<ResClassify> lazyloads = new List<ResClassify>();
            for (int i = 0; i < lazyload.childCount; i++)
            {
                var ele = lazyload.ElementAt(i);
                var tf1 = ele.Q<TextField>("Label1");
                var tf2 = ele.Q<TextField>("Label2");
                if (string.IsNullOrEmpty(tf1.value)) continue;
                if (string.IsNullOrEmpty(tf2.value)) continue;
                lazyloads.Add(new ResClassify() { key = tf1.value, value = tf2.value });
            }
            ResClassifySettings.proloads = builtins.ToArray();
            ResClassifySettings.lazyloads = lazyloads.ToArray();
            CreateYooAssetUIGroup();
            if (UICodeGenWindow.Export() && EditorUtility.DisplayDialog("提示", "创建成功!", "ok"))
            {
                _OnBtnLoadClick();
            }
        }
        partial void _OnBtnLoadClick()
        {
            _ResClassifySettings = null;
            UpdateBuildIn();
            UpdateLazyload();
        }

        private void UpdateBuildIn()
        {
            buildin.Clear();
            for (int i = 0; i < ResClassifySettings.proloads.Length; i++)
            {
                var element = MakeBuildinItem();
                BindBuildinItem(element, i, ResClassifySettings.proloads[i]);
                buildin.Add(element);
            }
        }
        private void UpdateLazyload()
        {
            lazyload.Clear();
            for (int i = 0; i < ResClassifySettings.lazyloads.Length; i++)
            {
                var element = MakeLazyloadItem();
                BindLazyloadItem(element, i, ResClassifySettings.lazyloads[i]);
                lazyload.Add(element);
            }
        }
        private VisualElement MakeBuildinItem()
        {
            VisualElement element = new VisualElement();
            {
                element.style.alignItems = Align.Center;
                element.style.flexDirection = FlexDirection.Row;
                var btn = new Button();
                btn.name = "Sub";
                btn.text = "[-]";
                btn.style.width = 30f;
                btn.style.height = 20;
                element.Add(btn);
                var label = new TextField();
                label.name = "Label";
                label.style.unityTextAlign = TextAnchor.MiddleLeft;
                label.style.flexGrow = 1f;
                label.style.height = 20f;
                element.Add(label);
            }

            return element;
        }
        private void BindBuildinItem(VisualElement element, int index, string pkg = "")
        {

            var btn = element.Q<Button>("Sub");
            btn.clicked += () =>
            {
                buildin.RemoveAt(index);
            };
            if (!string.IsNullOrEmpty(pkg))
            {
                var textField = element.Q<TextField>("Label");
                textField.value = pkg;
            }
        }

        private VisualElement MakeLazyloadItem()
        {
            VisualElement element = new VisualElement();
            {
                element.style.alignItems = Align.Center;
                element.style.flexDirection = FlexDirection.Row;
                var btn = new Button();
                btn.name = "Sub";
                btn.text = "[-]";
                btn.style.width = 30f;
                btn.style.height = 20;
                element.Add(btn);
                VisualElement element2 = new VisualElement();
                {
                    element2.style.flexGrow = 1f;
                    var label = new TextField("资源包");
                    label.name = "Label1";
                    label.style.unityTextAlign = TextAnchor.MiddleLeft;
                    label.style.flexGrow = 1f;
                    label.style.height = 20f;
                    element2.Add(label);
                    var label2 = new TextField("资源标签");
                    label2.name = "Label2";
                    label2.style.unityTextAlign = TextAnchor.MiddleLeft;
                    label2.style.flexGrow = 1f;
                    label2.style.height = 20f;
                    element2.Add(label2);
                }
                element.Add(element2);
            }

            return element;
        }
        private void BindLazyloadItem(VisualElement element, int index, ResClassify resClassify = null)
        {

            var btn = element.Q<Button>("Sub");
            btn.clicked += () =>
            {
                lazyload.RemoveAt(index);
            };
            if (resClassify != null)
            {
                var textField = element.Q<TextField>("Label1");
                textField.value = resClassify.key;
                var textField2 = element.Q<TextField>("Label2");
                textField2.value = resClassify.value;
            }
        }
    }
}