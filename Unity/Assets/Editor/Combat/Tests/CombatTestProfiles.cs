using UnityEditor;
using UnityEngine;

namespace Ux.Editor.Combat.Tests
{
    /// <summary>战斗单元测试共用的资源构造入口。全部走 ScriptableObject，测试结束需自行销毁。</summary>
    internal static class CombatTestProfiles
    {
        public static CharacterCombatProfile CreateProfile(params CombatActionAsset[] actions)
        {
            var profile = ScriptableObject.CreateInstance<CharacterCombatProfile>();
            var serialized = new SerializedObject(profile);
            var list = serialized.FindProperty("actions");
            list.arraySize = actions?.Length ?? 0;
            for (var i = 0; i < list.arraySize; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = actions[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            profile.ValidateData();
            return profile;
        }

        public static CombatActionPresentation AddActionPresentation(
            CharacterCombatProfile profile,
            CombatActionAsset action,
            TimelineAsset timeline)
        {
            var serialized = new SerializedObject(profile);
            var list = serialized.FindProperty("actionPresentations");
            if (list == null)
            {
                throw new System.InvalidOperationException(
                    "CharacterCombatProfile 缺少 actionPresentations 字段。");
            }
            var index = list.arraySize;
            list.InsertArrayElementAtIndex(index);
            var element = list.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("action").objectReferenceValue = action;
            element.FindPropertyRelative("timeline").objectReferenceValue = timeline;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            profile.ValidateData();
            return profile.ActionPresentations[index];
        }

        public static CombatStatePresentation AddStatePresentation(
            CharacterCombatProfile profile,
            StateLayer layer,
            int stateId,
            string variantId,
            TimelineAsset timeline,
            int priority = 0,
            string stableId = null,
            string displayName = null)
        {
            var serialized = new SerializedObject(profile);
            var list = serialized.FindProperty("statePresentations");
            if (list == null)
            {
                throw new System.InvalidOperationException(
                    "CharacterCombatProfile 缺少 statePresentations 字段。");
            }
            var index = list.arraySize;
            list.InsertArrayElementAtIndex(index);
            var element = list.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("stableId").stringValue =
                stableId ?? CombatStatePresentation.BuildStableId(layer, stateId, variantId);
            element.FindPropertyRelative("layer").enumValueIndex = (int)layer;
            element.FindPropertyRelative("stateId").intValue = stateId;
            element.FindPropertyRelative("variantId").stringValue =
                CombatStatePresentation.NormalizeVariantId(variantId);
            element.FindPropertyRelative("displayName").stringValue = displayName ?? string.Empty;
            element.FindPropertyRelative("priority").intValue = priority;
            element.FindPropertyRelative("timeline").objectReferenceValue = timeline;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            profile.ValidateData();
            return profile.StatePresentations[index];
        }

        public static CombatActionAsset CreateAction(
            int actionId,
            string stableId,
            int durationFrames)
        {
            var action = ScriptableObject.CreateInstance<CombatActionAsset>();
            var serialized = new SerializedObject(action);
            serialized.FindProperty("actionId").intValue = actionId;
            serialized.FindProperty("stableId").stringValue = stableId;
            serialized.FindProperty("displayName").stringValue = stableId;
            serialized.FindProperty("durationFrames").intValue = durationFrames;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            action.ValidateData();
            return action;
        }
    }
}
