//自动生成的代码，请勿修改!!!
using FairyGUI;
namespace Ux.UI
{
	[Package("Test","Common")]
	public partial class TestView : UIView
	{
		protected override string PkgName => "Test";
		protected override string ResName => "TestView";
		protected UIButton btnMultiple;
		protected UIButton btnNoneWindow;
		protected UIButton btnNoneView;
		protected UIButton btnSingle;
		protected UIButton btnDouble;
		protected UIButton btnLongClick;
		protected UIButton btnTest;
		protected UIButton btnBack;
		protected CommonUIModel testUIModel;
		protected CommonRTModel testRtModel;
		protected override void CreateChildren()
		{
			try
			{
				var gCom = ObjAs<GComponent>();
				btnMultiple = new UIButton(gCom.GetChildAt(2), this);
				btnNoneWindow = new UIButton(gCom.GetChildAt(3), this);
				btnNoneView = new UIButton(gCom.GetChildAt(4), this);
				btnSingle = new UIButton(gCom.GetChildAt(5), this);
				btnDouble = new UIButton(gCom.GetChildAt(6), this);
				btnLongClick = new UIButton(gCom.GetChildAt(7), this);
				btnTest = new UIButton(gCom.GetChildAt(8), this);
				btnBack = new UIButton(gCom.GetChildAt(9), this);
				testUIModel = new CommonUIModel(gCom.GetChildAt(10), this);
				testRtModel = new CommonRTModel(gCom.GetChildAt(11), this);
			}
			catch (System.Exception e)
			{
				 Log.Error(e);
			}
		}
		protected override void OnAddEvent()
		{
			AddClick(btnMultiple,_OnBtnMultipleClick);
			AddClick(btnNoneWindow,_OnBtnNoneWindowClick);
			AddClick(btnNoneView,_OnBtnNoneViewClick);
			AddClick(btnSingle,_OnBtnSingleClick);
			AddClick(btnTest,_OnBtnTestClick);
			AddClick(btnBack,_OnBtnBackClick);
			AddMultipleClick(btnDouble,_OnBtnDoubleMultipleClick, 2, 0.2f);
			AddLongPress(btnLongClick,-1f, _OnBtnLongClickLongPress, 0.2f, 0, 50);
		}
		void _OnBtnMultipleClick(EventContext e)
		{
			OnBtnMultipleClick(e);
		}
		partial void OnBtnMultipleClick(EventContext e);
		void _OnBtnNoneWindowClick(EventContext e)
		{
			OnBtnNoneWindowClick(e);
		}
		partial void OnBtnNoneWindowClick(EventContext e);
		void _OnBtnNoneViewClick(EventContext e)
		{
			OnBtnNoneViewClick(e);
		}
		partial void OnBtnNoneViewClick(EventContext e);
		void _OnBtnSingleClick(EventContext e)
		{
			OnBtnSingleClick(e);
		}
		partial void OnBtnSingleClick(EventContext e);
		void _OnBtnTestClick(EventContext e)
		{
			OnBtnTestClick(e);
		}
		partial void OnBtnTestClick(EventContext e);
		void _OnBtnBackClick(EventContext e)
		{
			OnBtnBackClick(e);
		}
		partial void OnBtnBackClick(EventContext e);
		void _OnBtnDoubleMultipleClick(EventContext e)
		{
			OnBtnDoubleMultipleClick(e);
		}
		partial void OnBtnDoubleMultipleClick(EventContext e);
		bool _OnBtnLongClickLongPress()
		{
			bool b = false;
			OnBtnLongClickLongPress(ref b);
			return b;
		}
		partial void OnBtnLongClickLongPress(ref bool isBreak);
	}
}
