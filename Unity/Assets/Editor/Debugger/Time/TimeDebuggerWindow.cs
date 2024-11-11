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
        public TimeSearchView(VisualElement parent)
        {
            CreateChildren();
            parent.Add(root);
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

        public void CreateGUI()
        {
            __Debugger_Time_CallBack = OnUpdateTime;
            __Debugger_Frame_CallBack = OnUpdateFrame;
            __Debugger_TimeStamp_CallBack = OnUpdateTimeStamp;
            __Debugger_Cron_CallBack = OnUpdateCron;
            CreateChildren();
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
                _txtTime.SetValueWithoutNotify(TimeMgr.Ins.TotalTime.ToString());
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