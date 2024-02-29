//自动生成的代码，请勿修改!!!
using FairyGUI;
namespace Ux.UI
{
	[Package("Login","Common")]
	public partial class LoginView : UIView
	{
		protected override string PkgName => "Login";
		protected override string ResName => "LoginView";
		protected UIButton btnLogin;
		protected GTextInput inputAcc;
		protected UIButton camp1;
		protected UIButton camp2;
		protected UIButton camp3;
		protected Controller camp;
		protected override void CreateChildren()
		{
			try
			{
				var gCom = ObjAs<GComponent>();
				btnLogin = new UIButton(gCom.GetChildAt(2), this);
				inputAcc = (GTextInput)gCom.GetChildAt(4);
				camp1 = new UIButton(gCom.GetChildAt(6), this);
				camp2 = new UIButton(gCom.GetChildAt(7), this);
				camp3 = new UIButton(gCom.GetChildAt(8), this);
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
