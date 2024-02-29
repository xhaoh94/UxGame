//自动生成的代码，请勿修改!!!
using FairyGUI;
namespace Ux.UI
{
	[Package("Multiple","Common")]
	[Lazyload("lazyload_multiple")]
	public partial class Multiple3TabView : UITabView
	{
		protected override string PkgName => "Multiple";
		protected override string ResName => "Multiple3TabView";
		protected UIButton btn1;
		protected override void CreateChildren()
		{
			try
			{
				var gCom = ObjAs<GComponent>();
				btn1 = new UIButton(gCom.GetChildAt(2), this);
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
