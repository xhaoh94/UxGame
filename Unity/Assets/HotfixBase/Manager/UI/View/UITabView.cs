
namespace Ux
{
    public abstract class UITabView : UIBase
    {
        // Tab子界面可能在父界面还未提交到_showed时就显示，所以查找父界面必须使用准备好的记录而不是可见字典
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
            // 在Tab开始自己的阶段工作之前，显示会话保证父实例已存在
            parent.AddChild(this);
        }

        protected override void OnLayout()
        {
            SetLayout(UILayout.Size);
        }

    }
}
