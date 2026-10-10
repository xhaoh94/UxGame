using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
namespace Ux.Editor.Debugger.Res
{
    public partial class ResDebuggerWindow : EditorWindow
    {
        [MenuItem("UxGame/调试/资源", false, 401)]
        public static void ShowExample()
        {
            var window = GetWindow<ResDebuggerWindow>("资源调试工具", true, DebuggerEditorDefine.DebuggerWindowTypes);
            window.minSize = new Vector2(800, 500);
        }

        protected VisualElement root;
        public VisualElement veList;
        public TextField inputSearch;
        public Button btnClear;
        public Toolbar TopBar;
        public ToolbarButton TopBar0;
        public ToolbarButton TopBar1;
        public ListView list;
        public VisualElement vePage;
        public Button btnLast;
        public IntegerField inputPage;
        public Label txtPage;
        public Button btnNext;

        DebuggerObjectSearchListView<ResDebuggerItem, UIPkgRef> _listPackageRef;


        private void OnDestroy()
        {
            UIMgr.__Debugger_Pkg_CallBack = null;
        }
        public void CreateGUI()
        {
            UIMgr.__Debugger_Pkg_CallBack = OnUpdateData;
            BuildUI();
            rootVisualElement.Add(root);

            _listPackageRef = new DebuggerObjectSearchListView<ResDebuggerItem, UIPkgRef>(veList);
            UIMgr.__Debugger_Pkg_Event();
        }

        /// <summary>原 ResDebuggerWindow.uxml 的手搭等价版本。</summary>
        private void BuildUI()
        {
            root = new VisualElement();
            root.style.flexGrow = 1f;

            var foldout = new Foldout { text = "FGUI" };
            root.Add(foldout);

            veList = new VisualElement { name = "veList" };
            foldout.Add(veList);

            var searchRow = DebuggerUiUtil.Row();
            inputSearch = new TextField("模糊搜索") { name = "inputSearch", pickingMode = PickingMode.Ignore };
            inputSearch.style.flexGrow = 1f;
            inputSearch.style.flexShrink = 0f;
            searchRow.Add(inputSearch);
            btnClear = new Button { name = "btnClear", text = "X" };
            searchRow.Add(btnClear);
            veList.Add(searchRow);

            TopBar = new Toolbar { name = "TopBar" };
            TopBar.style.height = 25f;
            TopBar.style.marginLeft = 1f;
            TopBar.style.marginRight = 1f;
            TopBar0 = new ToolbarButton { name = "TopBar0", text = "Package Name" };
            TopBar0.style.width = 250f;
            TopBar0.style.flexGrow = 0f;
            TopBar0.style.unityTextAlign = TextAnchor.MiddleLeft;
            TopBar.Add(TopBar0);
            TopBar1 = new ToolbarButton { name = "TopBar1", text = "引用计数" };
            TopBar1.style.width = 150f;
            TopBar1.style.flexGrow = 1f;
            TopBar1.style.unityTextAlign = TextAnchor.MiddleLeft;
            TopBar.Add(TopBar1);
            veList.Add(TopBar);

            list = DebuggerUiUtil.DynamicList("list");
            veList.Add(list);

            vePage = new VisualElement { name = "vePage" };
            vePage.style.flexDirection = FlexDirection.Row;
            vePage.style.alignItems = Align.Center;
            vePage.style.justifyContent = Justify.Center;
            btnLast = new Button { name = "btnLast", text = "上一页" };
            vePage.Add(btnLast);
            inputPage = new IntegerField { name = "inputPage", value = 1 };
            inputPage.style.width = 100f;
            vePage.Add(inputPage);
            txtPage = new Label("/10") { name = "txtPage" };
            vePage.Add(txtPage);
            btnNext = new Button { name = "btnNext", text = "下一页" };
            vePage.Add(btnNext);
            veList.Add(vePage);
        }

        private void OnUpdateData(Dictionary<string, UIPkgRef> dict)
        {
            _listPackageRef.SetData(dict);
        }
    }

    public class ResDebuggerItem : TemplateContainer, IDebuggerListItem<UIPkgRef>
    {
        public ResDebuggerItem()
        {
            style.flexDirection = FlexDirection.Row;
            {
                var label = new Label();
                label.name = "Label0";
                label.style.unityTextAlign = TextAnchor.MiddleLeft;
                label.style.marginLeft = 3f;
                //label.style.flexGrow = 1f;
                label.style.width = 250;
                Add(label);
            }

            {
                var label = new Label();
                label.name = "Label1";
                label.style.unityTextAlign = TextAnchor.MiddleLeft;
                label.style.marginLeft = 3f;
                label.style.flexGrow = 1f;
                label.style.width = 150;
                Add(label);
            }
        }

        public void SetClickEvt(Action<UIPkgRef> action)
        {

        }

        public void SetData(UIPkgRef data)
        {
            var lb0 = this.Q<Label>("Label0");
            lb0.text = data.PkgName;
            var lb1 = this.Q<Label>("Label1");
            lb1.text = data.RefCnt.ToString();
        }
    }
}