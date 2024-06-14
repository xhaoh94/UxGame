using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
namespace Ux.Editor.Debugger.Event
{
    public class EventDebuggerWindow : EditorWindow
    {
        [MenuItem("UxGame/调试/事件", false, 402)]
        public static void ShowExample()
        {
            var window = GetWindow<EventDebuggerWindow>("事件调试工具", true, DebuggerEditorDefine.DebuggerWindowTypes);
            window.minSize = new Vector2(800, 500);
        }
        [SerializeField]
        private VisualTreeAsset m_VisualTreeAsset = default;

        private DebuggerObjectSearchListView<EventDebuggerItem, EventList> _list;

        public void CreateGUI()
        {
            EventMgr.__Debugger_CallBack = OnUpdateData;
            VisualElement root = rootVisualElement;
            m_VisualTreeAsset.CloneTree(root);
            _list = new DebuggerObjectSearchListView<EventDebuggerItem, EventList>(root.Q<VisualElement>("veList"), 10);
            EventMgr.__Debugger_Event();
        }

        private void OnUpdateData(Dictionary<string, EventList> dict)
        {
            _list.SetData(dict);
        }
    }
}