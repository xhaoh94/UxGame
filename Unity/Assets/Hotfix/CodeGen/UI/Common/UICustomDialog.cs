//自动生成的代码，请勿修改!!!
using FairyGUI;
namespace Ux.UI
{
	[Package("Common")]
	public partial class UICustomDialog : UIDialog
	{
		protected override string PkgName => "Common";
		protected override string ResName => "UICustomDialog";
		protected override GTextField __txtTitle => txtTitle;
		protected override GTextField __txtContent => txtContent;
		protected override UIButton __btn1 => btn1;
		protected override UIButton __btn2 => btn2;
		protected override UIButton __btnClose => btnClose;

		protected GTextField txtTitle;
		protected GTextField txtContent;
		protected Btn1 btn1;
		protected Btn1 btn2;
		protected BtnClose btnClose;
		protected override void CreateChildren()
		{
			var gCom = ObjAs<GComponent>();
			txtTitle = (GTextField)gCom.GetChildAt(1);
			txtContent = (GTextField)gCom.GetChildAt(2);
			btn1 = new Btn1(gCom.GetChildAt(3), this);
			btn2 = new Btn1(gCom.GetChildAt(4), this);
			btnClose = new BtnClose(gCom.GetChildAt(5), this);
		}
	}
}
