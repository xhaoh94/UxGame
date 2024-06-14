using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static Ux.Editor.Build.UI.ComponentData;

namespace Ux.Editor.Build.UI
{
    public enum UIExtends
    {
        None,
        Panel,
        Component,
        FGUI,
    }
    public enum UIExtendPanel
    {
        View,
        Window,
        TabView,
        MessageBox,
        Tip,
    }
    public enum UIExtendComponent
    {
        Object,
        TabFrame,        
        ItemRenderer,
        UIModel,
        RTModel,
    }
    public enum UIExttendFGUI
    {
        Button,
        ProgressBar,
        List
    }
    public class UICodeFoldout : Foldout
    {
        public FairyGUI.UIPackage pkg;
    }
    public class UICodeButton : Button
    {
        public FairyGUI.PackageItem pi;
        public ComponentData comData;
        public UICodeButton(Action<UICodeButton> cb)
        {
            clicked += () => { cb(this); };
        }
    }
    public partial class UICodeGenWindow : EditorWindow
    {

        [MenuItem("UxGame/构建/UI/代码生成", false, 510)]
        public static void ShowExample()
        {
            var window = GetWindow<UICodeGenWindow>("UICodeGenWindow", true);
            window.minSize = new Vector2(800, 600);
        }

        [SerializeField]
        private VisualTreeAsset m_VisualTreeAsset = default;

        private string lastPkg;
        private string lastRes;
        private UICodeButton selectItem;


        TextField inputCodePath;
        TextField inputNS;
        Toggle tgIgnore;

        TextField inputSearch;
        ScrollView svPkg;
        ScrollView svMember;

        IMGUIContainer comContaine;
        Toggle tgExport;
        VisualElement elementExport;
        Toggle tgUseGlobal;
        IMGUIContainer settContainer;
        TextField inputCodePath_select;
        Toggle tgIgnore_select;
        TextField inputNS_select;
        TextField inputClsName;
        DropdownField ddExt;

        Dictionary<string, TextField> nameTextField = new Dictionary<string, TextField>();

        VisualElement elementContent;
        VisualElement tabViewElement;
        VisualElement messageBoxElement;
        VisualElement tipElement;
        VisualElement modelElement;


        public void CreateGUI()
        {
            UICodeGenSettingData.Load();
            VisualElement root = rootVisualElement;            
            m_VisualTreeAsset.CloneTree(root);
            try
            {
                inputCodePath = root.Q<TextField>("inputCodePath");
                inputCodePath.SetValueWithoutNotify(UICodeGenSettingData.CodeGenPath);
                inputCodePath.RegisterValueChangedCallback(e => { FreshComponentData(); });

                var btnCodePath = root.Q<Button>("btnCodePath");
                btnCodePath.clicked += OnBtnCodeGenPathClick;

                inputNS = root.Q<TextField>("inputNS");
                inputNS.SetValueWithoutNotify(UICodeGenSettingData.DefaultNs);
                inputCodePath.RegisterValueChangedCallback(e =>
                {
                    UICodeGenSettingData.DefaultNs = e.newValue;
                });

                tgIgnore = root.Q<Toggle>("tgIgnore");
                tgIgnore.SetValueWithoutNotify(UICodeGenSettingData.IngoreDefault);
                tgIgnore.RegisterValueChangedCallback(e =>
                {
                    UICodeGenSettingData.IngoreDefault = e.newValue;
                    FreshComponentData();
                });

                var btnShow = root.Q<Button>("btnShow");
                btnShow.clicked += OnAllShow;
                var btnHide = root.Q<Button>("btnHide");
                btnHide.clicked += OnAllHide;

                var btnFresh = root.Q<Button>("btnFresh");
                btnFresh.clicked += () =>
                {
                    lastPkg = lastRes = string.Empty;
                    if (selectItem != null)
                    {
                        lastPkg = selectItem.comData.pkgName;
                        lastRes = selectItem.comData.resName;
                    }
                    UICodeGenSettingData.Save();
                    UICodeGenSettingData.Load();
                    FreshPkgData();
                    if (!string.IsNullOrEmpty(lastPkg) && !string.IsNullOrEmpty(lastRes))
                    {
                        for (int i = 0; i < svPkg.childCount; i++)
                        {
                            var foldout = svPkg.ElementAt(i) as UICodeFoldout;
                            for (var k = 0; k < foldout.childCount; k++)
                            {
                                var element = foldout.ElementAt(k) as UICodeButton;
                                if (element.pi == null) continue;
                                if (element.pi.owner.name == lastPkg &&
                                element.pi.name == lastRes)
                                {
                                    foldout.value = true;
                                    OnCodeButtonClick(element);
                                    goto b;
                                }
                            }
                        }
                    b:;
                        lastPkg = lastRes = string.Empty;
                    }

                };
                var btnSearch = root.Q<Button>("btnSearch");
                btnSearch.clicked += OnSearch;
                inputSearch = root.Q<TextField>("inputSearch");

                svPkg = root.Q<ScrollView>("svPkg");
                svMember = root.Q<ScrollView>("svMember");

                comContaine = root.Q<IMGUIContainer>("comContaine");
                comContaine.style.display = DisplayStyle.None;

                tgExport = root.Q<Toggle>("tgExport");
                tgExport.RegisterValueChangedCallback(e => { SaveSelectItemData(); });

                elementExport = root.Q<VisualElement>("elementExport");

                tgUseGlobal = root.Q<Toggle>("tgUseGlobal");
                tgUseGlobal.RegisterValueChangedCallback(e => { SaveSelectItemData(); });

                settContainer = root.Q<IMGUIContainer>("settContainer");
                settContainer.style.display = DisplayStyle.None;

                inputCodePath_select = root.Q<TextField>("inputCodePath_select");
                inputCodePath.RegisterValueChangedCallback(e => { SaveSelectItemData(); });
                var btnCodePath_select = root.Q<Button>("btnCodePath_select");
                btnCodePath_select.clicked += OnBtnCodeGenPathSelectClick;

                tgIgnore_select = root.Q<Toggle>("tgIgnore_select");
                tgIgnore_select.RegisterValueChangedCallback(e => { SaveSelectItemData(); });

                inputNS_select = root.Q<TextField>("inputNS_select");
                inputNS_select.RegisterValueChangedCallback(e => { SaveSelectItemData(); });
                inputClsName = root.Q<TextField>("inputClsName");
                inputClsName.RegisterValueChangedCallback(e => { SaveSelectItemData(); });

                ddExt = root.Q<DropdownField>("ddExt");
                var keys = UIEditorTools.CheckExt();
                ddExt.choices = keys;
                ddExt.index = 0;
                ddExt.RegisterValueChangedCallback(e => { SaveSelectItemData(); });

                elementContent = root.Q<VisualElement>("elementContent");

                tabViewElement = new VisualElement();
                elementContent.Add(tabViewElement);

                messageBoxElement = new VisualElement();
                elementContent.Add(messageBoxElement);

                tipElement = new VisualElement();
                elementContent.Add(tipElement);

                modelElement = new VisualElement();
                elementContent.Add(modelElement);


                var btnGenSelectItem = root.Q<Button>("btnGenSelectItem");
                btnGenSelectItem.clicked += OnBtnGenClick;
                var btnGenAll = root.Q<Button>("btnGenAll");
                btnGenAll.clicked += OnBtnGenAllClick;

                FreshPkgData();
            }
            catch
            {

            }
        }

        void OnSearch()
        {
            var str = inputSearch.text.ToLower();
            if (string.IsNullOrEmpty(str))
            {
                for (int i = 0; i < svPkg.childCount; i++)
                {
                    var foldout = svPkg.ElementAt(i) as UICodeFoldout;
                    bool hit = false;
                    var itemDatas = UIEditorTools.GetPackageItems(foldout.pkg);
                    List<string> pid = new List<string>();
                    foreach (var pi in itemDatas)
                    {
                        var pName = pi.name.ToLower();
                        if (pName.IndexOf(str) >= 0)
                        {
                            hit = true;
                            pid.Add(pi.id);
                        }
                    }
                    foldout.value = hit;
                    foldout.style.display = hit ? DisplayStyle.Flex : DisplayStyle.None;
                    if (pid.Count > 0)
                    {
                        for (var k = 0; k < foldout.childCount; k++)
                        {
                            var element = foldout.ElementAt(k) as UICodeButton;
                            if (pid.Contains(element.pi.id))
                            {
                                element.style.display = DisplayStyle.Flex;
                            }
                            else
                            {
                                element.style.display = DisplayStyle.None;
                            }
                        }
                    }
                }
            }
            selectItem = null;
            FreshComponentData();
        }
        void OnAllShow()
        {
            for (int i = 0; i < svPkg.childCount; i++)
            {
                var foldout = svPkg.ElementAt(i) as UICodeFoldout;
                foldout.value = true;
            }
        }
        void OnAllHide()
        {
            for (int i = 0; i < svPkg.childCount; i++)
            {
                var foldout = svPkg.ElementAt(i) as UICodeFoldout;
                foldout.value = false;
            }
        }
        void FreshComContaine()
        {
            if (selectItem == null)
            {
                comContaine.style.display = DisplayStyle.None;
                return;
            }
            if (selectItem.comData == null)
            {
                comContaine.style.display = DisplayStyle.None;
                return;
            }
            var data = selectItem.comData;

            comContaine.style.display = DisplayStyle.Flex;
            tgExport.SetValueWithoutNotify(data.isExport);
            elementExport.style.display = data.isExport ? DisplayStyle.Flex : DisplayStyle.None;
            tgUseGlobal.SetValueWithoutNotify(data.useGlobal);
            settContainer.style.display = data.useGlobal ? DisplayStyle.None : DisplayStyle.Flex;
            inputCodePath_select.SetValueWithoutNotify(data.path);
            tgIgnore_select.SetValueWithoutNotify(data.ingoreDefault);
            inputNS_select.SetValueWithoutNotify(data.ns);
            inputClsName.SetValueWithoutNotify(data.cls);
            ddExt.SetValueWithoutNotify(data.ext);

            if (data.IsTabFrame)
            {
                CreateText(data.TabViewData, tabViewElement);
            }
            else
            {
                tabViewElement.style.display = DisplayStyle.None;
            }
            if (data.IsMessageBox)
            {
                CreateText(data.MessageBoxData, messageBoxElement);
            }
            else
            {
                messageBoxElement.style.display = DisplayStyle.None;
            }

            if (data.IsTip)
            {
                CreateText(data.TipData, tipElement);
            }
            else
            {
                tipElement.style.display = DisplayStyle.None;
            }

            if (data.IsModel)
            {
                CreateText(data.ModelData, modelElement);
            }
            else
            {
                modelElement.style.display = DisplayStyle.None;
            }
        }
        void CreateText(List<CustomData> listData, VisualElement parent)
        {
            parent.style.display = DisplayStyle.Flex;
            var code = parent.GetHashCode();

            foreach (var temData in listData)
            {
                var key = temData.Key + "_" + code;
                if (!nameTextField.TryGetValue(key, out var textField))
                {
                    textField = new TextField(temData.Key);
                    textField.RegisterValueChangedCallback(e => { SaveSelectItemData(); });
                    parent.Add(textField);
                    nameTextField.Add(key, textField);
                }
                textField.value = temData.Name;
            }
        }
        void SaveSelectItemData()
        {
            if (selectItem == null)
            {
                return;
            }
            var data = selectItem.comData;
            if (data == null)
            {
                return;
            }
            var pi = selectItem.pi;

            data.useGlobal = tgUseGlobal.value;
            data.path = inputCodePath_select.text;
            data.ingoreDefault = tgIgnore_select.value;
            data.pkgName = pi.owner.name;
            data.resName = pi.name;
            data.ns = inputNS_select.text;
            data.cls = inputClsName.text;
            data.ext = ddExt.value;
            data.isExport = tgExport.value;

            if (data.IsTabFrame)
            {
                FreshDict(tabViewElement, data.TabViewData);
            }

            if (data.IsMessageBox)
            {
                FreshDict(messageBoxElement, data.MessageBoxData);
            }

            if (data.IsTip)
            {
                FreshDict(tipElement, data.TipData);
            }

            if (data.IsModel)
            {
                FreshDict(modelElement, data.ModelData);
            }
            UICodeGenSettingData.SetComponentData(data);
            FreshComponentData();
        }

        void FreshDict(VisualElement parent, List<CustomData> listData)
        {
            var code = parent.GetHashCode();
            for (int i = 0; i < listData.Count; i++)
            {
                var temData = listData[i];
                var key = temData.Key + "_" + code;
                if (nameTextField.TryGetValue(key, out var textField))
                {
                    temData.Name = textField.value;
                    listData[i] = temData;
                }
            }
        }
        private void OnDestroy()
        {
            UICodeGenSettingData.Save();
            UIEditorTools.Clear();
            UIEditorTools.ClearFairyGUI();
            AssetDatabase.Refresh();
        }

        private void Update()
        {
            if (EditorApplication.isCompiling)
            {
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.isPlaying = false;
                }
                Close();
            }
        }

        void OnBtnCodeGenPathSelectClick()
        {
            var temPath = EditorUtility.OpenFolderPanel("请选择生成路径", UICodeGenSettingData.CodeGenPath, "");
            if (temPath.Length == 0)
            {
                return;
            }

            if (!Directory.Exists(temPath))
            {
                EditorUtility.DisplayDialog("错误", "路径不存在!", "ok");
                return;
            }
            inputCodePath_select.SetValueWithoutNotify(temPath);
        }
        void OnBtnCodeGenPathClick()
        {
            var temPath = EditorUtility.OpenFolderPanel("请选择生成路径", UICodeGenSettingData.CodeGenPath, "");
            if (temPath.Length == 0)
            {
                return;
            }

            if (!Directory.Exists(temPath))
            {
                EditorUtility.DisplayDialog("错误", "路径不存在!", "ok");
                return;
            }
            UICodeGenSettingData.CodeGenPath = temPath;
            inputCodePath.SetValueWithoutNotify(temPath);
        }
        private void FreshPkgData()
        {
            AssetDatabase.Refresh();
            svPkg.Clear();
            svMember.Clear();
            comContaine.style.display = DisplayStyle.None;
            UIEditorTools.ClearFairyGUI();
            FairyGUIEditor.EditorToolSet.ReloadPackages();
            List<FairyGUI.UIPackage> pkgs = FairyGUI.UIPackage.GetPackages();
            HashSet<string> pkgNames = new HashSet<string>();
            foreach (var pkg in pkgs)
            {
                var element = MakePkgItem();
                BindPkgItem(element, pkg);
                svPkg.Add(element);
                pkgNames.Add(pkg.name);
            }
            UICodeGenSettingData.Check(pkgNames);
        }
        private UICodeFoldout MakePkgItem()
        {
            var foldout = new UICodeFoldout();
            foldout.name = "Foldout1";
            foldout.value = false;
            return foldout;
        }
        private void BindPkgItem(UICodeFoldout foldout, FairyGUI.UIPackage pkg)
        {
            foldout.pkg = pkg;
            foldout.text = pkg.name;
            foldout.Clear();
            selectItem = null;
            var itemDatas = UIEditorTools.GetPackageItems(pkg);
            foreach (var pi in itemDatas)
            {
                BindComponentItem(foldout, MakeComponentItem(), pi);
            }
        }
        void OnCodeButtonClick(UICodeButton button)
        {
            if (selectItem == button) return;
            var normalKeyWord = button.style.backgroundColor.keyword;
            StyleColor normal = new StyleColor(normalKeyWord);
            StyleColor clicked = new StyleColor(new Color(176f / 255, 88f / 255, 88f / 255));
            if (selectItem != null)
            {
                selectItem.style.backgroundColor = normal;
            }
            button.style.backgroundColor = clicked;
            selectItem = button;
            FreshComponentData();
        }
        private UICodeButton MakeComponentItem()
        {

            var element = new UICodeButton(OnCodeButtonClick);
            {
                var label = new Label();
                label.name = "Label1";
                label.style.unityTextAlign = TextAnchor.MiddleLeft;
                label.style.flexGrow = 1f;
                label.style.height = 20f;
                element.Add(label);
            }
            return element;
        }
        private void BindComponentItem(Foldout foldout, UICodeButton element, FairyGUI.PackageItem pi)
        {
            element.pi = pi;
            var textField1 = element.Q<Label>("Label1");
            textField1.text = pi.name;
            foldout.Add(element);
        }
        void FreshComponentData()
        {
            if (selectItem == null)
            {
                svMember.Clear();
                FreshComContaine();
                return;
            }
            var pi = selectItem.pi;
            var com = UIEditorTools.GetOrAddGComBy(pi);

            if (com == null)
            {
                svMember.Clear();
                return;
            }
            var data = UICodeGenSettingData.GetOrAddComponentData(com);
            selectItem.comData = data;
            var members = data.GetMembers();
            svMember.Clear();
            foreach (var member in members)
            {
                var element = new UICodeMemberItem(SaveSelectItemData);
                element.SetData(member);
                svMember.Add(element);
            }

            FreshComContaine();
        }
    }
}
