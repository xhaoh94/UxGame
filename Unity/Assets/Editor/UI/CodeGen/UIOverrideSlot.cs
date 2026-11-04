#if UNITY_EDITOR
namespace Ux.Editor.Build.UI
{
    /// <summary>
    /// 页面基类的固定 override 槽位。生成时只认面板里的显式绑定，没配就不生成对应 override。
    /// </summary>
    public enum UIOverrideSlot
    {
        DialogTitle,
        DialogContent,
        DialogClose,
        DialogBtn1,
        DialogBtn2,
        DialogCheckbox,

        TabList,
        TabContent,
        TabClose,
    }
}
#endif
