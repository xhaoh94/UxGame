using UnityEngine;
using UnityEngine.UIElements;
using Ux.Editor.State.Item;
namespace Ux.Editor.State
{
    public partial class StateConditionContent : TemplateContainer
    {
        StateConditionBase _currentCondition;
        IStateItem _item;
        StateConditinItem _content;
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
                _content = new StateConditinItem(ChangedCondition);
                {
                    _content.name = "Content";
                    _content.style.flexGrow = 1f;
                }
                element.Add(_content);
            }
            Add(element);
        }
        public void SetData(StateConditionBase currentCondition)
        {
            if (currentCondition == null)
            {
                StateWindow.SetMute(_currentCondition, true);                
                parent.Remove(this);
                return;
            }
            _currentCondition = currentCondition;
            _content.SetConditionType(_currentCondition.Condition);

            if (_item != null)
            {
                _item.SetData(_content, null);
                _item = null;
            }
            switch (_currentCondition.Condition)
            {
                case StateConditionBase.ConditionType.State:
                    _item = new StateItem();
                    break;
                case StateConditionBase.ConditionType.TempBoolVar:
                    _item = new TempBoolVarItem();
                    break;
                case StateConditionBase.ConditionType.Action_Keyboard:
                    _item = new ActionKeyboardItem();
                    break;
                case StateConditionBase.ConditionType.Action_Input:
                    _item = new ActionInputItem();
                    break;
                case StateConditionBase.ConditionType.Custom:
                    _item = new CustomItem();
                    break;
            }
            if (_item != null)
            {
                _item.SetData(_content, _currentCondition);
            }
        }
        void ChangedCondition(StateConditionBase.ConditionType conditionType)
        {
            if (_currentCondition != null)
            {
                StateWindow.SetMute(_currentCondition,true);
            }
            var changeCondition = StateWindow.Asset.conditions.Find(x => x.Condition == conditionType);
            if (changeCondition == null)
            {
                changeCondition = StateWindow.CreateCondition(conditionType);
                StateWindow.Asset.conditions.Add(changeCondition);
            }
            else
            {
                StateWindow.SetMute(changeCondition, false);                
            }
            SetData(changeCondition);
        }

    }
}
