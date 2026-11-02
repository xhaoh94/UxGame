namespace Ux
{
    public abstract class UIWindow : UIBase
    {
        protected override void AddToStage()
        {
            // 创建阶段已保证 GameObject 有效（加载失败会抛异常并回滚）。
            GameObject.transform.SetParent(UIMgr.Ins.GetUILayer(UILayer.Window), false);
            GameObject.SetActive(true);
        }

        protected override void RemoveToStage()
        {
            if (GameObject != null)
                GameObject.SetActive(false);
        }

        protected virtual bool IsModal()
        {
            return true;
        }

        protected void ShowModalWait()
        {
        }

        protected void CloseModalWait()
        {
        }
    }
}
