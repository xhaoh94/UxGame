using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
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
            UIMgr.Ins.AddUIData(par);
            var data1 = new UITestData(333301, typeof(LoginTestSub), new UITestTabData(3333, "测试2"));
            UIMgr.Ins.AddUIData(data1);
            var data3 = new UITestData(333302, typeof(LoginTestSub), new UITestTabData(3333, "测试3"));
            UIMgr.Ins.AddUIData(data3);
            Test_EventCallAsync().Forget();
        }
        async UniTaskVoid Test_EventCallAsync()
        {
            Log.Info("Test_EventCall1111");            
            await EventMgr.Ins.Call(EventType.Test_EventCall, this).Task;            
            Log.Info("Test_EventCall2222");
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

    public class LoginTestUI : UIView
    {
        public string aaa;

        // 复用登录界面的 Prefab，覆盖默认的 UI_LoginTestUI。
        protected override string ResAddress => "UI_LoginView";

        protected override UILayer Layer => UILayer.View;

        protected override void OnShow()
        {
            base.OnShow();
        }
    }

    public class LoginTestSub : UITabView
    {
        // 复用 Multiple1TabView 的 Prefab。
        protected override string ResAddress => "UI_Multiple1TabView";
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