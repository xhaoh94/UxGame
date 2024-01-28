//自动生成的代码，请勿修改!!!
using FairyGUI;
namespace Ux.UI
{
	[Package("Common")]
	public partial class CommonDialog : UIMessageBox
	{
		protected override string PkgName => "Common";
		protected override string ResName => "CommonDialog";
		protected override GTextField __txtTitle => txtTitle;
		protected override GTextField __txtContent => txtContent;
		protected override UIButton __btnClose => btnClose;
		protected override UIButton __btn1 => btn1;
		protected override UIButton __btn2 => btn2;
		protected override UIButton __checkbox => checkbox;

		protected GTextField txtTitle;
		protected GTextField txtContent;
		protected Btn1 btn1;
		protected Btn1 btn2;
		protected BtnClose btnClose;
		protected CheckBox checkbox;
		protected override void CreateChildren()
		{
			try
			{
				var gCom = ObjAs<Window>().contentPane;
				txtTitle = (GTextField)gCom.GetChildAt(1);
				txtContent = (GTextField)gCom.GetChildAt(2);
				btn1 = new Btn1(gCom.GetChildAt(4), this);
				btn2 = new Btn1(gCom.GetChildAt(5), this);
				btnClose = new BtnClose(gCom.GetChildAt(8), this);
				checkbox = new CheckBox(gCom.GetChildAt(3), this);
			}
			catch (System.Exception e)
			{
				 Log.Error(e);
			}
		}
	}
}
