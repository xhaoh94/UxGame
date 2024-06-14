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
            lb0.text = data.IDStr;
        }
    }
}