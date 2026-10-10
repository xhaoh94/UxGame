using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static Ux.TimeMgr;
namespace Ux.Editor.Debugger.Time
{
    partial class TimeSearchView
    {
        protected VisualElement root;
        public VisualElement veContent;
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

        public TimeSearchView(VisualElement parent)
        {
            BuildUI();
            parent.Add(root);
        }

        /// <summary>原 TimeSearchView.uxml 的手搭等价版本；uxml 根元素本身就是 veContent。</summary>
        private void BuildUI()
        {
            veContent = new VisualElement { name = "veContent" };
            veContent.style.flexGrow = 0f;
            root = veContent;

            var searchRow = DebuggerUiUtil.Row();
            inputSearch = new TextField("模糊搜索") { name = "inputSearch", pickingMode = PickingMode.Ignore };
            inputSearch.style.flexGrow = 1f;
            inputSearch.style.flexShrink = 0f;
            searchRow.Add(inputSearch);
            btnClear = new Button { name = "btnClear", text = "X" };
            searchRow.Add(btnClear);
            veContent.Add(searchRow);

            TopBar = new Toolbar { name = "TopBar" };
            TopBar.style.height = 25f;
            TopBar.style.marginLeft = 1f;
            TopBar.style.marginRight = 1f;
            TopBar0 = new ToolbarButton { name = "TopBar0", text = "执行方法" };
            TopBar0.style.width = 300f;
            TopBar0.style.flexGrow = 0f;
            TopBar0.style.unityTextAlign = TextAnchor.MiddleLeft;
            TopBar.Add(TopBar0);
            TopBar1 = new ToolbarButton { name = "TopBar1", text = "Handle" };
            TopBar1.style.width = 150f;
            TopBar1.style.flexGrow = 1f;
            TopBar1.style.unityTextAlign = TextAnchor.MiddleLeft;
            TopBar.Add(TopBar1);
            veContent.Add(TopBar);

            list = DebuggerUiUtil.DynamicList("list");
            list.style.flexGrow = 0f;
            veContent.Add(list);

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
            veContent.Add(vePage);
        }
        public static DebuggerObjectSearchListView<TimeDebuggerItem<A,B>, TimeList> Create<A, B>(VisualElement parent, int num) where A : TemplateContainer, IDebuggerListItem<B>, new()
        {
            var view = new TimeSearchView(parent);
            return new DebuggerObjectSearchListView<TimeDebuggerItem<A, B>, TimeList>(view.root, num);
        }
    }
    public partial class TimeDebuggerWindow : EditorWindow
    {
        [MenuItem("UxGame/调试/定时器", false, 403)]
        public static void ShowExample()
        {
            var window = GetWindow<TimeDebuggerWindow>("定时器调试工具", true, DebuggerEditorDefine.DebuggerWindowTypes);
            window.minSize = new Vector2(800, 500);
        }       

        ToolbarButton _tbBtnTime;
        ToolbarButton _tbBtnFrame;
        ToolbarButton _tbBtnTimeStamp;
        ToolbarButton _tbBtnCron;
        TimeType _timeType;

        TextField _txtTime;
        TextField _txtFrame;
        TextField _txtLocalTime;
        TextField _txtServerTime;

        DebuggerObjectSearchListView<TimeDebuggerItem<TimeDebuggerItemSub1, TimeHandle>, TimeList> _time;
        DebuggerObjectSearchListView<TimeDebuggerItem<TimeDebuggerItemSub1, TimeHandle>, TimeList> _frame;
        DebuggerObjectSearchListView<TimeDebuggerItem<TimeDebuggerItemSub2TimeStamp, TimeStampHandle>, TimeList> _timeStamp;
        DebuggerObjectSearchListView<TimeDebuggerItem<TimeDebuggerItemSub2Cron, CronHandle>, TimeList> _timeCron;

        protected VisualElement root;
        public TextField txtLocalTime;
        public TextField txtServerTime;
        public TextField txtTime;
        public Label Label;
        public TextField txtFrame;
        public ToolbarButton tbBtnTime;
        public ToolbarButton tbBtnFrame;
        public ToolbarButton tbBtnTimeStamp;
        public ToolbarButton tbBtnCron;
        public ScrollView scr;

        /// <summary>原 TimeDebuggerWindow.uxml 的手搭等价版本。</summary>
        private void BuildUI()
        {
            root = new VisualElement();

            txtLocalTime = DebuggerUiUtil.LockedField("txtLocalTime", "本地时间");
            txtLocalTime.style.flexGrow = 1f;
            txtLocalTime.style.flexShrink = 1f;
            root.Add(FieldRow(txtLocalTime));

            txtServerTime = DebuggerUiUtil.LockedField("txtServerTime", "服务器时间");
            txtServerTime.style.flexGrow = 1f;
            txtServerTime.style.flexShrink = 1f;
            root.Add(FieldRow(txtServerTime));

            txtTime = DebuggerUiUtil.LockedField("txtTime", "当前游戏运行总时间");
            txtTime.style.flexGrow = 1f;
            txtTime.style.flexShrink = 1f;
            var timeRow = FieldRow(txtTime);
            Label = new Label("秒") { name = "Label" };
            timeRow.Add(Label);
            root.Add(timeRow);

            var frameRow = DebuggerUiUtil.Row();
            frameRow.style.flexGrow = 1f;
            txtFrame = DebuggerUiUtil.LockedField("txtFrame", "当前游戏运行总帧数");
            txtFrame.style.flexGrow = 1f;
            frameRow.Add(txtFrame);
            frameRow.Add(new Label("帧"));
            root.Add(frameRow);

            var toolbar = new Toolbar();
            tbBtnTime = new ToolbarButton { name = "tbBtnTime", text = "时间" };
            toolbar.Add(tbBtnTime);
            tbBtnFrame = new ToolbarButton { name = "tbBtnFrame", text = "帧" };
            tbBtnFrame.style.flexDirection = FlexDirection.Column;
            toolbar.Add(tbBtnFrame);
            tbBtnTimeStamp = new ToolbarButton { name = "tbBtnTimeStamp", text = "时间戳" };
            toolbar.Add(tbBtnTimeStamp);
            tbBtnCron = new ToolbarButton { name = "tbBtnCron", text = "Cron表达式" };
            toolbar.Add(tbBtnCron);
            root.Add(toolbar);

            scr = new ScrollView { name = "scr" };
            scr.style.flexGrow = 1f;
            root.Add(scr);
        }

        private static VisualElement FieldRow(VisualElement field)
        {
            var row = DebuggerUiUtil.Row();
            row.style.alignItems = Align.Center;
            row.style.flexGrow = 1f;
            row.Add(field);
            return row;
        }

        public void CreateGUI()
        {
            __Debugger_Time_CallBack = OnUpdateTime;
            __Debugger_Frame_CallBack = OnUpdateFrame;
            __Debugger_TimeStamp_CallBack = OnUpdateTimeStamp;
            __Debugger_Cron_CallBack = OnUpdateCron;
            BuildUI();
            rootVisualElement.Add(root);

            _tbBtnTime = root.Q<ToolbarButton>("tbBtnTime");
            _tbBtnTime.clicked += () => { OnChangeType(TimeType.Time); };
            _tbBtnFrame = root.Q<ToolbarButton>("tbBtnFrame");
            _tbBtnFrame.clicked += () => { OnChangeType(TimeType.Frame); };
            _tbBtnTimeStamp = root.Q<ToolbarButton>("tbBtnTimeStamp");
            _tbBtnTimeStamp.clicked += () => { OnChangeType(TimeType.TimeStamp); };
            _tbBtnCron = root.Q<ToolbarButton>("tbBtnCron");
            _tbBtnCron.clicked += () => { OnChangeType(TimeType.Cron); };

            _txtTime = root.Q<TextField>("txtTime");
            _txtFrame = root.Q<TextField>("txtFrame");
            _txtLocalTime = root.Q<TextField>("txtLocalTime");
            _txtServerTime = root.Q<TextField>("txtServerTime");

            
            _time = TimeSearchView.Create< TimeDebuggerItemSub1 ,TimeHandle >(scr,4);
            _frame = TimeSearchView.Create<TimeDebuggerItemSub1, TimeHandle>(scr, 4);
            _timeStamp = TimeSearchView.Create<TimeDebuggerItemSub2TimeStamp, TimeStampHandle>(scr, 5); 
            _timeCron = TimeSearchView.Create<TimeDebuggerItemSub2Cron, CronHandle>(scr, 5);
            OnChangeType(TimeType.Time, true);
        }

        private void Update()
        {
            if (EditorApplication.isPlaying)
            {
                _txtTime.SetValueWithoutNotify(TimeMgr.Ins.TotalTime.ToString("#0.###"));
                _txtFrame.SetValueWithoutNotify(TimeMgr.Ins.TotalFrame.ToString());

                _txtLocalTime.SetValueWithoutNotify(TimeMgr.Ins.LocalTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff"));
                _txtServerTime.SetValueWithoutNotify(TimeMgr.Ins.ServerTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff"));
            }
        }
        private void OnChangeType(TimeType type, bool force = false)
        {
            if (_timeType != type || force)
            {
                _time.SetVisable(false);
                _frame.SetVisable(false);
                _timeStamp.SetVisable(false);
                _timeCron.SetVisable(false);
                _timeType = type;
                switch (type)
                {
                    case TimeType.Time:
                        _time.SetVisable(true);
                        __Debugger_Time_Event();
                        break;
                    case TimeType.Frame:
                        _frame.SetVisable(true);
                        __Debugger_Frame_Event();
                        break;
                    case TimeType.TimeStamp:
                        _timeStamp.SetVisable(true);
                        __Debugger_TimeStamp_Event();
                        break;
                    case TimeType.Cron:
                        _timeCron.SetVisable(true);
                        __Debugger_Cron_Event();
                        break;
                }
            }
        }

        void OnUpdateTime(Dictionary<string, TimeList> dict)
        {
            if (_timeType != TimeType.Time) return;
            _time.SetData(dict);
        }
        void OnUpdateFrame(Dictionary<string, TimeList> dict)
        {
            if (_timeType != TimeType.Frame) return;
            _frame.SetData(dict);
        }
        void OnUpdateTimeStamp(Dictionary<string, TimeList> dict)
        {
            if (_timeType != TimeType.TimeStamp) return;
            _timeStamp.SetData(dict);
        }
        void OnUpdateCron(Dictionary<string, TimeList> dict)
        {
            if (_timeType != TimeType.Cron) return;
            _timeCron.SetData(dict);
        }
    }
}