//自动生成的代码，请勿修改!!!
using FairyGUI;
namespace Ux.UI
{
	[Package("Test","Common")]
	public partial class Stack3View : UIView
	{
		protected override string PkgName => "Test";
		protected override string ResName => "Stack3View";

		protected Btn1 btnBack;
		protected Btn1 btn1;
		protected Btn1 btn2;
		protected Btn1 btn4;
		protected override void CreateChildren()
		{
			try
			{
				var gCom = ObjAs<GComponent>();
				btnBack = new Btn1(gCom.GetChildAt(2), this);
				btn1 = new Btn1(gCom.GetChildAt(3), this);
				btn2 = new Btn1(gCom.GetChildAt(4), this);
				btn4 = new Btn1(gCom.GetChildAt(5), this);
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
			AddClick(btn2,_OnBtn2Click);
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
		void _OnBtn2Click(EventContext e)
		{
			OnBtn2Click(e);
		}
		partial void OnBtn2Click(EventContext e);
		void _OnBtn4Click(EventContext e)
		{
			OnBtn4Click(e);
		}
		partial void OnBtn4Click(EventContext e);
	}
}
