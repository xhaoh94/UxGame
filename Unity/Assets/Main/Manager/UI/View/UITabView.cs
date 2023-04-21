namespace Ux
{
    public abstract class UITabView : UIBase
    {
        public override UIObject Parent => UIMgr.Instance.GetUI<UIBase>(Data.TabData.PID);

        protected override void AddToStage()
        {
            if (GObject == null) return;
            if (string.IsNullOrEmpty(Data.TabData.PID))
            {
                Log.Error("UITab界面没有指定父类");
                return;
            }
            var parent = UIMgr.Instance.GetUI<UIBase>(Data.TabData.PID);
            if (parent == null)
            {
                Log.Error("父类界面未打开就打开了子界面");
                return;
            }
            ParentAs<UIBase>().AddChild(this);
        }

        protected override void OnLayout()
        {
            SetLayout(UILayout.Size);
        }
    }
}