using UnityEditor;
using UnityEngine;

namespace Ux.Editor
{
    [CustomEditor(typeof(UIControllerBinding))]
    [CanEditMultipleObjects]
    public sealed class UIControllerBindingEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var binding = (UIControllerBinding)target;

            EditorGUILayout.HelpBox(
                "上方“页面通用属性”控制所有页面的属性集合；启用后每个页面都会有该属性，" +
                "各页面可以填写不同的值。未启用的属性回落到“默认状态”。",
                MessageType.None);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_controller"),
                new GUIContent("控制器"));
            // 先提交控制器引用，下面的页面列表和状态同步才能使用最新对象。
            serializedObject.ApplyModifiedProperties();

            var controller = binding.Controller;
            if (controller != null && controller.PageCount > 0)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("当前页", GUILayout.Width(52f));
                    EditorGUILayout.LabelField(
                        $"[{controller.SelectedIndex}] {controller.SelectedPageName}",
                        EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField($"共 {controller.PageCount} 页",
                        EditorStyles.miniLabel, GUILayout.Width(56f));
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("记录当前状态为默认"))
                {
                    Undo.RecordObject(binding, "记录控制器默认状态");
                    binding.RecaptureDefault();
                    EditorUtility.SetDirty(binding);
                }
                using (new EditorGUI.DisabledScope(binding.Controller == null))
                {
                    if (GUILayout.Button("重新应用当前页"))
                    {
                        binding.ApplyPage(binding.Controller != null
                            ? binding.Controller.SelectedIndex
                            : 0);
                        EditorUtility.SetDirty(binding);
                    }
                }
            }

            controller = binding.Controller;
            if (controller == null || controller.PageCount == 0)
            {
                EditorGUILayout.HelpBox("先指定一个带页面的控制器。", MessageType.Warning);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            SyncStates();

            var states = serializedObject.FindProperty("_states");
            NormalizeCommonFields(binding, states);
            DrawCommonFields(binding, states);

            var pages = controller.Pages;
            for (int i = 0; i < states.arraySize; i++)
            {
                var state = states.GetArrayElementAtIndex(i);
                var pageIndex = state.FindPropertyRelative("PageIndex").intValue;
                if (pageIndex < 0 || pageIndex >= pages.Length) continue;
                DrawState(state, binding, pageIndex, pages[pageIndex].Name);
            }

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>让状态数组与控制器页面一一对应：多余的删掉，缺的补一条空配置。</summary>
        private void SyncStates()
        {
            var controller = (UIControllerBinding)target;
            var pages = controller.Controller != null ? controller.Controller.Pages : null;
            if (pages == null) return;

            var states = serializedObject.FindProperty("_states");
            for (int i = states.arraySize - 1; i >= 0; i--)
            {
                var pageIndex = states.GetArrayElementAtIndex(i)
                    .FindPropertyRelative("PageIndex").intValue;
                if (pageIndex < 0 || pageIndex >= pages.Length)
                    states.DeleteArrayElementAtIndex(i);
            }

            for (int page = 0; page < pages.Length; page++)
            {
                var found = false;
                for (int i = 0; i < states.arraySize; i++)
                {
                    if (states.GetArrayElementAtIndex(i).FindPropertyRelative("PageIndex").intValue != page)
                        continue;
                    found = true;
                    break;
                }
                if (found) continue;

                states.InsertArrayElementAtIndex(states.arraySize);
                var entry = states.GetArrayElementAtIndex(states.arraySize - 1);
                entry.FindPropertyRelative("PageIndex").intValue = page;
                entry.FindPropertyRelative("Alpha").floatValue = 1f;
                entry.FindPropertyRelative("Color").colorValue = Color.white;
                entry.FindPropertyRelative("Scale").vector3Value = Vector3.one;
                entry.FindPropertyRelative("Text").stringValue = string.Empty;
            }
        }

        private static readonly (string SetName, string ValueName, string Label)[] Fields =
        {
            ("SetVisible", "Visible", "可见"),
            ("SetPosition", "Position", "位置"),
            ("SetSize", "Size", "尺寸"),
            ("SetColor", "Color", "颜色"),
            ("SetText", "Text", "文本"),
            ("SetAlpha", "Alpha", "透明度"),
            ("SetRotation", "Rotation", "旋转"),
            ("SetScale", "Scale", "缩放"),
        };

        /// <summary>
        /// 每个页面只编辑属性值；属性是否启用由上方的“页面通用属性”统一控制。
        /// 这样所有页面的属性集合一致，但每个页面仍可以拥有不同的属性值。
        /// </summary>
        private static void DrawState(SerializedProperty state, UIControllerBinding binding,
            int pageIndex, string pageName)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            using (new EditorGUILayout.HorizontalScope())
            {
                state.isExpanded = EditorGUILayout.Foldout(state.isExpanded,
                    $"[{pageIndex}] {pageName}", true);
                if (binding.Controller != null &&
                    binding.Controller.SelectedIndex == pageIndex)
                {
                    GUILayout.Label("当前页", EditorStyles.miniButton, GUILayout.Width(48f));
                }
            }

            if (!state.isExpanded)
            {
                EditorGUILayout.EndVertical();
                return;
            }

            var hasField = false;
            for (int i = 0; i < Fields.Length; i++)
            {
                var setProperty = state.FindPropertyRelative(Fields[i].SetName);
                if (!setProperty.boolValue) continue;
                hasField = true;
                DrawFieldRow(state, Fields[i]);
            }

            if (!hasField)
            {
                EditorGUILayout.HelpBox(
                    "该页当前没有覆盖属性，将使用对象的默认状态。请在上方“页面通用属性”中启用属性。",
                    MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private static void DrawFieldRow(SerializedProperty state,
            (string SetName, string ValueName, string Label) field)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(field.Label, GUILayout.Width(52f));
                EditorGUILayout.PropertyField(state.FindPropertyRelative(field.ValueName),
                    GUIContent.none);
            }
        }

        /// <summary>按节点实际具备的组件过滤候选属性，例如没有文本组件就不会出现“文本”。</summary>
        private static bool IsSupported(UIControllerBinding binding, int fieldIndex)
        {
            switch (Fields[fieldIndex].SetName)
            {
                case "SetPosition":
                case "SetSize":
                case "SetRotation":
                case "SetScale":
                    return binding.HasRectTransform;
                case "SetColor":
                    return binding.HasGraphic;
                case "SetText":
                    return binding.HasText;
                case "SetAlpha":
                    return binding.HasAlphaControl;
                default:
                    return true;
            }
        }

        private void DrawCommonFields(UIControllerBinding binding,
            SerializedProperty states)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("页面通用属性", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "这里控制所有页面共用的属性集合。启用后每个页面都会有该属性，" +
                "但每个页面可以分别填写不同的值；新启用的属性默认读取当前实际值。",
                MessageType.None);

            using (new EditorGUILayout.HorizontalScope())
            {
                for (int i = 0; i < Fields.Length; i++)
                {
                    if (!IsSupported(binding, i)) continue;
                    var field = Fields[i];
                    var allEnabled = IsFieldEnabled(states, field.SetName, out _);
                    if (!allEnabled) continue;

                    if (GUILayout.Button($"{field.Label} ×", EditorStyles.miniButton))
                    {
                        Undo.RecordObject(binding, "移除控制器通用属性");
                        SetFieldForAllPages(states, binding, field, false);
                        EditorUtility.SetDirty(binding);
                    }
                }

                GUILayout.FlexibleSpace();
                if (GUILayout.Button("添加属性", EditorStyles.toolbarButton, GUILayout.Width(76f)))
                    ShowAddPropertyMenu(binding);
            }
        }

        private void ShowAddPropertyMenu(UIControllerBinding binding)
        {
            var states = serializedObject.FindProperty("_states");
            var menu = new GenericMenu();
            var hasMissing = false;

            for (int i = 0; i < Fields.Length; i++)
            {
                if (!IsSupported(binding, i)) continue;
                var field = Fields[i];
                if (IsFieldEnabled(states, field.SetName, out _)) continue;

                hasMissing = true;
                var menuField = field;
                menu.AddItem(new GUIContent(menuField.Label), false,
                    () => AddCommonProperty(binding, menuField));
            }

            if (!hasMissing)
                menu.AddDisabledItem(new GUIContent("所有支持的属性都已添加"));

            menu.ShowAsContext();
        }

        private void AddCommonProperty(UIControllerBinding binding,
            (string SetName, string ValueName, string Label) field)
        {
            serializedObject.Update();
            var states = serializedObject.FindProperty("_states");
            if (IsFieldEnabled(states, field.SetName, out var mixed) && !mixed)
                return;

            Undo.RecordObject(binding, "添加控制器通用属性");
            SetFieldForAllPages(states, binding, field, true);
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(binding);
            Repaint();
        }

        private static void NormalizeCommonFields(UIControllerBinding binding,
            SerializedProperty states)
        {
            for (int i = 0; i < Fields.Length; i++)
            {
                if (!IsSupported(binding, i)) continue;
                var field = Fields[i];
                IsFieldEnabled(states, field.SetName, out var mixed);
                if (!mixed) continue;

                Undo.RecordObject(binding, "统一控制器页面属性");
                SetFieldForAllPages(states, binding, field, true);
                EditorUtility.SetDirty(binding);
            }
        }

        private static bool IsFieldEnabled(SerializedProperty states, string setName,
            out bool mixed)
        {
            var any = false;
            var all = states.arraySize > 0;
            for (int i = 0; i < states.arraySize; i++)
            {
                var enabled = states.GetArrayElementAtIndex(i)
                    .FindPropertyRelative(setName).boolValue;
                any |= enabled;
                all &= enabled;
            }

            mixed = any && !all;
            return all;
        }

        private static void SetFieldForAllPages(SerializedProperty states,
            UIControllerBinding binding,
            (string SetName, string ValueName, string Label) field, bool enabled)
        {
            var current = enabled ? binding.CaptureCurrentState() : default;
            for (int i = 0; i < states.arraySize; i++)
            {
                var state = states.GetArrayElementAtIndex(i);
                var setProperty = state.FindPropertyRelative(field.SetName);
                var wasEnabled = setProperty.boolValue;
                setProperty.boolValue = enabled;

                // 已经存在的页面值保留；只有新补上的页面才从当前实际状态初始化。
                if (enabled && !wasEnabled)
                    SetCurrentValue(state, field, current);
            }
        }

        private static void SetCurrentValue(SerializedProperty state,
            (string SetName, string ValueName, string Label) field, ControllerState current)
        {
            var value = state.FindPropertyRelative(field.ValueName);
            switch (field.SetName)
            {
                case "SetVisible":
                    value.boolValue = current.Visible;
                    break;
                case "SetPosition":
                    value.vector2Value = current.Position;
                    break;
                case "SetSize":
                    value.vector2Value = current.Size;
                    break;
                case "SetColor":
                    value.colorValue = current.Color;
                    break;
                case "SetText":
                    value.stringValue = current.Text ?? string.Empty;
                    break;
                case "SetAlpha":
                    value.floatValue = current.Alpha;
                    break;
                case "SetRotation":
                    value.floatValue = current.Rotation;
                    break;
                case "SetScale":
                    value.vector3Value = current.Scale;
                    break;
            }
        }
    }
}
