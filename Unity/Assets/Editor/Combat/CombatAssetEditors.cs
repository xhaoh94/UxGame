using UnityEditor;
using UnityEngine;
using Ux.Editor.Timeline;

namespace Ux.Editor.Combat
{
    [CustomEditor(typeof(CharacterCombatProfile))]
    internal sealed class CharacterCombatProfileEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var profile = target as CharacterCombatProfile;
            if (profile != null && GUILayout.Button("打开角色技能表现", GUILayout.Height(26)))
            {
                CombatEditorWindow.Open(profile);
            }

            EditorGUILayout.Space(4);
            DrawDefaultInspector();
        }
    }

    [CustomEditor(typeof(CombatActionAsset))]
    internal sealed class CombatActionAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var action = target as CombatActionAsset;
            if (action != null)
            {
                using (new EditorGUI.DisabledScope(action.Timeline == null))
                {
                    if (GUILayout.Button("打开技能 Timeline", GUILayout.Height(26)))
                    {
                        TimelineWindow.Open(action.Timeline);
                    }
                }

                if (GUILayout.Button("在战斗配置中定位"))
                {
                    var profiles = AssetDatabase.FindAssets("t:CharacterCombatProfile");
                    foreach (var guid in profiles)
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guid);
                        var profile = AssetDatabase.LoadAssetAtPath<CharacterCombatProfile>(path);
                        if (profile?.Actions == null)
                        {
                            continue;
                        }

                        foreach (var candidate in profile.Actions)
                        {
                            if (candidate == action)
                            {
                                CombatEditorWindow.OpenAction(profile, action);
                                return;
                            }
                        }
                    }

                    EditorUtility.DisplayDialog(
                        "未找到所属 Profile",
                        "当前技能没有被任何 CharacterCombatProfile 引用。",
                        "确定");
                }
            }

            EditorGUILayout.Space(4);
            DrawDefaultInspector();
        }
    }

    internal static class CombatAssetMenuItems
    {
        [MenuItem("Assets/UxGame/战斗/打开角色配置", true)]
        private static bool ValidateOpenProfile()
        {
            return Selection.activeObject is CharacterCombatProfile;
        }

        [MenuItem("Assets/UxGame/战斗/打开角色配置", false, 100)]
        private static void OpenProfile()
        {
            CombatEditorWindow.Open(Selection.activeObject as CharacterCombatProfile);
        }

        [MenuItem("Assets/UxGame/战斗/打开技能 Timeline", true)]
        private static bool ValidateOpenActionTimeline()
        {
            return Selection.activeObject is CombatActionAsset action && action.Timeline != null;
        }

        [MenuItem("Assets/UxGame/战斗/打开技能 Timeline", false, 101)]
        private static void OpenActionTimeline()
        {
            var action = Selection.activeObject as CombatActionAsset;
            if (action != null && action.Timeline != null)
            {
                TimelineWindow.Open(action.Timeline);
            }
        }
    }
}
