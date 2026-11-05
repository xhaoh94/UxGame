using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ux.UI
{
    [UI]
    partial class Stack4View
	{
        protected override UILayer Layer => UILayer.View;
        public override UIType Type => UIType.Stackable;
        partial void OnBtn1Click(UIEventContext e)
        {
            UIMgr.Ins.Show<UI.Stack1View>();
        }
        partial void OnBtn2Click(UIEventContext e)
        {
            UIMgr.Ins.Show<UI.Stack2View>();
        }
        partial void OnBtn3Click(UIEventContext e)
        {
            UIMgr.Ins.Show<UI.Stack3View>();
        }
        partial void OnBtnBackClick(UIEventContext e)
        {
            HideSelf();
        }
    }
}
