using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
namespace Ux.Editor.Debugger.Event
{
    public partial class EventDebuggerWindow : EditorWindow
    {
        [MenuItem("UxGame/调试/事件", false, 402)]
        public static void ShowExample()
        {
            var window = GetWindow<EventDebuggerWindow>("事件调试工具", true, DebuggerEditorDefine.DebuggerWindowTypes);
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

        private DebuggerObjectSearchListView<EventDebuggerItem, EventList> _list;

        public void CreateGUI()
        {
            BuildUI();
            rootVisualElement.Add(root);
            EventMgr.__Debugger_CallBack = OnUpdateData;
            _list = new DebuggerObjectSearchListView<EventDebuggerItem, EventList>(veList, 10);
            EventMgr.Debugger_Event();
        }

        /// <summary>原 EventDebuggerWindow.uxml 的手搭等价版本。</summary>
        private void BuildUI()
        {
            root = new VisualElement();
            root.style.flexGrow = 1f;

            veList = new VisualElement { name = "veList" };
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
            TopBar0 = new ToolbarButton { name = "TopBar0", text = "事件ID" };
            TopBar0.style.width = 305f;
            TopBar0.style.flexGrow = 0f;
            TopBar0.style.unityTextAlign = TextAnchor.MiddleLeft;
            TopBar.Add(TopBar0);
            TopBar1 = new ToolbarButton { name = "TopBar1", text = "事件" };
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

        private void OnUpdateData(Dictionary<string, EventList> dict)
        {
            _list.SetData(dict);
        }
    }
}