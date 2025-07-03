using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
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
        private DebuggerStringListView _listShowing;
        private DebuggerStringListView _listCacel;
        private DebuggerStringListView _listTemCacel;
        private DebuggerStringListView _listWaitDel;

        public void CreateGUI()
        {
            UIMgr.__Debugger_UI_CallBack = OnUpdateUI;
            UIMgr.__Debugger_Showed_CallBack = OnUpdateShowed;
            UIMgr.__Debugger_Stack_CallBack = OnUpdateStack;
            UIMgr.__Debugger_Showing_CallBack = OnUpdateShowing;
            UIMgr.__Debugger_Cacel_CallBack = OnUpdateCacel;
            UIMgr.__Debugger_TemCacel_CallBack = OnUpdateTemCacel;
            UIMgr.__Debugger_WaitDel_CallBack = OnUpdateWaitDel;

            CreateChildren();
            rootVisualElement.Add(root);

            _listUI = new DebuggerObjectSearchListView<UIDebuggerItem, IUIData>(veList, 5);
            _listShowed = new DebuggerStringListView(listShowed, OnBtnClick);
            _listStack = new DebuggerObjectListView<UIDebuggerStackItem, UIMgr.UIStack>(listStack, OnBtnClick);
            _listShowing = new DebuggerStringListView(listShowing, OnBtnClick);
            _listCacel = new DebuggerStringListView(listCacel, OnBtnClick);
            _listTemCacel = new DebuggerStringListView(listTemCacel, OnBtnClick);
            _listWaitDel = new DebuggerStringListView(listWaitDel, OnBtnClick);

            UIMgr.__Debugger_Event();
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
        private void OnUpdateShowing(List<string> list)
        {
            _listShowing.SetData(list);
        }
        private void OnUpdateCacel(List<string> list)
        {
            _listCacel.SetData(list);
        }
        private void OnUpdateTemCacel(List<string> list)
        {
            _listTemCacel.SetData(list);
        }
        private void OnUpdateWaitDel(List<string> list)
        {
            _listWaitDel.SetData(list);
        }
    }

}
