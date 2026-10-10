using System;
using UnityEditor;
using UnityEngine.UIElements;
using static Ux.TimeMgr;
namespace Ux.Editor.Debugger.Time
{
    public partial class TimeDebuggerItemSub2 : TemplateContainer
    {
        protected VisualElement root;
        public TextField txtKey;
        public TextField txtCorn;
        public TextField txtTimeDesc;
        public TextField txtTimeStamp;

        /// <summary>原 TimeDebuggerItemSub2.uxml 的手搭等价版本。</summary>
        protected void BuildUI()
        {
            root = new VisualElement();
            root.style.flexGrow = 1f;

            txtKey = DebuggerUiUtil.LockedField("txtKey", "Key");
            root.Add(txtKey);
            txtCorn = DebuggerUiUtil.LockedField("txtCorn", "Cron表达式");
            root.Add(txtCorn);
            txtTimeDesc = DebuggerUiUtil.LockedField("txtTimeDesc", "触发时间");
            root.Add(txtTimeDesc);
            txtTimeStamp = DebuggerUiUtil.LockedField("txtTimeStamp", "时间戳");
            root.Add(txtTimeStamp);
        }
    }

    public class TimeDebuggerItemSub2<T> : TimeDebuggerItemSub2, IDebuggerListItem<T>
    {
        public TimeDebuggerItemSub2()
        {
            BuildUI();
            Add(root);
        }

        public virtual void SetData(T data)
        {
        }

        public virtual void SetClickEvt(Action<T> action)
        {
        }
    }

    public class TimeDebuggerItemSub2Cron : TimeDebuggerItemSub2<CronHandle>
    {
        public override void SetData(CronHandle data)
        {
            base.SetData(data);
            txtKey.SetValueWithoutNotify(data.Key.ToString());
            txtTimeStamp.SetValueWithoutNotify(data.TimeStamp.ToString());
            txtTimeDesc.SetValueWithoutNotify(data.TimeStampDesc);
            txtCorn.style.display = DisplayStyle.Flex;
            txtCorn.SetValueWithoutNotify(data.Cron);
        }
    }
    public class TimeDebuggerItemSub2TimeStamp : TimeDebuggerItemSub2<TimeStampHandle>
    {
        public override void SetData(TimeStampHandle data)
        {
            base.SetData(data);
            txtKey.SetValueWithoutNotify(data.Key.ToString());
            txtTimeStamp.SetValueWithoutNotify(data.TimeStamp.ToString());
            txtTimeDesc.SetValueWithoutNotify(data.TimeStampDesc);
            txtCorn.style.display = DisplayStyle.None;
        }
    }
}