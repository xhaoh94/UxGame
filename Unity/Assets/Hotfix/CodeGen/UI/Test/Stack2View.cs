//自动生成的代码，请勿修改!!!
using FairyGUI;
namespace Ux.UI
{
	[Package("Test","Common")]
	public partial class Stack2View : UIView
	{
		protected override string PkgName => "Test";
		protected override string ResName => "Stack2View";
		protected UIButton btnBack;
		protected UIButton btn1;
		protected UIButton btn3;
		protected UIButton btn4;
		protected override void CreateChildren()
		{
			try
			{
				var gCom = ObjAs<GComponent>();
				btnBack = new UIButton(gCom.GetChildAt(2), this);
				btn1 = new UIButton(gCom.GetChildAt(3), this);
				btn3 = new UIButton(gCom.GetChildAt(4), this);
				btn4 = new UIButton(gCom.GetChildAt(5), this);
			}
			catch (System.Exception e)
			{
				 Log.Error(e);
			}
		}
		protected override void OnAddEvent()
		{
			AddClick(btnBack,_OnBtnBackClick);
			AddClick(btn1,_OnBtn1Click);
			AddClick(btn3,_OnBtn3Click);
			AddClick(btn4,_OnBtn4Click);
		}
		void _OnBtnBackClick(EventContext e)
		{
			OnBtnBackClick(e);
		}
		partial void OnBtnBackClick(EventContext e);
		void _OnBtn1Click(EventContext e)
		{
			OnBtn1Click(e);
		}
		partial void OnBtn1Click(EventContext e);
		void _OnBtn3Click(EventContext e)
		{
			OnBtn3Click(e);
		}
		partial void OnBtn3Click(EventContext e);
		void _OnBtn4Click(EventContext e)
		{
			OnBtn4Click(e);
		}
		partial void OnBtn4Click(EventContext e);
	}
}
