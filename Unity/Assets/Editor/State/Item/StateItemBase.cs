using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ux.Editor.State.Item
{
    public interface IStateItem
    {
        void SetData(TemplateContainer parent, object data);
    }
    public class StateItemBase<T> : TemplateContainer, IStateItem where T : StateConditionBase
    {
        protected T ConditionData { get; private set; }
        public void SetData(TemplateContainer parent, object data)
        {
            if (data == null)
            {
                parent.Remove(this);
                return;
            }
            parent.Add(this);
            ConditionData = (T)data;
            OnData();
        }
        protected virtual void OnData()
        {

        }
    }
}