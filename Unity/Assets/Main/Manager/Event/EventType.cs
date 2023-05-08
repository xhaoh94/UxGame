namespace Ux.Main
{
    public enum EventType
    {
        Min = 0,
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
        /// 补丁状态更新
        /// </summary>
        PATCH_STATE_CHANGED,
        /// <summary>
        /// 查询更新文件
        /// </summary>
        FOUND_UPDATE_FILES,
        /// <summary>
        /// 更新补丁下载进度
        /// </summary>
        DOWNLOAD_PROGRESS_UPDATE,
        /// <summary>
        /// 资源版本获取失败
        /// </summary>
        STATIC_VERSION_UPDATE_FAILED,
        /// <summary>
        /// 补丁清单获取失败
        /// </summary>
        PATCH_MANIFEST_UPDATE_FAILED,
        /// <summary>
        /// 文件下载失败
        /// </summary>
        WEBFILE_DOWNLOAD_FAILED,
        /// <summary>
        /// UI界面显示
        /// </summary>
        UI_SHOW,
        /// <summary>
        /// UI界面重置
        /// </summary>
        UI_RESUME,
        /// <summary>
        /// UI界面关闭
        /// </summary>
        UI_HIDE,

        Max
    }
}