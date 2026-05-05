using System.Threading;

namespace Ux
{
    public abstract class UITabView : UIBase
    {
        // Tab children can be shown before the parent has been committed into _showed,
        // so parent lookup must use the prepared record instead of the visible dictionary.
        public override UIObject Parent => UIMgr.Ins.GetPreparedUI<UIBase>(Data.TabData.PID);
        /// <summary>
        /// 不可重写，以父类类型为准
        /// </summary>
        public sealed override UIType Type => ParentAs<UIBase>()?.Type ?? UIType.Normal;
        /// <summary>
        /// 不可重写，模糊层只判断最顶层的父界面
        /// </summary>
        public sealed override UIBlur Blur => UIBlur.None;

        protected override void AddToStage()
        {
            if (GObject == null) return;
            if (Data.TabData.PID == 0)
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
            // Parent instance is guaranteed by the show session before the tab starts its own stage work.
            parent.AddChild(this);
        }

        protected override void OnLayout()
        {
            SetLayout(UILayout.Size);
        }

    }
}
