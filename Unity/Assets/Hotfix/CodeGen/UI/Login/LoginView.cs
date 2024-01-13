//自动生成的代码，请勿修改!!!
using FairyGUI;
namespace Ux.UI
{
	[Package("Login","Common")]
	public partial class LoginView : UIView
	{
		protected override string PkgName => "Login";
		protected override string ResName => "LoginView";

		protected Btn1 btnLogin;
		protected GTextInput inputAcc;
		protected SingleBtn camp1;
		protected SingleBtn camp2;
		protected SingleBtn camp3;
		protected Controller camp;
		protected override void CreateChildren()
		{
			try
			{
				var gCom = ObjAs<GComponent>();
				btnLogin = new Btn1(gCom.GetChildAt(2), this);
				inputAcc = (GTextInput)gCom.GetChildAt(4);
				camp1 = new SingleBtn(gCom.GetChildAt(6), this);
				camp2 = new SingleBtn(gCom.GetChildAt(7), this);
				camp3 = new SingleBtn(gCom.GetChildAt(8), this);
				camp = gCom.GetControllerAt(0);
			}
			catch (System.Exception e)
			{
				 Log.Error(e);
			}
		}
		protected override void OnAddEvent()
		{
			AddClick(btnLogin,_OnBtnLoginClick);
		}
		void _OnBtnLoginClick(EventContext e)
		{
			OnBtnLoginClick(e);
		}
		partial void OnBtnLoginClick(EventContext e);
	}
}
