namespace Ux
{
    /// <summary>
    /// UI层级枚举，定义了UI界面在屏幕上的显示层级
    /// 数值越小显示越靠后，数值越大显示越靠前
    /// </summary>
    public enum UILayer
    {
        /// <summary>
        /// 根层级，最底层，通常用于放置UI系统的根容器
        /// </summary>
        Root,
        
        /// <summary>
        /// 底部层级，用于显示背景或底层UI
        /// </summary>
        Bottom,
        
        /// <summary>
        /// 普通层级，用于显示大多数普通UI界面
        /// </summary>
        Normal,
        
        /// <summary>
        /// 视图层级，用于显示主要内容和功能界面
        /// </summary>
        View,
        
        /// <summary>
        /// 提示层级，用于显示提示、消息等临时性UI
        /// </summary>
        Tip,
        
        /// <summary>
        /// 顶部层级，最上层，用于显示弹窗、警告等需要优先显示的UI
        /// </summary>
        Top
    }
}