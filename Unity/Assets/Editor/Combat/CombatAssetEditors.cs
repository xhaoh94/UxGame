using System;
using System.Collections.Generic;
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
                if (GUILayout.Button("打开技能双源时间轴", GUILayout.Height(26)) &&
                    !CombatActionPresentationLookup.OpenTimeline(action))
                {
                    EditorUtility.DisplayDialog(
                        "未找到所属 Profile",
                        "请先将当前技能加入 CharacterCombatProfile，再打开双源时间轴。",
                        "确定");
                }

                if (GUILayout.Button("在战斗配置中定位") &&
                    !CombatActionPresentationLookup.OpenProfile(action))
                {
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

        [MenuItem("Assets/UxGame/战斗/打开技能双源时间轴", true)]
        private static bool ValidateOpenActionTimeline()
        {
            return Selection.activeObject is CombatActionAsset action &&
                   CombatActionPresentationLookup.CanOpenTimeline(action);
        }

        [MenuItem("Assets/UxGame/战斗/打开技能双源时间轴", false, 101)]
        private static void OpenActionTimeline()
        {
            CombatActionPresentationLookup.OpenTimeline(
                Selection.activeObject as CombatActionAsset);
        }
    }

    internal static class CombatActionPresentationLookup
    {
        internal static bool CanOpenTimeline(CombatActionAsset action)
        {
            return FindProfiles(action).Count > 0;
        }

        internal static bool OpenTimeline(CombatActionAsset action)
        {
            var profiles = FindProfiles(action);
            if (profiles.Count == 0)
            {
                return false;
            }
            if (profiles.Count == 1)
            {
                var profile = profiles[0];
                TimelineWindow.Open(action, FindTimeline(profile, action), profile);
                return true;
            }

            var menu = new GenericMenu();
            foreach (var profile in profiles)
            {
                var capturedProfile = profile;
                var path = AssetDatabase.GetAssetPath(profile);
                menu.AddItem(
                    new GUIContent($"{profile.name}  ({path})"),
                    false,
                    () => TimelineWindow.Open(
                        action,
                        FindTimeline(capturedProfile, action),
                        capturedProfile));
            }
            menu.ShowAsContext();
            return true;
        }

        internal static bool OpenProfile(CombatActionAsset action)
        {
            var profiles = FindProfiles(action);
            if (profiles.Count == 0)
            {
                return false;
            }
            if (profiles.Count == 1)
            {
                CombatEditorWindow.OpenAction(profiles[0], action);
                return true;
            }

            var menu = new GenericMenu();
            foreach (var profile in profiles)
            {
                var capturedProfile = profile;
                var path = AssetDatabase.GetAssetPath(profile);
                menu.AddItem(
                    new GUIContent($"{profile.name}  ({path})"),
                    false,
                    () => CombatEditorWindow.OpenAction(capturedProfile, action));
            }
            menu.ShowAsContext();
            return true;
        }

        private static List<CharacterCombatProfile> FindProfiles(CombatActionAsset action)
        {
            var result = new List<CharacterCombatProfile>();
            if (action == null)
            {
                return result;
            }

            foreach (var guid in AssetDatabase.FindAssets("t:CharacterCombatProfile"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var profile = AssetDatabase.LoadAssetAtPath<CharacterCombatProfile>(path);
                if (ContainsAction(profile, action))
                {
                    result.Add(profile);
                }
            }
            result.Sort((a, b) => string.Compare(
                AssetDatabase.GetAssetPath(a),
                AssetDatabase.GetAssetPath(b),
                StringComparison.Ordinal));
            return result;
        }

        private static bool ContainsAction(
            CharacterCombatProfile profile,
            CombatActionAsset action)
        {
            if (profile?.Actions == null)
            {
                return false;
            }
            foreach (var candidate in profile.Actions)
            {
                if (candidate == action)
                {
                    return true;
                }
            }
            return false;
        }

        private static TimelineAsset FindTimeline(
            CharacterCombatProfile profile,
            CombatActionAsset action)
        {
            if (profile?.ActionPresentations == null)
            {
                return null;
            }
            foreach (var presentation in profile.ActionPresentations)
            {
                if (presentation?.Action == action)
                {
                    return presentation.Timeline;
                }
            }
            return null;
        }
    }
}
