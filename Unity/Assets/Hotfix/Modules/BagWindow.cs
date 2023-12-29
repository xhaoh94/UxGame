using FairyGUI;
using Ux;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ux.UI
{
    [UI]
    partial class BagWindow
    {
        public override UIType Type => UIType.Stack;
        protected override void OnShow(object param)
        {
            base.OnShow(param);
            //TimeMgr.Ins.DoOnce(2, () =>
            //{
            //    UIMgr.Ins.Show<MultipleView>();
            //});
        }
        protected override void OnHide()
        {
            base.OnHide();
        }
    }

    [UI]
    [Package("Bag", "Common")]
    [Lazyload("lazyload_bag")]
    partial class BagWindow2 : UIWindow
    {
        public override UIType Type => UIType.Stack;
        protected override string PkgName => "Bag";
        protected override string ResName => "BagWindow";

        protected Common2TabFrame mCommonBg;
        protected override void CreateChildren()
        {
            try
            {
                var gCom = ObjAs<Window>().contentPane;
                mCommonBg = new Common2TabFrame(gCom.GetChildAt(0), this);
            }
            catch (System.Exception e)
            {
                Log.Error(e);
            }
        }

        protected override void OnHide()
        {
            base.OnHide();
        }
    }
}
