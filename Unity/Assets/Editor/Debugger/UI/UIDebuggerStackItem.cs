using System;
using UnityEngine;
using UnityEngine.UIElements;
namespace Ux.Editor.Debugger.UI
{
    public class UIDebuggerStackItem : TemplateContainer, IDebuggerListItem<Ux.UIMgr.UIStack>
    {
        Action<UIMgr.UIStack> _clickEvt;
        UIMgr.UIStack data;
        public UIDebuggerStackItem()
        {
            var element = new VisualElement();
            element.style.flexDirection = FlexDirection.Row;
            {
                var btn = new Button();
                btn.clicked += () =>
                {

                    _clickEvt?.Invoke(data);
                };
                var label = new Label();
                label.name = "Label0";
                label.style.unityTextAlign = TextAnchor.MiddleLeft;
                label.style.flexGrow = 1f;

                //label.style.width = 200;
                btn.Add(label);
                element.Add(btn);
            }
            Add(element);
        }

        public void SetClickEvt(Action<UIMgr.UIStack> action)
        {
            _clickEvt = action;
        }

        public void SetData(UIMgr.UIStack data)
        {
            this.data = data;
            var lb0 = this.Q<Label>("Label0");
            // 动态界面销毁后 UIData 会被移除，栈条目可能残留，这里不能走带错误日志的 GetUIData。
            lb0.text = UIMgr.Ins.IsHasUIData(data.ID) ? UIMgr.Ins.GetUIData(data.ID).Name : $"未注册UI({data.ID})";
        }
    }
}