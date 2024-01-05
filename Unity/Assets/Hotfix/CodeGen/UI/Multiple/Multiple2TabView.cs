//自动生成的代码，请勿修改!!!
using FairyGUI;
namespace Ux.UI
{
	[Package("Multiple","Common")]
	[Lazyload("lazyload_multiple")]
	public partial class Multiple2TabView : UITabView
	{
		protected override string PkgName => "Multiple";
		protected override string ResName => "Multiple2TabView";

		protected Btn1 btn1;
		protected Transition t0;
		protected Transition t1;
		protected override void CreateChildren()
		{
			try
			{
				var gCom = ObjAs<GComponent>();
				btn1 = new Btn1(gCom.GetChildAt(2), this);
				t0 = gCom.GetTransitionAt(0);
				t1 = gCom.GetTransitionAt(1);
			}
			catch (System.Exception e)
			{
				 Log.Error(e);
			}
		}
		protected override void OnAddEvent()
		{
			AddClick(btn1,_OnBtn1Click);
		}
		void _OnBtn1Click(EventContext e)
		{
			OnBtn1Click(e);
		}
		partial void OnBtn1Click(EventContext e);
	}
}
