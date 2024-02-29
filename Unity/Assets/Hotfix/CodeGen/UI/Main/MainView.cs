﻿//自动生成的代码，请勿修改!!!
using FairyGUI;
namespace Ux.UI
{
	[Package("Main","Common")]
	public partial class MainView : UIView
	{
		protected override string PkgName => "Main";
		protected override string ResName => "MainView";
		protected UIButton btnMainView;
		protected UIButton btnBack;
		protected UIButton btnStack1;
		protected UIButton btnStack2;
		protected UIButton btnStack3;
		protected UIButton btnStack4;
		protected override void CreateChildren()
		{
			try
			{
				var gCom = ObjAs<GComponent>();
				btnMainView = new UIButton(gCom.GetChildAt(0), this);
				btnBack = new UIButton(gCom.GetChildAt(1), this);
				btnStack1 = new UIButton(gCom.GetChildAt(2), this);
				btnStack2 = new UIButton(gCom.GetChildAt(3), this);
				btnStack3 = new UIButton(gCom.GetChildAt(4), this);
				btnStack4 = new UIButton(gCom.GetChildAt(5), this);
			}
			catch (System.Exception e)
			{
				 Log.Error(e);
			}
		}
		protected override void OnAddEvent()
		{
			AddClick(btnMainView,_OnBtnMainViewClick);
			AddClick(btnBack,_OnBtnBackClick);
			AddClick(btnStack1,_OnBtnStack1Click);
			AddClick(btnStack2,_OnBtnStack2Click);
			AddClick(btnStack3,_OnBtnStack3Click);
			AddClick(btnStack4,_OnBtnStack4Click);
		}
		void _OnBtnMainViewClick(EventContext e)
		{
			OnBtnMainViewClick(e);
		}
		partial void OnBtnMainViewClick(EventContext e);
		void _OnBtnBackClick(EventContext e)
		{
			OnBtnBackClick(e);
		}
		partial void OnBtnBackClick(EventContext e);
		void _OnBtnStack1Click(EventContext e)
		{
			OnBtnStack1Click(e);
		}
		partial void OnBtnStack1Click(EventContext e);
		void _OnBtnStack2Click(EventContext e)
		{
			OnBtnStack2Click(e);
		}
		partial void OnBtnStack2Click(EventContext e);
		void _OnBtnStack3Click(EventContext e)
		{
			OnBtnStack3Click(e);
		}
		partial void OnBtnStack3Click(EventContext e);
		void _OnBtnStack4Click(EventContext e)
		{
			OnBtnStack4Click(e);
		}
		partial void OnBtnStack4Click(EventContext e);
	}
}
