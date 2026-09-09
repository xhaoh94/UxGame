using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Ux.Editor.Combat
{
    internal enum CombatValidationSeverity
    {
        Info,
        Warning,
        Error,
    }

    internal sealed class CombatValidationIssue
    {
        public CombatValidationIssue(
            CombatValidationSeverity severity,
            string message,
            UnityEngine.Object context = null)
        {
            Severity = severity;
            Message = message ?? string.Empty;
            Context = context;
        }

        public CombatValidationSeverity Severity { get; }
        public string Message { get; }
        public UnityEngine.Object Context { get; }
    }

    /// <summary>
    /// 战斗编辑器共用的资源路径、状态映射、创建和校验逻辑。
    /// UI 只负责编辑 SerializedObject，资源规则集中在这里，避免 Timeline 和 Combat 窗口各自实现一套。
    /// </summary>
    internal static class CombatEditorUtility
    {
        internal const string CombatRoot = "Assets/Data/Res/Combat";
        internal const string TimelineRoot = "Assets/Data/Res/Timeline";

        internal static string GetStateLayerDisplayName(StateLayer layer)
        {
            return layer switch
            {
                StateLayer.Locomotion => "Locomotion",
                StateLayer.Control => "Control",
                StateLayer.Life => "Life",
                StateLayer.Action => "Action（技能由技能列表管理）",
                _ => layer.ToString(),
            };
        }

        internal static int[] GetDefinedStateIds(StateLayer layer)
        {
            // 统一从枚举反射生成（CombatStateId.GetMappableStateIds），避免手写列表与枚举漂移。
            return CombatStateId.GetMappableStateIds(layer);
        }

        internal static string GetStatePresentationTimelineSuffix(
            StateLayer layer,
            int stateId,
            string variantId)
        {
            var stateName = CombatStateId.GetDisplayName(layer, stateId);
            var normalizedVariant = CombatStatePresentation.NormalizeVariantId(variantId);
            var suffix = string.Equals(
                    normalizedVariant,
                    CombatStatePresentation.DefaultVariantId,
                    StringComparison.Ordinal)
                ? stateName
                : $"{stateName}_{normalizedVariant}";
            return $"State_{SanitizePathSegment(layer.ToString())}_{SanitizePathSegment(suffix)}";
        }

        internal static string NormalizeAssetPath(string path)
        {
            return string.IsNullOrEmpty(path) ? string.Empty : path.Replace('\\', '/');
        }

        internal static string GetProfileDirectory(CharacterCombatProfile profile)
        {
            var assetPath = NormalizeAssetPath(AssetDatabase.GetAssetPath(profile));
            if (!string.IsNullOrEmpty(assetPath))
            {
                var directory = NormalizeAssetPath(Path.GetDirectoryName(assetPath));
                if (!string.IsNullOrEmpty(directory))
                {
                    return directory;
                }
            }

            return CombatRoot;
        }

        internal static string GetCharacterKey(CharacterCombatProfile profile)
        {
            if (profile != null && !string.IsNullOrWhiteSpace(profile.Group))
            {
                return SanitizePathSegment(profile.Group.Trim());
            }

            var directory = GetProfileDirectory(profile);
            var slash = directory.LastIndexOf('/');
            if (slash >= 0 && slash < directory.Length - 1)
            {
                var directoryName = directory.Substring(slash + 1);
                if (!string.Equals(directoryName, "Combat", StringComparison.OrdinalIgnoreCase))
                {
                    return SanitizePathSegment(directoryName);
                }
            }

            return SanitizePathSegment(profile == null ? "Character" : profile.name);
        }

        internal static string GetTimelineDirectory(CharacterCombatProfile profile)
        {
            return NormalizeAssetPath($"{TimelineRoot}/{GetCharacterKey(profile)}");
        }

        internal static string GetTimelineAssetName(
            CharacterCombatProfile profile,
            string suffix)
        {
            var profileName = profile == null ? "Character" : profile.name;
            return SanitizeFileName($"{profileName}{suffix}");
        }

        internal static string GetActionAssetName(
            CharacterCombatProfile profile,
            int actionId)
        {
            var profileName = profile == null ? "Character" : profile.name;
            return SanitizeFileName($"{profileName}Action{Mathf.Max(1, actionId):000}");
        }

        internal static string SanitizeFileName(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "NewAsset";
            }

            var invalid = Path.GetInvalidFileNameChars();
            var chars = value.ToCharArray();
            for (var i = 0; i < chars.Length; i++)
            {
                for (var j = 0; j < invalid.Length; j++)
                {
                    if (chars[i] == invalid[j] || chars[i] == '/' || chars[i] == '\\')
                    {
                        chars[i] = '_';
                        break;
                    }
                }
            }

            return new string(chars).Trim();
        }

        internal static string SanitizePathSegment(string value)
        {
            var result = SanitizeFileName(value);
            return string.IsNullOrEmpty(result) ? "Character" : result;
        }

        internal static void EnsureFolder(string assetFolder)
        {
            var normalized = NormalizeAssetPath(assetFolder).TrimEnd('/');
            if (string.IsNullOrEmpty(normalized) ||
                string.Equals(normalized, "Assets", StringComparison.OrdinalIgnoreCase) ||
                AssetDatabase.IsValidFolder(normalized))
            {
                return;
            }

            var slash = normalized.LastIndexOf('/');
            if (slash <= 0)
            {
                return;
            }

            var parent = normalized.Substring(0, slash);
            var child = normalized.Substring(slash + 1);
            EnsureFolder(parent);
            if (!AssetDatabase.IsValidFolder(normalized))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        internal static TimelineAsset CreateTimelineAsset(
            CharacterCombatProfile profile,
            string suffix,
            bool addDefaultAnimationTrack = true,
            AnimationClip primaryAnimation = null)
        {
            var directory = GetTimelineDirectory(profile);
            EnsureFolder(directory);

            var assetName = GetTimelineAssetName(profile, suffix);
            var assetPath = AssetDatabase.GenerateUniqueAssetPath(
                NormalizeAssetPath($"{directory}/{assetName}.asset"));
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.SetFrameRate(profile == null ? TimelineAsset.DefaultFrameRate : profile.FrameRate);
            if (addDefaultAnimationTrack)
            {
                timeline.tracks.Add(new AnimationTrackAsset
                {
                    trackName = "动画",
                });
            }
            timeline.ValidateData();
            if (primaryAnimation != null)
            {
                SetPrimaryAnimationClip(timeline, primaryAnimation, out _);
            }
            AssetDatabase.CreateAsset(timeline, assetPath);
            EditorUtility.SetDirty(timeline);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(assetPath);
            var persisted = AssetDatabase.LoadAssetAtPath<TimelineAsset>(assetPath);
            if (persisted == null)
            {
                AssetDatabase.DeleteAsset(assetPath);
                if (!EditorUtility.IsPersistent(timeline))
                {
                    UnityEngine.Object.DestroyImmediate(timeline);
                }
                return null;
            }

            Undo.RegisterCreatedObjectUndo(persisted, "创建 Timeline 资产");
            return persisted;
        }

        /// <summary>
        /// 以单个 Undo 事务创建逻辑技能、表现 Timeline 和 Profile 映射。
        /// 任一步骤失败都会撤销 Profile 修改并删除本次新建的资产。
        /// </summary>
        internal static bool TryCreateActionAssets(
            CharacterCombatProfile profile,
            int actionId,
            string stableId,
            string displayName,
            int durationFrames,
            ActionMovementPolicy movementPolicy,
            string timelineSuffix,
            AnimationClip primaryAnimation,
            out CombatActionAsset action,
            out TimelineAsset timeline,
            out string error)
        {
            action = null;
            timeline = null;
            error = string.Empty;
            if (profile == null)
            {
                error = "没有选择 CharacterCombatProfile。";
                return false;
            }
            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(profile)))
            {
                error = "请先保存 CharacterCombatProfile。";
                return false;
            }
            if (actionId <= 0 || profile.FindAction(actionId) != null)
            {
                error = $"Profile 中已存在或无法使用 ActionId：{actionId}。";
                return false;
            }

            var profileSerialized = new SerializedObject(profile);
            profileSerialized.Update();
            var actions = profileSerialized.FindProperty("actions");
            var presentations = profileSerialized.FindProperty("actionPresentations");
            if (actions == null || presentations == null)
            {
                error = "CharacterCombatProfile 缺少 actions 或 actionPresentations 字段。";
                return false;
            }

            Undo.IncrementCurrentGroup();
            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName($"创建技能 Action {actionId}");
            string actionPath = null;
            string timelinePath = null;
            try
            {
                action = ScriptableObject.CreateInstance<CombatActionAsset>();
                var actionSerialized = new SerializedObject(action);
                actionSerialized.FindProperty("stableId").stringValue = stableId ?? string.Empty;
                actionSerialized.FindProperty("actionId").intValue = actionId;
                actionSerialized.FindProperty("displayName").stringValue = displayName ?? string.Empty;
                actionSerialized.FindProperty("durationFrames").intValue = durationFrames;
                actionSerialized.FindProperty("movementPolicy").enumValueIndex = (int)movementPolicy;
                actionSerialized.ApplyModifiedPropertiesWithoutUndo();
                action.ValidateData();

                var directory = GetProfileDirectory(profile);
                EnsureFolder(directory);
                actionPath = AssetDatabase.GenerateUniqueAssetPath(NormalizeAssetPath(
                    $"{directory}/{GetActionAssetName(profile, actionId)}.asset"));
                AssetDatabase.CreateAsset(action, actionPath);
                Undo.RegisterCreatedObjectUndo(action, "创建技能逻辑资产");

                timeline = CreateTimelineAsset(
                    profile,
                    timelineSuffix,
                    true,
                    primaryAnimation);
                if (timeline == null)
                {
                    throw new InvalidOperationException("表现 Timeline 创建失败。");
                }
                timelinePath = AssetDatabase.GetAssetPath(timeline);

                actions.InsertArrayElementAtIndex(actions.arraySize);
                actions.GetArrayElementAtIndex(actions.arraySize - 1).objectReferenceValue = action;
                var presentationIndex = presentations.arraySize;
                presentations.InsertArrayElementAtIndex(presentationIndex);
                var presentation = presentations.GetArrayElementAtIndex(presentationIndex);
                presentation.FindPropertyRelative("action").objectReferenceValue = action;
                presentation.FindPropertyRelative("timeline").objectReferenceValue = timeline;
                if (!profileSerialized.ApplyModifiedProperties())
                {
                    throw new InvalidOperationException("Profile 技能列表和表现映射保存失败。");
                }

                profile.ValidateData();
                EditorUtility.SetDirty(profile);
                EditorUtility.SetDirty(action);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(actionPath);
                if (!string.IsNullOrEmpty(timelinePath))
                {
                    AssetDatabase.ImportAsset(timelinePath);
                }

                action = AssetDatabase.LoadAssetAtPath<CombatActionAsset>(actionPath);
                timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(timelinePath);
                if (action == null || timeline == null)
                {
                    throw new InvalidOperationException("新建技能资产重新导入失败。");
                }

                Undo.CollapseUndoOperations(undoGroup);
                return true;
            }
            catch (Exception exception)
            {
                Undo.RevertAllDownToGroup(undoGroup);
                if (!string.IsNullOrEmpty(actionPath))
                {
                    AssetDatabase.DeleteAsset(actionPath);
                }
                if (!string.IsNullOrEmpty(timelinePath))
                {
                    AssetDatabase.DeleteAsset(timelinePath);
                }
                if (action != null && !EditorUtility.IsPersistent(action))
                {
                    UnityEngine.Object.DestroyImmediate(action);
                }
                if (timeline != null && !EditorUtility.IsPersistent(timeline))
                {
                    UnityEngine.Object.DestroyImmediate(timeline);
                }
                AssetDatabase.SaveAssets();
                action = null;
                timeline = null;
                error = exception.Message;
                return false;
            }
        }

        /// <summary>
        /// 仅修改 Profile 持有的客户端表现映射，不写入 CombatActionAsset。
        /// 编辑器中的逻辑资产与表现映射必须分别通过各自 SerializedObject 保存。
        /// </summary>
        internal static bool TrySetActionTimeline(
            CharacterCombatProfile profile,
            CombatActionAsset action,
            TimelineAsset timeline,
            out string error)
        {
            error = string.Empty;
            if (profile == null || action == null)
            {
                error = "Profile 或逻辑技能为空。";
                return false;
            }

            var belongsToProfile = false;
            if (profile.Actions != null)
            {
                foreach (var candidate in profile.Actions)
                {
                    if (candidate == action)
                    {
                        belongsToProfile = true;
                        break;
                    }
                }
            }
            if (!belongsToProfile)
            {
                error = $"技能 {action.name} 不属于 Profile {profile.name}。";
                return false;
            }

            var serialized = new SerializedObject(profile);
            serialized.Update();
            var presentations = serialized.FindProperty("actionPresentations");
            if (presentations == null)
            {
                error = "CharacterCombatProfile 缺少 actionPresentations 字段。";
                return false;
            }

            SerializedProperty selected = null;
            for (var i = 0; i < presentations.arraySize; i++)
            {
                var candidate = presentations.GetArrayElementAtIndex(i);
                if (candidate.FindPropertyRelative("action")?.objectReferenceValue != action)
                {
                    continue;
                }
                if (selected != null)
                {
                    error = $"技能 Action {action.ActionId} 存在重复表现映射，请先修复 Profile。";
                    return false;
                }
                selected = candidate;
            }

            Undo.RecordObject(profile, "设置技能表现 Timeline");
            if (selected == null)
            {
                var index = presentations.arraySize;
                presentations.InsertArrayElementAtIndex(index);
                selected = presentations.GetArrayElementAtIndex(index);
                selected.FindPropertyRelative("action").objectReferenceValue = action;
            }

            var timelineProperty = selected.FindPropertyRelative("timeline");
            if (timelineProperty == null)
            {
                error = "技能表现映射缺少 timeline 字段。";
                return false;
            }
            timelineProperty.objectReferenceValue = timeline;
            if (!serialized.ApplyModifiedProperties())
            {
                return true;
            }

            profile.ValidateData();
            EditorUtility.SetDirty(profile);
            return true;
        }

        /// <summary>
        /// 获取技能 Timeline 首条动画轨道中的首个动画 Clip。
        /// 这是技能编辑器的快捷入口，Timeline 仍然是动画表现的唯一数据源。
        /// </summary>
        internal static AnimationClip GetPrimaryAnimationClip(TimelineAsset timeline)
        {
            if (timeline?.tracks == null)
            {
                return null;
            }

            foreach (var track in timeline.tracks)
            {
                if (!(track is AnimationTrackAsset animationTrack) || animationTrack.clips == null)
                {
                    continue;
                }

                foreach (var clip in animationTrack.clips)
                {
                    if (clip is AnimationClipAsset animationClipAsset)
                    {
                        return animationClipAsset.clip;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 设置技能 Timeline 首条动画轨道中的首个动画 Clip，并返回对应帧数。
        /// 不负责轨道/Clip 的完整编辑，复杂编排继续交给 TimelineWindow。
        /// </summary>
        internal static bool SetPrimaryAnimationClip(
            TimelineAsset timeline,
            AnimationClip clip,
            out int durationFrames)
        {
            durationFrames = 0;
            if (timeline == null)
            {
                return false;
            }

            timeline.tracks ??= new List<TimelineTrackAsset>();
            AnimationTrackAsset animationTrack = null;
            foreach (var track in timeline.tracks)
            {
                if (track is AnimationTrackAsset candidate)
                {
                    animationTrack = candidate;
                    break;
                }
            }

            if (animationTrack == null && clip == null)
            {
                return false;
            }
            if (animationTrack == null)
            {
                animationTrack = new AnimationTrackAsset
                {
                    trackName = "动画",
                };
                timeline.tracks.Insert(0, animationTrack);
            }
            animationTrack.clips ??= new List<TimelineClipAsset>();

            AnimationClipAsset primary = null;
            foreach (var clipAsset in animationTrack.clips)
            {
                if (clipAsset is AnimationClipAsset candidate)
                {
                    primary = candidate;
                    break;
                }
            }

            if (clip == null)
            {
                if (primary == null)
                {
                    return false;
                }

                animationTrack.clips.Remove(primary);
                timeline.ValidateData();
                return true;
            }

            durationFrames = Mathf.Max(1, Mathf.RoundToInt(clip.length * timeline.FrameRate));
            if (primary == null)
            {
                primary = new AnimationClipAsset
                {
                    StartFrame = 0,
                    EndFrame = durationFrames,
                    clip = clip,
                    clipName = clip.name,
                };
                animationTrack.clips.Add(primary);
            }
            else
            {
                primary.clip = clip;
                primary.clipName = clip.name;
                primary.EndFrame = primary.StartFrame + durationFrames;
            }

            primary.ValidateData();
            timeline.ValidateData();
            return true;
        }

        internal static List<CombatValidationIssue> ValidateProfile(
            CharacterCombatProfile profile)
        {
            var issues = new List<CombatValidationIssue>();
            if (profile == null)
            {
                issues.Add(new CombatValidationIssue(
                    CombatValidationSeverity.Error, "没有选择角色战斗配置。"));
                return issues;
            }

            if (profile.FrameRate <= 0)
            {
                issues.Add(new CombatValidationIssue(
                    CombatValidationSeverity.Error,
                    "Profile 帧率必须大于 0。",
                    profile));
            }

            // 状态表现是动态列表：允许同一 layer/stateId 下存在多个 variantId，
            // 不再为每个 Idle/Move/Dead 保留一个固定资源槽位。
            profile.ValidateData();
            var presentations = profile.StatePresentations;
            if (presentations == null || presentations.Count == 0)
            {
                issues.Add(new CombatValidationIssue(
                    CombatValidationSeverity.Info,
                    "尚未配置状态表现；运行时将只播放技能 Timeline（若有）。",
                    profile));
            }
            else
            {
                var presentationStableIds = new HashSet<string>(StringComparer.Ordinal);
                var presentationKeys = new HashSet<string>(StringComparer.Ordinal);
                for (var i = 0; i < presentations.Count; i++)
                {
                    var presentation = presentations[i];
                    if (presentation == null)
                    {
                        issues.Add(new CombatValidationIssue(
                            CombatValidationSeverity.Error,
                            $"状态表现列表第 {i + 1} 项为空引用。",
                            profile));
                        continue;
                    }

                    var stateName = CombatStateId.GetDisplayName(
                        presentation.Layer,
                        presentation.StateId);
                    var label = $"状态表现 {GetStateLayerDisplayName(presentation.Layer)}/{stateName}/{presentation.VariantId}";

                    var timeline = presentation.Timeline;
                    var context = timeline != null ? (UnityEngine.Object)timeline : profile;

                    if (!CombatStateId.IsStatePresentationMappable(
                            presentation.Layer,
                            presentation.StateId))
                    {
                        var reason = presentation.Layer == StateLayer.Action
                            ? "使用了 Action 层；技能表现必须放在动态技能列表中。"
                            : "不是允许配置表现映射的逻辑状态。";
                        issues.Add(new CombatValidationIssue(
                            CombatValidationSeverity.Error,
                            $"{label} {reason}",
                            context));
                    }

                    if (string.IsNullOrEmpty(presentation.StableId))
                    {
                        issues.Add(new CombatValidationIssue(
                            CombatValidationSeverity.Error,
                            $"{label} 缺少 StableId。",
                            context));
                    }
                    else if (!presentationStableIds.Add(presentation.StableId))
                    {
                        issues.Add(new CombatValidationIssue(
                            CombatValidationSeverity.Error,
                            $"状态表现 StableId 重复：{presentation.StableId}。",
                            context));
                    }

                    var key = $"{(int)presentation.Layer}:{presentation.StateId}:{presentation.VariantId}";
                    if (!presentationKeys.Add(key))
                    {
                        issues.Add(new CombatValidationIssue(
                            CombatValidationSeverity.Error,
                            $"同一逻辑状态的 variantId 重复：{label}。",
                            context));
                    }

                    if (timeline == null)
                    {
                        issues.Add(new CombatValidationIssue(
                            CombatValidationSeverity.Warning,
                            $"{label} 未配置 Timeline，进入该状态时不会有表现。",
                            profile));
                        continue;
                    }

                    ValidateTimelineRate(issues, profile, timeline, label);
                    ValidateTimelineStructure(issues, timeline, label);
                    if (timeline.DurationFrames <= 0)
                    {
                        issues.Add(new CombatValidationIssue(
                            CombatValidationSeverity.Warning,
                            $"{label} 的 Timeline 尚未添加表现 Clip。",
                            timeline));
                    }
                }
            }

            var actions = profile.Actions;
            if (actions == null)
            {
                issues.Add(new CombatValidationIssue(
                    CombatValidationSeverity.Error,
                    "技能列表为空引用。",
                    profile));
                return issues;
            }

            var actionIds = new HashSet<int>();
            var stableIds = new HashSet<string>(StringComparer.Ordinal);
            var actionAssets = new HashSet<CombatActionAsset>();
            foreach (var action in actions)
            {
                if (action == null)
                {
                    issues.Add(new CombatValidationIssue(
                        CombatValidationSeverity.Error,
                        "技能列表包含空引用。",
                        profile));
                    continue;
                }

                actionAssets.Add(action);
                if (action.ActionId <= 0)
                {
                    issues.Add(new CombatValidationIssue(
                        CombatValidationSeverity.Error,
                        $"技能 {action.name} 的技能 ID 必须大于 0。",
                        action));
                }
                else if (!actionIds.Add(action.ActionId))
                {
                    issues.Add(new CombatValidationIssue(
                        CombatValidationSeverity.Error,
                        $"技能 ID 重复：{action.ActionId}。",
                        action));
                }

                if (string.IsNullOrEmpty(action.StableId))
                {
                    issues.Add(new CombatValidationIssue(
                        CombatValidationSeverity.Error,
                        $"技能 {action.name} 缺少 StableId。",
                        action));
                }
                else if (!stableIds.Add(action.StableId))
                {
                    issues.Add(new CombatValidationIssue(
                        CombatValidationSeverity.Error,
                        $"技能 StableId 重复：{action.StableId}。",
                        action));
                }

                if (action.DurationFrames <= 0)
                {
                    issues.Add(new CombatValidationIssue(
                        CombatValidationSeverity.Error,
                        $"技能 {action.name} 的逻辑持续帧必须大于 0。",
                        action));
                }
            }

            var mappedActions = new HashSet<CombatActionAsset>();
            var actionPresentations = profile.ActionPresentations;
            if (actionPresentations != null)
            {
                foreach (var presentation in actionPresentations)
                {
                    var action = presentation?.Action;
                    if (action == null)
                    {
                        issues.Add(new CombatValidationIssue(
                            CombatValidationSeverity.Error,
                            "技能表现映射缺少逻辑技能。",
                            profile));
                        continue;
                    }
                    if (!actionAssets.Contains(action))
                    {
                        issues.Add(new CombatValidationIssue(
                            CombatValidationSeverity.Error,
                            $"技能表现映射引用了不属于当前 Profile 的技能：{action.name}。",
                            profile));
                        continue;
                    }
                    if (!mappedActions.Add(action))
                    {
                        issues.Add(new CombatValidationIssue(
                            CombatValidationSeverity.Error,
                            $"技能表现映射重复：Action {action.ActionId}。",
                            profile));
                        continue;
                    }

                    var timeline = presentation.Timeline;
                    if (timeline == null)
                    {
                        issues.Add(new CombatValidationIssue(
                            CombatValidationSeverity.Warning,
                            $"技能 {action.name} 未配置表现 Timeline，播放技能时不会有表现。",
                            action));
                        continue;
                    }

                    var label = $"技能 {action.name}";
                    ValidateTimelineRate(issues, profile, timeline, label);
                    ValidateTimelineStructure(issues, timeline, label);
                    if (timeline.DurationFrames <= 0)
                    {
                        issues.Add(new CombatValidationIssue(
                            CombatValidationSeverity.Warning,
                            $"{label} 的 Timeline 尚未添加表现 Clip。",
                            timeline));
                    }
                    else if (timeline.DurationFrames != action.DurationFrames)
                    {
                        issues.Add(new CombatValidationIssue(
                            CombatValidationSeverity.Warning,
                            $"{label} 的逻辑时长与表现时长不一致：逻辑={action.DurationFrames}，Timeline={timeline.DurationFrames}。",
                            timeline));
                    }
                }
            }

            // 先收集完整的技能 ID，再检查取消窗口，避免目标技能排在当前技能后面时误报。
            foreach (var action in actions)
            {
                if (action == null)
                {
                    continue;
                }

                if (!mappedActions.Contains(action))
                {
                    issues.Add(new CombatValidationIssue(
                        CombatValidationSeverity.Warning,
                        $"技能 {action.name} 尚未建立表现映射，播放技能时不会有表现。",
                        action));
                }

                var logicItemIds = new HashSet<string>(StringComparer.Ordinal);
                var cancelWindows = action.CancelWindows;
                if (cancelWindows == null)
                {
                    issues.Add(new CombatValidationIssue(
                        CombatValidationSeverity.Error,
                        $"技能 {action.name} 的取消窗口列表为空引用。",
                        action));
                }
                else
                {
                    foreach (var window in cancelWindows)
                    {
                        if (window == null)
                        {
                            issues.Add(new CombatValidationIssue(
                                CombatValidationSeverity.Error,
                                $"技能 {action.name} 包含空的取消窗口。",
                                action));
                            continue;
                        }

                        ValidateLogicItemId(issues, action, logicItemIds, window.StableId, "取消窗口");
                        if (window.TargetActionId <= 0 || !actionIds.Contains(window.TargetActionId))
                        {
                            issues.Add(new CombatValidationIssue(
                                CombatValidationSeverity.Error,
                                $"技能 {action.name} 的取消窗口目标不存在：{window.TargetActionId}。",
                                action));
                        }
                        ValidateLogicWindowRange(
                            issues,
                            action,
                            window.StartFrame,
                            window.EndFrame,
                            "取消窗口");
                    }
                }

                var hitWindows = action.HitWindows;
                if (hitWindows == null)
                {
                    issues.Add(new CombatValidationIssue(
                        CombatValidationSeverity.Error,
                        $"技能 {action.name} 的命中窗口列表为空引用。",
                        action));
                }
                else
                {
                    foreach (var window in hitWindows)
                    {
                        if (window == null)
                        {
                            issues.Add(new CombatValidationIssue(
                                CombatValidationSeverity.Error,
                                $"技能 {action.name} 包含空的命中窗口。",
                                action));
                            continue;
                        }

                        ValidateLogicItemId(issues, action, logicItemIds, window.StableId, "命中窗口");
                        ValidateLogicWindowRange(
                            issues,
                            action,
                            window.StartFrame,
                            window.EndFrame,
                            "命中窗口");
                        if (!Enum.IsDefined(typeof(ActionHitShape), window.Shape) ||
                            window.RadiusMillimeters <= 0 ||
                            window.RadiusMillimeters > 10000000)
                        {
                            issues.Add(new CombatValidationIssue(
                                CombatValidationSeverity.Error,
                                $"技能 {action.name} 的命中窗口形状参数无效：shape={window.Shape}, radius={window.RadiusMillimeters}。",
                                action));
                        }
                    }
                }
            }

            return issues;
        }

        private static void ValidateLogicItemId(
            List<CombatValidationIssue> issues,
            CombatActionAsset action,
            HashSet<string> ids,
            string stableId,
            string label)
        {
            if (string.IsNullOrEmpty(stableId))
            {
                issues.Add(new CombatValidationIssue(
                    CombatValidationSeverity.Error,
                    $"技能 {action.name} 的{label}缺少 StableId。",
                    action));
            }
            else if (!ids.Add(stableId))
            {
                issues.Add(new CombatValidationIssue(
                    CombatValidationSeverity.Error,
                    $"技能 {action.name} 的逻辑子项 StableId 重复：{stableId}。",
                    action));
            }
        }

        private static void ValidateLogicWindowRange(
            List<CombatValidationIssue> issues,
            CombatActionAsset action,
            int startFrame,
            int endFrame,
            string label)
        {
            if (startFrame < 0 || endFrame <= startFrame)
            {
                issues.Add(new CombatValidationIssue(
                    CombatValidationSeverity.Error,
                    $"技能 {action.name} 存在无效{label}区间：[{startFrame}, {endFrame})。",
                    action));
            }
            else if (action.DurationFrames > 0 && endFrame > action.DurationFrames)
            {
                issues.Add(new CombatValidationIssue(
                    CombatValidationSeverity.Error,
                    $"技能 {action.name} 的{label}终点超出逻辑时长：{endFrame}/{action.DurationFrames}。",
                    action));
            }
        }

        private static void ValidateTimelineRate(
            List<CombatValidationIssue> issues,
            CharacterCombatProfile profile,
            TimelineAsset timeline,
            string label)
        {
            if (profile == null || timeline == null || timeline.FrameRate == profile.FrameRate)
            {
                return;
            }

            issues.Add(new CombatValidationIssue(
                CombatValidationSeverity.Error,
                $"{label} 的帧率不一致：Profile={profile.FrameRate}，Timeline={timeline.FrameRate}。",
                timeline));
        }

        private static void ValidateTimelineStructure(
            List<CombatValidationIssue> issues,
            TimelineAsset timeline,
            string label)
        {
            if (timeline == null)
            {
                return;
            }

            if (timeline.tracks == null)
            {
                issues.Add(new CombatValidationIssue(
                    CombatValidationSeverity.Error,
                    $"{label} 的轨道列表为空引用。",
                    timeline));
                return;
            }

            for (var trackIndex = 0; trackIndex < timeline.tracks.Count; trackIndex++)
            {
                var track = timeline.tracks[trackIndex];
                if (track == null)
                {
                    issues.Add(new CombatValidationIssue(
                        CombatValidationSeverity.Warning,
                        $"{label} 的第 {trackIndex + 1} 条轨道为空。",
                        timeline));
                    continue;
                }

                if (track.clips == null)
                {
                    issues.Add(new CombatValidationIssue(
                        CombatValidationSeverity.Error,
                        $"{label} 的轨道 {track.trackName} Clip 列表为空引用。",
                        timeline));
                }
            }
        }
    }
}
