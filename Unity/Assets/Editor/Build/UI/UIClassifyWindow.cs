using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using YooAsset.Editor;
using static UI.Editor.UIClassifySettingData;

namespace UI.Editor
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
            var package = packages.Find(x => x.PackageName == "UIPackage");
            if (package == null)
            {
                package = new AssetBundleCollectorPackage();
                package.PackageDesc = "UI��Դ��";
                package.PackageName = "UIPackage";
                packages.Add(package);
            }
            #region Builtin
            var buildinGp = package.Groups.Find(x => x.GroupName == "Builtin");
            if (buildinGp == null)
            {
                buildinGp = new AssetBundleCollectorGroup();
                buildinGp.AssetTags = "builtin";
                buildinGp.GroupDesc = "������Դ";
                buildinGp.GroupName = "Builtin";
                package.Groups.Add(buildinGp);
            }
            buildinGp.Collectors.Clear();

            HashSet<string> excludes = new HashSet<string>();
            var buildins = ResClassifySettings.builtins;
            foreach (var buildin in buildins)
            {
                var dir = $"{ResClassifySettings.path}/{buildin}";
                if (!Directory.Exists(dir)) continue;
                var collector = new AssetBundleCollector();
                collector.CollectPath = dir;
                collector.AddressRuleName = nameof(AddressByFolderAndFileName);
                buildinGp.Collectors.Add(collector);
                excludes.Add(buildin);
            }
            #endregion

            #region Lazyload
            var lazyloadGp = package.Groups.Find(x => x.GroupName == "Lazyload");
            if (lazyloadGp == null)
            {
                lazyloadGp = new AssetBundleCollectorGroup();
                lazyloadGp.AssetTags = "lazyload";
                lazyloadGp.GroupDesc = "��������Դ";
                lazyloadGp.GroupName = "Lazyload";
                package.Groups.Add(lazyloadGp);
            }
            lazyloadGp.Collectors.Clear();

            var lazyloads = ResClassifySettings.lazyloads;
            foreach (var lazyload in lazyloads)
            {
                var dir = $"{ResClassifySettings.path}/{lazyload.key}";
                if (!Directory.Exists(dir)) continue;
                var collector = new AssetBundleCollector();
                collector.CollectPath = dir;
                collector.AssetTags = lazyload.value;
                collector.AddressRuleName = nameof(AddressByFolderAndFileName);
                lazyloadGp.Collectors.Add(collector);
                excludes.Add(lazyload.key);
            }
            #endregion

            #region Preload
            var preloadGp = package.Groups.Find(x => x.GroupName == "Preload");
            if (preloadGp == null)
            {
                preloadGp = new AssetBundleCollectorGroup();
                preloadGp.AssetTags = "preload";
                preloadGp.GroupDesc = "Ԥ������Դ";
                preloadGp.GroupName = "Preload";
                package.Groups.Add(preloadGp);
            }
            preloadGp.Collectors.Clear();

            var collectorPreload = new AssetBundleCollector();
            collectorPreload.CollectPath = ResClassifySettings.path;
            collectorPreload.PackRuleName = typeof(PackTopDirectory).Name;
            collectorPreload.FilterRuleName = typeof(CollectPreloadUI).Name;
            collectorPreload.AddressRuleName = nameof(AddressByFolderAndFileName);
            preloadGp.Collectors.Add(collectorPreload);
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

                var helpBox = new HelpBox("��ȥ���ú������أ���������Դ��ΪԤ����", HelpBoxMessageType.Info);
                helpBox.style.fontSize = 26;

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
            ResClassifySettings.builtins = builtins.ToArray();
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
            for (int i = 0; i < ResClassifySettings.builtins.Length; i++)
            {
                var element = MakeBuildinItem();
                BindBuildinItem(element, i, ResClassifySettings.builtins[i]);
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