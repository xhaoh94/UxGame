using UnityEditor;
using UnityEngine;

namespace Ux.Editor
{
    [CustomEditor(typeof(UIController))]
    [CanEditMultipleObjects]
    public sealed class UIControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var controller = (UIController)target;

            serializedObject.FindProperty("_pages").isExpanded = true;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_pages"),
                new GUIContent("页面"), true);
            serializedObject.ApplyModifiedProperties();

            var pages = controller.Pages;
            if (pages == null || pages.Length == 0)
            {
                EditorGUILayout.HelpBox("至少需要一个页面。", MessageType.Info);
                if (GUILayout.Button("添加页面"))
                {
                    Undo.RecordObject(controller, "添加控制器页面");
                    controller.AddPage();
                    EditorUtility.SetDirty(controller);
                    serializedObject.Update();
                }
                return;
            }

            var names = new string[pages.Length];
            for (int i = 0; i < pages.Length; i++)
                names[i] = $"[{i}] {pages[i].Name} (Id {pages[i].Id})";

            DrawPageSelector(controller, names);
            DrawSelectedPageNote(controller);

            DrawPageWarnings(pages);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("添加页面"))
                {
                    Undo.RecordObject(controller, "添加控制器页面");
                    controller.AddPage();
                    EditorUtility.SetDirty(controller);
                    serializedObject.Update();
                }
                using (new EditorGUI.DisabledScope(controller.PageCount <= 1))
                {
                    if (GUILayout.Button("删除末页"))
                    {
                        Undo.RecordObject(controller, "删除控制器页面");
                        controller.RemovePage(controller.PageCount - 1);
                        EditorUtility.SetDirty(controller);
                        serializedObject.Update();
                    }
                }
                if (GUILayout.Button("重新应用"))
                {
                    controller.ApplyAll();
                    EditorUtility.SetDirty(controller);
                }
            }

            EditorGUILayout.LabelField("已绑定对象数", controller.BindingCount.ToString());
            EditorGUILayout.HelpBox(
                "切换当前页会立即修改已绑定对象的场景状态，用于编辑器预览；运行时通过 " +
                "SetSelectedIndex / SetSelectedPage 切换。",
                MessageType.None);
        }

        /// <summary>
        /// 用页签式 Toggle 选择当前页，避免 Popup 不直观且需要反复打开下拉菜单。
        /// 同一时间只保留一个页面为选中状态。
        /// </summary>
        private static void DrawPageSelector(UIController controller, string[] names)
        {
            EditorGUILayout.LabelField("当前页", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                var pages = controller.Pages;
                for (int i = 0; i < names.Length; i++)
                {
                    var selected = controller.SelectedIndex == i;
                    var note = pages[i].EditorNote;
                    var displayName = string.IsNullOrWhiteSpace(note)
                        ? names[i]
                        : $"{names[i]} · {TrimNote(note)}";
                    var content = new GUIContent(displayName, note);
                    var nextSelected = GUILayout.Toggle(selected, content,
                        EditorStyles.toolbarButton);
                    if (!selected && nextSelected)
                    {
                        Undo.RecordObject(controller, "切换控制器页面");
                        controller.SetSelectedIndex(i, true);
                        EditorUtility.SetDirty(controller);
                    }
                }
            }

            EditorGUILayout.LabelField(
                $"当前：[{controller.SelectedIndex}] {controller.SelectedPageName}",
                EditorStyles.miniLabel);
        }

        private static string TrimNote(string note)
        {
            const int maxLength = 12;
            note = note.Replace("\r", " ").Replace("\n", " ").Trim();
            return note.Length <= maxLength
                ? note
                : note.Substring(0, maxLength) + "…";
        }

        private void DrawSelectedPageNote(UIController controller)
        {
            var pages = serializedObject.FindProperty("_pages");
            if (pages == null || controller.SelectedIndex < 0 ||
                controller.SelectedIndex >= pages.arraySize)
                return;

            var page = pages.GetArrayElementAtIndex(controller.SelectedIndex);
            var note = page.FindPropertyRelative("EditorNote");
            if (note == null) return;

            EditorGUILayout.PropertyField(note, new GUIContent("编辑器备注"));
            EditorGUILayout.HelpBox(
                "备注只用于编辑器中识别页面，不会参与运行时逻辑。",
                MessageType.None);
        }

        private static void DrawPageWarnings(ControllerPage[] pages)
        {
            for (int i = 0; i < pages.Length; i++)
            {
                if (string.IsNullOrEmpty(pages[i].Name))
                {
                    EditorGUILayout.HelpBox($"第 {i} 页没有名称，建议填写便于代码调用。",
                        MessageType.Warning);
                }

                for (int j = 0; j < i; j++)
                {
                    if (!string.IsNullOrEmpty(pages[i].Name) &&
                        pages[i].Name == pages[j].Name)
                    {
                        EditorGUILayout.HelpBox(
                            $"第 {i} 页与第 {j} 页名称重复：{pages[i].Name}。",
                            MessageType.Warning);
                    }

                    if (pages[i].Id == pages[j].Id)
                    {
                        EditorGUILayout.HelpBox(
                            $"第 {i} 页与第 {j} 页 Id 重复：{pages[i].Id}，按 Id 切页时会产生歧义。",
                            MessageType.Warning);
                    }
                }
            }
        }
    }
}
