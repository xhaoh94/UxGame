namespace Ux
{
    public abstract class UITabView : UIBase
    {
        public override UIObject Parent => UIMgr.Ins.GetUI<UIBase>(Data.TabData.PID);

        /// <summary>
        /// 指定这个无效，类型由最上层的父类决定
        /// </summary>
        public override UIType Type => UIType.None;

        protected override void AddToStage()
        {
            if (GObject == null) return;
            if (Data.TabData.PID == 0)
            {
                Log.Error("UITab界面没有指定父类");
                return;
            }
            var parent = UIMgr.Ins.GetUI<UIBase>(Data.TabData.PID);
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