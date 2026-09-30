using System;
using UnityEditor;
using UnityEngine;

namespace Ux.Editor.Combat
{
    internal sealed class CombatActionCreatePopup : EditorWindow
    {
        internal delegate bool CreateActionCallback(
            string stableId,
            string displayName,
            string actionAssetName,
            string timelineAssetName,
            out string error);

        private int actionId;
        private string stableId;
        private string displayName;
        private string actionAssetName;
        private string timelineAssetName;
        private CreateActionCallback createAction;

        internal static void Open(
            int actionId,
            string stableId,
            string displayName,
            string actionAssetName,
            string timelineAssetName,
            CreateActionCallback callback)
        {
            var window = CreateInstance<CombatActionCreatePopup>();
            window.titleContent = new GUIContent("创建技能");
            window.actionId = actionId;
            window.stableId = stableId ?? string.Empty;
            window.displayName = displayName ?? string.Empty;
            window.actionAssetName = actionAssetName ?? string.Empty;
            window.timelineAssetName = timelineAssetName ?? string.Empty;
            window.createAction = callback;
            window.minSize = new Vector2(480f, 260f);
            window.maxSize = new Vector2(720f, 320f);
            window.ShowUtility();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField(
                $"创建技能 Action {actionId}",
                EditorStyles.boldLabel);
            EditorGUILayout.Space(6);

            stableId = EditorGUILayout.TextField("StableId", stableId);
            displayName = EditorGUILayout.TextField("技能名称", displayName);
            actionAssetName = EditorGUILayout.TextField("逻辑资产文件名", actionAssetName);
            timelineAssetName = EditorGUILayout.TextField("Timeline 文件名", timelineAssetName);

            EditorGUILayout.HelpBox(
                "文件名不需要填写 .asset 后缀；创建后会自动加入当前 CharacterCombatProfile。",
                MessageType.Info);
            EditorGUILayout.Space(8);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("取消", GUILayout.Width(90)))
                {
                    Close();
                }
                if (GUILayout.Button("创建", GUILayout.Width(90)))
                {
                    Create();
                }
            }
        }

        private void Create()
        {
            stableId = stableId?.Trim() ?? string.Empty;
            displayName = displayName?.Trim() ?? string.Empty;
            actionAssetName = actionAssetName?.Trim() ?? string.Empty;
            timelineAssetName = timelineAssetName?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(stableId) ||
                string.IsNullOrEmpty(displayName) ||
                string.IsNullOrEmpty(actionAssetName) ||
                string.IsNullOrEmpty(timelineAssetName))
            {
                EditorUtility.DisplayDialog(
                    "无法创建技能",
                    "StableId、技能名称、逻辑资产文件名和 Timeline 文件名都不能为空。",
                    "确定");
                return;
            }

            if (createAction == null)
            {
                Close();
                return;
            }

            if (createAction(
                    stableId,
                    displayName,
                    actionAssetName,
                    timelineAssetName,
                    out var error))
            {
                Close();
                return;
            }

            EditorUtility.DisplayDialog(
                "无法创建技能",
                string.IsNullOrEmpty(error) ? "技能创建失败。" : error,
                "确定");
        }
    }
}
