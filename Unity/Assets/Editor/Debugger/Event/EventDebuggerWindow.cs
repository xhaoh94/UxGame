using System.Collections.Generic;
using UnityEditor;
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

        private DebuggerObjectSearchListView<EventDebuggerItem, EventList> _list;

        public void CreateGUI()
        {
            CreateChildren();
            rootVisualElement.Add(root);
            EventMgr.__Debugger_CallBack = OnUpdateData;
            _list = new DebuggerObjectSearchListView<EventDebuggerItem, EventList>(veList, 10);
            EventMgr.Debugger_Event();
        }

        private void OnUpdateData(Dictionary<string, EventList> dict)
        {
            _list.SetData(dict);
        }
    }
}