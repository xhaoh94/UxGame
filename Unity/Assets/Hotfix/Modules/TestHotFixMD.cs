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

            var par = new UITestData("3333", typeof(LoginTestUI), new[] { "Common", "Login" }, null);
            var data1 = new UITestData("333301", typeof(LoginTestSub), null, null, new UITabData("3333", "测试1"));
            var data3 = new UITestData("333302", typeof(LoginTestSub), null, null, new UITabData("3333", "测试3"));
            TTTChilds.Add("3333", new List<string>() { data1.ID, data3.ID });
            UIMgr.Instance.RegisterUI(par);
            UIMgr.Instance.RegisterUI(data1);
            UIMgr.Instance.RegisterUI(data3);
        }
        protected override void OnRelease()
        {
            TTTChilds.Clear();
        }
        public static Dictionary<string, List<string>> TTTChilds = new Dictionary<string, List<string>>();        
    }

    public class TTData
    {

    }

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
        public UITestData(string id, Type type, string[] pkgs, string[] lazyloads, IUITabData tabData = null) : base(id, type, pkgs, lazyloads, tabData)
        {
        }

        public override List<string> Children
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
}