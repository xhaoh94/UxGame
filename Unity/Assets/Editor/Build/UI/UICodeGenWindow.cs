using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditor.UIElements;
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

        [MenuItem("UxGame/工具/UI/代码生成", false, 510)]
        public static void ShowExample()
        {
            var window = GetWindow<UICodeGenWindow>("UICodeGenWindow", true);
            window.minSize = new Vector2(800, 600);
        }


        private string lastPkg;
        private string lastRes;
        private UICodeButton selectItem;

        Dictionary<string, TextField> nameTextField = new Dictionary<string, TextField>();

        VisualElement tabViewElement;
        VisualElement messageBoxElement;
        VisualElement tipElement;
        VisualElement modelElement;

        protected VisualElement root;
        public TextField inputCodePath;
        public Button btnCodePath;
        public TextField inputNS;
        public Toggle tgIgnore;
        public IMGUIContainer top;
        public Button btnFresh;
        public TextField inputSearch;
        public Button btnSearch;
        public IMGUIContainer mid;
        public IMGUIContainer left;
        public Button btnHide;
        public Button btnShow;
        public ScrollView svPkg;
        public ScrollView svMember;
        public IMGUIContainer right;
        public IMGUIContainer comContaine;
        public DropdownField ddExt;
        public Toggle tgExport;
        public VisualElement elementExport;
        public Toggle tgUseGlobal;
        public IMGUIContainer settContainer;
        public Toggle tgIgnore_select;
        public TextField inputNS_select;
        public TextField inputCodePath_select;
        public Button btnCodePath_select;
        public TextField inputClsName;
        public VisualElement elementContent;
        public Button btnGenSelectItem;
        public Button btnGenAll;

        /// <summary>原 UICodeGenWindow.uxml 的手搭等价版本。
        /// GenCode 里注册、主类却没实现的回调（inputSearch 与 _select 那组的控件）不能注册 —— 原本就被编译器擦除。</summary>
        private void BuildUI()
        {
            root = new IMGUIContainer();
            root.style.flexGrow = 1f;

            var globalFoldout = new Foldout { text = "全局设置", value = false };
            globalFoldout.style.flexGrow = 0f;
            root.Add(globalFoldout);

            var codePathRow = new IMGUIContainer();
            codePathRow.style.flexDirection = FlexDirection.Row;
            codePathRow.style.alignItems = Align.Center;
            codePathRow.style.flexGrow = 0f;
            inputCodePath = new TextField("代码生成路径") { name = "inputCodePath", pickingMode = PickingMode.Ignore };
            inputCodePath.style.flexGrow = 1f;
            inputCodePath.RegisterValueChangedCallback(_OnInputCodePathChanged);
            codePathRow.Add(inputCodePath);
            btnCodePath = new Button(_OnBtnCodePathClick) { name = "btnCodePath", text = "修改" };
            btnCodePath.style.width = 100f;
            codePathRow.Add(btnCodePath);
            globalFoldout.Add(codePathRow);

            inputNS = new TextField("命名空间") { name = "inputNS", pickingMode = PickingMode.Ignore };
            inputNS.RegisterValueChangedCallback(_OnInputNSChanged);
            globalFoldout.Add(inputNS);

            tgIgnore = new Toggle("忽略默认命名的字段") { name = "tgIgnore" };
            tgIgnore.style.flexGrow = 0f;
            tgIgnore.RegisterValueChangedCallback(_OnTgIgnoreChanged);
            globalFoldout.Add(tgIgnore);

            top = new IMGUIContainer { name = "top" };
            top.style.flexDirection = FlexDirection.Row;
            top.style.justifyContent = Justify.SpaceBetween;
            root.Add(top);
            btnFresh = new Button(_OnBtnFreshClick) { name = "btnFresh", text = "刷新资源" };
            top.Add(btnFresh);
            var searchRow = new IMGUIContainer();
            searchRow.style.flexDirection = FlexDirection.RowReverse;
            searchRow.style.flexGrow = 1f;
            inputSearch = new TextField { name = "inputSearch", pickingMode = PickingMode.Ignore };
            inputSearch.style.flexGrow = 1f;
            searchRow.Add(inputSearch);
            btnSearch = new Button(_OnBtnSearchClick) { name = "btnSearch", text = "搜索组件" };
            searchRow.Add(btnSearch);
            top.Add(searchRow);

            mid = new IMGUIContainer { name = "mid" };
            mid.style.flexDirection = FlexDirection.Row;
            mid.style.flexGrow = 1f;
            root.Add(mid);

            left = new IMGUIContainer { name = "left" };
            left.style.flexDirection = FlexDirection.Row;
            left.style.width = 550f;
            left.style.flexGrow = 0f;
            mid.Add(left);

            var packageColumn = new IMGUIContainer();
            packageColumn.style.flexDirection = FlexDirection.Column;
            packageColumn.style.flexGrow = 1f;
            left.Add(packageColumn);

            var hideShowRow = new IMGUIContainer();
            hideShowRow.style.flexDirection = FlexDirection.RowReverse;
            packageColumn.Add(hideShowRow);
            btnHide = new Button(_OnBtnHideClick) { name = "btnHide", text = "收缩" };
            btnHide.style.width = 30f;
            hideShowRow.Add(btnHide);
            btnShow = new Button(_OnBtnShowClick) { name = "btnShow", text = "展开" };
            btnShow.style.width = 30f;
            hideShowRow.Add(btnShow);

            svPkg = new ScrollView { name = "svPkg" };
            svPkg.style.flexGrow = 1f;
            packageColumn.Add(svPkg);

            var memberColumn = new IMGUIContainer();
            memberColumn.style.backgroundColor = new Color(56f / 255f, 56f / 255f, 56f / 255f);
            memberColumn.style.flexGrow = 1f;
            left.Add(memberColumn);
            var memberLabel = new Label("成员变量");
            memberLabel.style.borderLeftWidth = 1f;
            memberLabel.style.borderRightWidth = 1f;
            memberLabel.style.borderTopWidth = 1f;
            memberLabel.style.borderBottomWidth = 1f;
            var borderColor = new Color(144f / 255f, 144f / 255f, 144f / 255f);
            memberLabel.style.borderLeftColor = borderColor;
            memberLabel.style.borderRightColor = borderColor;
            memberLabel.style.borderTopColor = borderColor;
            memberLabel.style.borderBottomColor = borderColor;
            memberColumn.Add(memberLabel);
            svMember = new ScrollView { name = "svMember" };
            svMember.style.flexGrow = 1f;
            svMember.style.backgroundColor = new Color(56f / 255f, 56f / 255f, 56f / 255f);
            memberColumn.Add(svMember);

            right = new IMGUIContainer { name = "right" };
            right.style.flexGrow = 1f;
            right.style.backgroundColor = new Color(0f, 0f, 0f, 0.18f);
            mid.Add(right);

            comContaine = new IMGUIContainer { name = "comContaine" };
            right.Add(comContaine);

            ddExt = new DropdownField("继承类型") { name = "ddExt" };
            ddExt.RegisterValueChangedCallback(_OnDdExtChanged);
            comContaine.Add(ddExt);

            tgExport = new Toggle("是否导出") { name = "tgExport", value = true };
            tgExport.style.flexGrow = 0f;
            tgExport.RegisterValueChangedCallback(_OnTgExportChanged);
            comContaine.Add(tgExport);

            elementExport = new VisualElement { name = "elementExport" };
            comContaine.Add(elementExport);

            tgUseGlobal = new Toggle("使用全局设置") { name = "tgUseGlobal", value = true };
            tgUseGlobal.style.flexGrow = 0f;
            tgUseGlobal.RegisterValueChangedCallback(_OnTgUseGlobalChanged);
            elementExport.Add(tgUseGlobal);

            settContainer = new IMGUIContainer { name = "settContainer" };
            elementExport.Add(settContainer);

            tgIgnore_select = new Toggle("忽略默认命名的字段") { name = "tgIgnore_select" };
            tgIgnore_select.style.flexGrow = 0f;
            tgIgnore_select.RegisterValueChangedCallback(_OnTgIgnore_selectChanged);
            settContainer.Add(tgIgnore_select);

            inputNS_select = new TextField("命名空间") { name = "inputNS_select", pickingMode = PickingMode.Ignore };
            inputNS_select.RegisterValueChangedCallback(_OnInputNS_selectChanged);
            settContainer.Add(inputNS_select);

            var selectPathRow = new IMGUIContainer();
            selectPathRow.style.flexDirection = FlexDirection.Row;
            selectPathRow.style.alignItems = Align.Center;
            selectPathRow.style.flexGrow = 0f;
            inputCodePath_select = new TextField("代码生成路径") { name = "inputCodePath_select", pickingMode = PickingMode.Ignore };
            inputCodePath_select.style.flexGrow = 1f;
            inputCodePath_select.RegisterValueChangedCallback(_OnInputCodePath_selectChanged);
            selectPathRow.Add(inputCodePath_select);
            btnCodePath_select = new Button(_OnBtnCodePath_selectClick) { name = "btnCodePath_select", text = "修改" };
            selectPathRow.Add(btnCodePath_select);
            settContainer.Add(selectPathRow);

            inputClsName = new TextField("类名") { name = "inputClsName", pickingMode = PickingMode.Ignore };
            inputClsName.RegisterValueChangedCallback(_OnInputClsNameChanged);
            elementExport.Add(inputClsName);

            elementContent = new VisualElement { name = "elementContent" };
            elementExport.Add(elementContent);

            btnGenSelectItem = new Button(_OnBtnGenSelectItemClick) { name = "btnGenSelectItem", text = "导出选中组件" };
            elementExport.Add(btnGenSelectItem);

            btnGenAll = new Button(_OnBtnGenAllClick) { name = "btnGenAll", text = "导出全部的包" };
            comContaine.Add(btnGenAll);
        }

        public void CreateGUI()
        {
            UICodeGenSettingData.Load();
            BuildUI();
            rootVisualElement.Add(root);
            inputCodePath.SetValueWithoutNotify(UICodeGenSettingData.CodeGenPath);
            inputNS.SetValueWithoutNotify(UICodeGenSettingData.DefaultNs);
            tgIgnore.SetValueWithoutNotify(UICodeGenSettingData.IngoreDefault);
            comContaine.style.display = DisplayStyle.None;
            settContainer.style.display = DisplayStyle.None;

            var keys = UIEditorTools.CheckExt();
            ddExt.choices = keys;
            ddExt.index = 0;


            tabViewElement = new VisualElement();
            elementContent.Add(tabViewElement);

            messageBoxElement = new VisualElement();
            elementContent.Add(messageBoxElement);

            tipElement = new VisualElement();
            elementContent.Add(tipElement);

            modelElement = new VisualElement();
            elementContent.Add(modelElement);

            FreshPkgData();
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
            var code = RuntimeHelpers.GetHashCode(parent);

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
            var code = RuntimeHelpers.GetHashCode(parent);
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

        ///////////////////////////////////////////////////////////
        private void _OnInputCodePathChanged(ChangeEvent<string> e)
        {
            FreshComponentData();
        }
        private void _OnBtnCodePathClick()
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
        private void _OnInputNSChanged(ChangeEvent<string> e)
        {
            UICodeGenSettingData.DefaultNs = e.newValue;
        }
        private void _OnTgIgnoreChanged(ChangeEvent<bool> e)
        {
            UICodeGenSettingData.IngoreDefault = e.newValue;
            FreshComponentData();
        }
        private void _OnBtnShowClick()
        {
            for (int i = 0; i < svPkg.childCount; i++)
            {
                var foldout = svPkg.ElementAt(i) as UICodeFoldout;
                foldout.value = true;
            }
        }
        private void _OnBtnHideClick()
        {
            for (int i = 0; i < svPkg.childCount; i++)
            {
                var foldout = svPkg.ElementAt(i) as UICodeFoldout;
                foldout.value = false;
            }
        }
        private void _OnBtnFreshClick()
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
        }
        private void _OnBtnSearchClick()
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
        private void _OnTgExportChanged(ChangeEvent<bool> e)
        {
            SaveSelectItemData();
        }
        private void _OnTgUseGlobalChanged(ChangeEvent<bool> e)
        {
            SaveSelectItemData();
        }
        private void _OnInputCodePath_selectChanged(ChangeEvent<string> e)
        {
            SaveSelectItemData();
        }
        private void _OnBtnCodePath_selectClick()
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
        private void _OnTgIgnore_selectChanged(ChangeEvent<bool> e)
        {
            SaveSelectItemData();
        }
        private void _OnInputNS_selectChanged(ChangeEvent<string> e)
        {
            SaveSelectItemData();
        }
        private void _OnInputClsNameChanged(ChangeEvent<string> e)
        {
            SaveSelectItemData();
        }
        private void _OnDdExtChanged(ChangeEvent<string> e)
        {
            SaveSelectItemData();
        }
        private void _OnBtnGenSelectItemClick()
        {
            if (selectItem != null)
            {
                if (UIEditorTools.GetGComBy(selectItem.pi, out var com) && OnExport(com))
                {
                    Log.Debug("UI代码生成");
                    EditorUtility.DisplayDialog("提示", "生成选中代码成功", "确定");
                }
                else
                {
                    EditorUtility.DisplayDialog("提示", "生成选中代码失败", "确定");
                }
            }
        }
        private void _OnBtnGenAllClick()
        {
            var genPath = UICodeGenSettingData.CodeGenPath;
            if (Directory.Exists(genPath))
            {
                Directory.Delete(genPath, true);
            }
            if (Export())
            {
                EditorUtility.DisplayDialog("提示", "生成所有代码成功", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("提示", "生成所有代码失败", "确定");
            }
        }
    }
}
