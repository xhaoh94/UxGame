using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using Ux;

public class StateConditionContent : TemplateContainer
{
    StateSettingData.StateCondition data;
    StateItemBase item;
    StateConditinItem content;
    public StateConditionContent()
    {
        VisualElement element = new VisualElement();
        {
            element.style.flexGrow = 1f;
            element.style.borderBottomColor = new StyleColor(new Color(80 / 255f, 200 / 255f, 80 / 255, 1));
            element.style.alignItems = Align.Center;
            element.style.flexDirection = FlexDirection.Row;
            var btn = new Button();
            btn.name = "Sub";
            btn.text = "[-]";
            btn.style.width = 30f;
            btn.style.height = 20;
            element.Add(btn);
            content = new StateConditinItem();
            {
                content.name = "Content";
                content.style.flexGrow = 1f;
            }
            element.Add(content);
        }
        Add(element);
    }
    public void SetData(StateSettingData.StateCondition enterCondition)
    {
        content.SetData(enterCondition, UpdateView);
        data = enterCondition;
        UpdateView();
    }
    public void UpdateView()
    {
        if (item != null)
        {
            content.Remove(item);
            item = null;
        }
        switch (data.Type)
        {
            case StateConditionBase.Type.State:
                item = new StateItem();
                break;
            case StateConditionBase.Type.TempBoolVar:
                item = new TempBoolVarItem();
                break;
            case StateConditionBase.Type.Action_Keyboard:
                item = new ActionKeyboardItem();
                break;
            case StateConditionBase.Type.Action_Input:
                item = new ActionInputItem();
                break;
        }
        if (item != null)
        {
            item.SetData(data);
            content.Add(item);
        }
    }
}