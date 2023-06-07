using Ux;
using System;
using System.Collections.Generic;
namespace Ux
{
    [Module]
    public class TestHotFixMD : ModuleBase<TestHotFixMD>
    {
        protected override void OnInit()
        {
            base.OnInit();
            TTTChilds.Add(3333, new List<int>() { 333301, 333302 });
            var par = new UITestData(3333, typeof(LoginTestUI));
            UIMgr.Ins.RegisterUI(par);
            var data1 = new UITestData(333301, typeof(LoginTestSub), new UITestTabData(3333, "测试2"));
            UIMgr.Ins.RegisterUI(data1);
            var data3 = new UITestData(333302, typeof(LoginTestSub), new UITestTabData(3333, "测试3"));
            UIMgr.Ins.RegisterUI(data3);

        }
        protected override void OnRelease()
        {
            TTTChilds.Clear();
        }
        public static Dictionary<int, List<int>> TTTChilds = new Dictionary<int, List<int>>();
    }

    public class TTData
    {

    }

    [Package("Common", "Login")]
    public class LoginTestUI : UIView
    {
        public string aaa;

        protected override UILayer Layer => UILayer.Normal;

        protected override string PkgName => "Login";

        protected override string ResName => "LoginUI";

        protected override void OnShow(object param)
        {
            base.OnShow(param);
        }
    }

    public class LoginTestSub : UITabView
    {
        protected override string PkgName => "Login";

        protected override string ResName => "Test1";
    }

    public class UITestData : UIData
    {
        public UITestData(int id, Type type, IUITabData tabData = null) : base(id, type, tabData)
        {
        }

        public override List<int> Children
        {
            get
            {
                if (TestHotFixMD.TTTChilds.TryGetValue(ID, out var child))
                {
                    return child;
                }

                return null;
            }
        }
    }
    public class UITestTabData : UITabData
    {
        public UITestTabData(int pId, string title) : base(pId)
        {
            Title = title;
        }
    }
}