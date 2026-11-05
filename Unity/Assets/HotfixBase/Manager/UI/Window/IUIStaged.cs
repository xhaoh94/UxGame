namespace Ux
{
    /// <summary>
    /// 参与「窗口层（<see cref="UILayer.Window"/>）内层叠分组」的界面。    
    /// </summary>
    internal interface IUIStaged
    {
        /// <summary>当前层叠级别（运行时改写优先，否则用声明值）。</summary>
        UIWindowTier WindowTier { get; }

        /// <summary>运行时改写级别；由 UIStageHandler.SetWindowTier 调用。</summary>
        void SetWindowTier(UIWindowTier tier);
    }
}
