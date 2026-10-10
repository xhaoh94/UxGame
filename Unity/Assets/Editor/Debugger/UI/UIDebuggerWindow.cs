using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
namespace Ux.Editor.Debugger.UI
{
    public partial class UIDebuggerWindow : EditorWindow
    {
        [MenuItem("UxGame/调试/UI", false, 400)]
        public static void ShowExample()
        {
            var window = GetWindow<UIDebuggerWindow>("UI调试工具", true, DebuggerEditorDefine.DebuggerWindowTypes);
            window.minSize = new Vector2(800, 500);
        }

        private DebuggerObjectSearchListView<UIDebuggerItem, IUIData> _listUI;
        private DebuggerStringListView _listShowed;
        private DebuggerObjectListView<UIDebuggerStackItem, Ux.UIMgr.UIStack> _listStack;        
        private DebuggerStringListView _listCacel;        
        private DebuggerStringListView _listWaitDel;

        public void CreateGUI()
        {
            UIMgr.__Debugger_UI_CallBack = OnUpdateUI;
            UIMgr.__Debugger_Showed_CallBack = OnUpdateShowed;
            UIMgr.__Debugger_Stack_CallBack = OnUpdateStack;            
            UIMgr.__Debugger_Cacel_CallBack = OnUpdateCacel;            
            UIMgr.__Debugger_WaitDel_CallBack = OnUpdateWaitDel;

            BuildUI();
            rootVisualElement.Add(root);

            _listUI = new DebuggerObjectSearchListView<UIDebuggerItem, IUIData>(veList, 5);
            _listShowed = new DebuggerStringListView(listShowed, OnBtnClick);
            _listStack = new DebuggerObjectListView<UIDebuggerStackItem, UIMgr.UIStack>(listStack, OnBtnClick);            
            _listCacel = new DebuggerStringListView(listCacel, OnBtnClick);            
            _listWaitDel = new DebuggerStringListView(listWaitDel, OnBtnClick);

            UIMgr.__Debugger_Event();
        }
        protected VisualElement root;
        public ListView listStack;
        public ListView listShowed;
        public ListView listCacel;
        public ListView listWaitDel;
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

        /// <summary>原 UIDebuggerWindow.uxml 的手搭等价版本。</summary>
        private void BuildUI()
        {
            root = new VisualElement();
            root.style.flexDirection = FlexDirection.Row;

            var leftColumn = new VisualElement();
            leftColumn.style.flexGrow = 0f;
            leftColumn.style.width = 250f;
            root.Add(leftColumn);

            listStack = DebuggerUiUtil.DynamicList("listStack");
            leftColumn.Add(StackFoldout("队列", listStack));
            listShowed = DebuggerUiUtil.DynamicList("listShowed");
            leftColumn.Add(StackFoldout("已显示", listShowed));
            listCacel = DebuggerUiUtil.DynamicList("listCacel");
            leftColumn.Add(StackFoldout("缓存", listCacel));
            listWaitDel = DebuggerUiUtil.DynamicList("listWaitDel");
            leftColumn.Add(StackFoldout("待删除", listWaitDel));

            veList = new VisualElement { name = "veList" };
            veList.style.flexGrow = 1f;
            root.Add(veList);

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
            TopBar0 = new ToolbarButton { name = "TopBar0", text = "ID" };
            TopBar0.style.width = 205f;
            TopBar0.style.flexGrow = 0f;
            TopBar0.style.unityTextAlign = TextAnchor.MiddleLeft;
            TopBar.Add(TopBar0);
            TopBar1 = new ToolbarButton { name = "TopBar1", text = "UIData" };
            TopBar1.style.width = 150f;
            TopBar1.style.flexGrow = 1f;
            TopBar1.style.unityTextAlign = TextAnchor.MiddleLeft;
            TopBar.Add(TopBar1);
            veList.Add(TopBar);

            list = DebuggerUiUtil.DynamicList("list");
            list.style.flexGrow = 1f;
            veList.Add(list);

            vePage = new VisualElement { name = "vePage" };
            vePage.style.flexDirection = FlexDirection.Row;
            vePage.style.alignItems = Align.Center;
            vePage.style.justifyContent = Justify.Center;
            vePage.style.flexGrow = 1f;
            vePage.style.flexShrink = 0f;
            vePage.style.height = 20f;
            btnLast = new Button { name = "btnLast", text = "上一页" };
            btnLast.style.flexGrow = 0f;
            vePage.Add(btnLast);
            inputPage = new IntegerField { name = "inputPage", value = 1 };
            inputPage.style.width = 100f;
            inputPage.style.flexGrow = 0f;
            inputPage.style.flexShrink = 1f;
            vePage.Add(inputPage);
            txtPage = new Label("/10") { name = "txtPage" };
            txtPage.style.flexShrink = 1f;
            vePage.Add(txtPage);
            btnNext = new Button { name = "btnNext", text = "下一页" };
            btnNext.style.flexGrow = 0f;
            vePage.Add(btnNext);
            veList.Add(vePage);
        }

        private static Foldout StackFoldout(string title, ListView list)
        {
            var foldout = new Foldout { text = title, value = true };
            foldout.style.flexGrow = 0f;
            foldout.Add(list);
            return foldout;
        }

        private void OnBtnClick(string idStr)
        {
            _listUI.Search(idStr);
        }
        private void OnBtnClick(Ux.UIMgr.UIStack data)
        {
            _listUI.Search(UIMgr.Ins.GetUIData(data.ID).Name);
        }

        private void OnUpdateUI(Dictionary<string, IUIData> dict)
        {
            _listUI.SetData(dict);
        }
        private void OnUpdateShowed(List<string> list)
        {
            _listShowed.SetData(list);
        }
        private void OnUpdateStack(List<Ux.UIMgr.UIStack> list)
        {
            _listStack.SetData(list);
        }
        private void OnUpdateCacel(List<string> list)
        {
            _listCacel.SetData(list);
        }
        private void OnUpdateWaitDel(List<string> list)
        {
            _listWaitDel.SetData(list);
        }
    }

}
