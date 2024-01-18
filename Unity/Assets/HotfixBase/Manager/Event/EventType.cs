namespace Ux
{
    public enum MainEventType
    {
        MIN = 0,
        /// <summary>
        /// 网络注销
        /// </summary>
        NET_DISPOSE,
        /// <summary>
        /// 网络连接成功
        /// </summary>
        NET_CONNECTED,
        /// <summary>
        /// 网络错误码
        /// </summary>
        NET_SOCKET_CODE,        
        /// <summary>
        /// UI界面显示
        /// </summary>
        UI_SHOW,
        /// <summary>
        /// UI界面关闭
        /// </summary>
        UI_HIDE,
        /// <summary>
        /// 战争迷雾初始化
        /// </summary>
        FOG_OF_WAR_INIT,
        MAX
    }
}