using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class TimeLineWindow : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;

    [MenuItem("UxGame/时间轴")]
    public static void ShowExample()
    {
        TimeLineWindow wnd = GetWindow<TimeLineWindow>();
        wnd.titleContent = new GUIContent("时间轴");
    }

    public void CreateGUI()
    {        
        VisualElement root = rootVisualElement;
        m_VisualTreeAsset.CloneTree(root);
    }
}
