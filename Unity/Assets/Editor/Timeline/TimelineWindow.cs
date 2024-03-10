using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class TimeLineWindow : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;

    [MenuItem("UxGame/ʱ����")]
    public static void ShowExample()
    {
        TimeLineWindow wnd = GetWindow<TimeLineWindow>();
        wnd.titleContent = new GUIContent("ʱ����");
    }

    public void CreateGUI()
    {        
        VisualElement root = rootVisualElement;
        m_VisualTreeAsset.CloneTree(root);
    }
}
