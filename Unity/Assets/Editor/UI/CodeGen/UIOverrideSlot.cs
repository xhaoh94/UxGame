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

        // 窗口槽位：**只能追加在末尾**。SavedConfig 用 JsonUtility 存 EditorPrefs，
        // enum 序列化成整数，插在中间会让已存工程的槽位绑定集体错位。
        WindowBody,
        WindowTitleBar,
        WindowTitle,
        WindowBtnMin,
        WindowBtnMax,
        WindowBtnClose,

        // 窗口框架（*WindowFrame）槽位，同样只能往后追加。
        FrameTitleBar,
        FrameTitle,
        FrameBtnMin,
        FrameBtnMax,
        FrameBtnClose,

        // 窗口拖动范围（只能往后追加）
        WindowDragArea,

        // 窗口框架：置顶按钮
        FrameBtnPin,
    }
}
#endif
