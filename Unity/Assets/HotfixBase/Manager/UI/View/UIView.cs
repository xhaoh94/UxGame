namespace Ux
{
    public abstract class UIView : UIBase
    {
        protected virtual UILayer Layer { get; } = UILayer.View;

        protected override void AddToStage()
        {
            // 创建阶段已保证 GameObject 有效（加载失败会抛异常并回滚）。
            GameObject.transform.SetParent(UIMgr.Ins.GetUILayer(Layer), false);
            GameObject.SetActive(true);
        }
    }
}
