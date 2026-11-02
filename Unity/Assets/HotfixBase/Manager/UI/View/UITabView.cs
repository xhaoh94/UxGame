namespace Ux
{
    public abstract class UITabView : UIBase
    {
        public override UIObject Parent => UIMgr.Ins.GetPreparedUI<UIBase>(Data.TabData.PID);
        public sealed override UIType Type => ParentAs<UIBase>()?.Type ?? UIType.Normal;
        public sealed override UIBlur Blur => UIBlur.None;

        protected override void AddToStage()
        {
            if (Data?.TabData == null || Data.TabData.PID == 0)
            {
                Log.Error("UITab界面没有指定父类");
                return;
            }

            var parent = ParentAs<UIBase>();
            if (parent == null)
            {
                Log.Error("父类界面未打开就打开了子界面");
                return;
            }

            parent.AddChild(this);
        }
    }
}
