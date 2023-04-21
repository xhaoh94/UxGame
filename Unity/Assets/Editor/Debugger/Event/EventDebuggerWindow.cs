using Ux;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class EventDebuggerWindow : EditorWindow
{
    [MenuItem("Tools/调试工具/事件调试工具", false, 102)]
    public static void ShowExample()
    {
        var window = GetWindow<EventDebuggerWindow>("事件调试工具", true, EditorDefine.DebuggerWindowTypes);
        window.minSize = new Vector2(800, 500);
    }

    private DebuggerObjectSearchListView<EventDebuggerItem, EventList> _list;

    public void CreateGUI()
    {
        EventMgr.__Debugger_CallBack = OnUpdateData;
        VisualElement root = rootVisualElement;
        var visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Debugger/Event/EventDebuggerWindow.uxml");
        if (visualAsset == null) return;
        visualAsset.CloneTree(root);
        _list = new DebuggerObjectSearchListView<EventDebuggerItem, EventList>(root.Q<VisualElement>("veList"), 10);
        EventMgr.__Debugger_Event();
    }

    private void OnUpdateData(Dictionary<string, EventList> dict)
    {
        _list.SetData(dict);
    }
}