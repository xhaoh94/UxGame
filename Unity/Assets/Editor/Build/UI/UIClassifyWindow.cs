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
    public class UIClassifyWindow : EditorWindow
    {
        [MenuItem("UxGame/����/UI/��Դ����", false, 511)]
        public static void ShowExample()
        {
            var window = GetWindow<UIClassifyWindow>("UIClassifyWindow", true);
            window.minSize = new Vector2(800, 600);
        }
        public static void CreateYooAssetUIGroup()
        {
            Log.Debug("------------------------------------>����YooAsset UI�ռ�������<------------------------------");
            var packages = AssetBundleCollectorSettingData.Setting.Packages;
            var package = packages.Find(x => x.PackageName == "MainPackage");
            if (package == null)
            {
                package = new AssetBundleCollectorPackage();
                package.PackageDesc = "����";
                package.PackageName = "MainPackage";
                packages.Add(package);
            }
            var group= package.Groups.Find(x => x.GroupName == "UI");
            if (group == null)
            {
                group = new AssetBundleCollectorGroup();
                group.AssetTags = string.Empty;
                group.GroupDesc = "UI����";
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

        [SerializeField]
        private VisualTreeAsset m_VisualTreeAsset = default;

        VisualElement builtin;
        VisualElement lazyload;
        public void CreateGUI()
        {
            _ResClassifySettings = null;
            VisualElement root = rootVisualElement;
            m_VisualTreeAsset.CloneTree(root);
            try
            {
                var pathField = root.Q<ObjectField>("pathField");
                var pathObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ResClassifySettings.path);
                if (pathObject != null)
                    pathObject.name = ResClassifySettings.path;
                pathField.SetValueWithoutNotify(pathObject);
                pathField.RegisterValueChangedCallback(evt =>
                {
                    ResClassifySettings.path = AssetDatabase.GetAssetPath(evt.newValue);
                    pathField.value.name = ResClassifySettings.path;
                });

                var helpBox = new HelpBox("����Ԥ������Դ����������Դ�ģ�����������Դ��������ʱ������������", HelpBoxMessageType.Info);
                helpBox.style.fontSize = 30;

                root.Insert(0, helpBox);
                builtin = root.Q<VisualElement>("buildin");
                builtin.style.unityParagraphSpacing = 10;

                lazyload = root.Q<VisualElement>("lazyload");
                lazyload.style.unityParagraphSpacing = 10;

                var btnBuildinAdd = root.Q<Button>("btnBuildinAdd");
                btnBuildinAdd.clicked += OnBtnBuildinAdd;
                var btnLazyloadAdd = root.Q<Button>("btnLazyloadAdd");
                btnLazyloadAdd.clicked += OnBtnLazyloadAdd;

                var btnApply = root.Q<Button>("btnApply");
                btnApply.clicked += OnBtnCreate;
                var btnLoad = root.Q<Button>("btnLoad");
                btnLoad.clicked += ResetRc;
                UpdateBuildIn();
                UpdateLazyload();
            }
            catch
            {

            }
        }
        void OnBtnBuildinAdd()
        {
            var element = MakeBuildinItem();
            BindBuildinItem(element, builtin.childCount);
            builtin.Add(element);
        }
        void OnBtnLazyloadAdd()
        {
            var element = MakeLazyloadItem();
            BindLazyloadItem(element, lazyload.childCount);
            lazyload.Add(element);
        }
        void OnBtnCreate()
        {
            List<string> builtins = new List<string>();
            for (int i = 0; i < builtin.childCount; i++)
            {
                var ele = builtin.ElementAt(i);
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
            if (UICodeGenWindow.Export() && EditorUtility.DisplayDialog("��ʾ", "�����ɹ�!", "ok"))
            {
                ResetRc();
            }
        }
        void ResetRc()
        {
            _ResClassifySettings = null;
            UpdateBuildIn();
            UpdateLazyload();
        }
        private void UpdateBuildIn()
        {
            builtin.Clear();
            for (int i = 0; i < ResClassifySettings.proloads.Length; i++)
            {
                var element = MakeBuildinItem();
                BindBuildinItem(element, i, ResClassifySettings.proloads[i]);
                builtin.Add(element);
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
                builtin.RemoveAt(index);
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
                    var label = new TextField("��Դ��");
                    label.name = "Label1";
                    label.style.unityTextAlign = TextAnchor.MiddleLeft;
                    label.style.flexGrow = 1f;
                    label.style.height = 20f;
                    element2.Add(label);
                    var label2 = new TextField("��Դ��ǩ");
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