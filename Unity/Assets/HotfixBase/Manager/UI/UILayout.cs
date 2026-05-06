namespace Ux
{
    /// <summary>
    /// UI布局枚举，定义了UI元素在父容器中的对齐方式
    /// 使用九宫格布局，支持左上、中上、右上等多种对齐方式
    /// </summary>
    public enum UILayout
    {
        /// <summary>
        /// 左上角对齐
        /// </summary>
        Left_Top,
        
        /// <summary>
        /// 左中垂直对齐
        /// </summary>
        Left_Middle,
        
        /// <summary>
        /// 左下角对齐
        /// </summary>
        Left_Bottom,
        
        /// <summary>
        /// 中上水平对齐
        /// </summary>
        Center_Top,
        
        /// <summary>
        /// 中心对齐（水平和垂直都居中）
        /// </summary>
        Center_Middle,
        
        /// <summary>
        /// 中下水平对齐
        /// </summary>
        Center_Bottom,
        
        /// <summary>
        /// 右上角对齐
        /// </summary>
        Right_Top,
        
        /// <summary>
        /// 右中垂直对齐
        /// </summary>
        Right_Middle,
        
        /// <summary>
        /// 右下角对齐
        /// </summary>
        Right_Bottom,
        
        /// <summary>
        /// 自适应大小，通常用于填充整个父容器
        /// </summary>
        Size
    }
}