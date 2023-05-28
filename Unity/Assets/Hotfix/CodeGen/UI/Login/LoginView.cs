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
		protected GTextInput inputPass;
		protected override void CreateChildren()
		{
			var gCom = ObjAs<GComponent>();
			btnLogin = new Btn1(gCom.GetChildAt(2), this);
			inputAcc = (GTextInput)gCom.GetChildAt(3);
			inputPass = (GTextInput)gCom.GetChildAt(5);
			OnShowCallBack += OnAddEvent;
		}
		protected void OnAddEvent()
		{
			btnLogin.AddClick(e => OnBtnLoginClick(e));
		}
		partial void OnBtnLoginClick(EventContext e);
	}
}
