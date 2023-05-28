//自动生成的代码，请勿修改!!!
using FairyGUI;
namespace Ux.UI
{
	[Package("Main","Common")]
	public partial class MainView : UIView
	{
		protected override string PkgName => "Main";
		protected override string ResName => "MainView";

		protected Btn1 btnMultiple;
		protected Btn1 btnNoneWindow;
		protected Btn1 btnNoneView;
		protected Btn1 btnSingle;
		protected Btn1 btnDouble;
		protected Btn1 btnLongClick;
		protected Btn1 btnTest;
		protected Btn1 btnBack;
		protected override void CreateChildren()
		{
			var gCom = ObjAs<GComponent>();
			btnMultiple = new Btn1(gCom.GetChildAt(2), this);
			btnNoneWindow = new Btn1(gCom.GetChildAt(3), this);
			btnNoneView = new Btn1(gCom.GetChildAt(4), this);
			btnSingle = new Btn1(gCom.GetChildAt(5), this);
			btnDouble = new Btn1(gCom.GetChildAt(6), this);
			btnLongClick = new Btn1(gCom.GetChildAt(7), this);
			btnTest = new Btn1(gCom.GetChildAt(8), this);
			btnBack = new Btn1(gCom.GetChildAt(9), this);
			OnShowCallBack += OnAddEvent;
		}
		protected void OnAddEvent()
		{
			btnMultiple.AddClick(e => OnBtnMultipleClick(e));
			btnNoneWindow.AddClick(e => OnBtnNoneWindowClick(e));
			btnNoneView.AddClick(e => OnBtnNoneViewClick(e));
			btnSingle.AddClick(e => OnBtnSingleClick(e));
			btnTest.AddClick(e => OnBtnTestClick(e));
			btnBack.AddClick(e => OnBtnBackClick(e));
			btnDouble.AddDoubleClick(e => OnBtnDoubleDoubleClick(e), 2, 0.2f);
			btnLongClick.AddLongClick(-1f, () =>
			{
				bool b = false;
				OnBtnLongClickLongClick(ref b);
				return b;
			}, 0.2f, 0, 50);
		}
		partial void OnBtnMultipleClick(EventContext e);
		partial void OnBtnNoneWindowClick(EventContext e);
		partial void OnBtnNoneViewClick(EventContext e);
		partial void OnBtnSingleClick(EventContext e);
		partial void OnBtnTestClick(EventContext e);
		partial void OnBtnBackClick(EventContext e);
		partial void OnBtnDoubleDoubleClick(EventContext e);
		partial void OnBtnLongClickLongClick(ref bool isBreak);
	}
}
