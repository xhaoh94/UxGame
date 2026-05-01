using System;
using UnityEditor;
using UnityEngine.UIElements;
using static Ux.TimeMgr;
namespace Ux.Editor.Debugger.Time
{
    public partial class TimeDebuggerItemSub1 : TemplateContainer, IDebuggerListItem<TimeHandle>
    {
        public TimeDebuggerItemSub1()
        {
            CreateChildren();
            this.Add(root);
            CreateView();
        }

        /// <summary>
        /// 初始化页面
        /// </summary>
        void CreateView()
        {
            tgLoop.SetEnabled(false);
        }


        public virtual void SetData(TimeHandle data)
        {
            if (txtKey == null) return;
            txtKey.SetValueWithoutNotify(data.Key.ToString());

            if (!data.IsLoop)
            {
                tgLoop.style.display = DisplayStyle.None;
                txtTotaCnt.style.display = DisplayStyle.Flex;
                txtTotaCnt.SetValueWithoutNotify((data.ExeCnt + data.Repeat).ToString());
            }
            else
            {
                tgLoop.style.display = DisplayStyle.Flex;
                txtTotaCnt.style.display = DisplayStyle.None;
                tgLoop.SetValueWithoutNotify(data.IsLoop);
            }
            txtNext.SetValueWithoutNotify(data.ExeTime.ToString("#0.###"));
            txtExeCnt.SetValueWithoutNotify(data.ExeCnt.ToString());
            txtGap.SetValueWithoutNotify(data.Delay.ToString());
            lbType.text = data.UseFrame ? "帧" : "秒";
        }

        public void SetClickEvt(Action<TimeHandle> action)
        {

        }
    }

}
