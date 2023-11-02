using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class SkillWindow : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;

    [MenuItem("UxGame/¼¼ÄÜ±à¼­Æ÷", false, 300)]
    public static void ShowExample()
    {
        SkillWindow wnd = GetWindow<SkillWindow>();
        wnd.titleContent = new GUIContent("SkillWindow");
    }

    public void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        // VisualElements objects can contain other VisualElement following a tree hierarchy.
        VisualElement label = new Label("Hello World! From C#");
        root.Add(label);

        // Instantiate UXML
        VisualElement labelFromUXML = m_VisualTreeAsset.Instantiate();
        root.Add(labelFromUXML);
    }
}
