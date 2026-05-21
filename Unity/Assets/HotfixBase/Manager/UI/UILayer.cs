namespace Ux
{
    /// <summary>
    /// UI层级枚举，定义了UI界面在屏幕上的显示层级
    /// 数值越小显示越靠后，数值越大显示越靠前
    /// </summary>
    public enum UILayer
    {
        Root,
        Bottom,
        BlurBackdrop,
        Normal,
        View,
        Tip,
        Top
    }
}