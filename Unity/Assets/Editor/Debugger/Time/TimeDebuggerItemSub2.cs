using System;
using UnityEditor;
using UnityEngine.UIElements;
using static Ux.TimeMgr;
namespace Ux.Editor.Debugger.Time
{
    public partial class TimeDebuggerItemSub2:TemplateContainer{}
    public class TimeDebuggerItemSub2<T> : TimeDebuggerItemSub2, IDebuggerListItem<T>
    {
        public TimeDebuggerItemSub2()
        {
            CreateChildren();
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