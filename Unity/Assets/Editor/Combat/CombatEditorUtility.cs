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

        internal sealed class StateTimelineDefinition
        {
            public StateTimelineDefinition(
                string propertyName,
                string displayName,
                string layerName,
                string shortName,
                StateLayer layer,
                int stateId)
            {
                PropertyName = propertyName;
                DisplayName = displayName;
                LayerName = layerName;
                ShortName = shortName;
                Layer = layer;
                StateId = stateId;
            }

            public string PropertyName { get; }
            public string DisplayName { get; }
            public string LayerName { get; }
            public string ShortName { get; }
            public StateLayer Layer { get; }
            public int StateId { get; }
        }

        private static readonly StateTimelineDefinition[] StateDefinitions =
        {
            new StateTimelineDefinition(
                "idleTimeline", "Idle", "Locomotion", "Idle",
                StateLayer.Locomotion, (int)LocomotionState.Idle),
            new StateTimelineDefinition(
                "moveTimeline", "Move", "Locomotion", "Move",
                StateLayer.Locomotion, (int)LocomotionState.Move),
            new StateTimelineDefinition(
                "airborneTimeline", "Airborne", "Locomotion", "Airborne",
                StateLayer.Locomotion, (int)LocomotionState.Airborne),
            new StateTimelineDefinition(
                "stunnedTimeline", "Stunned", "Control", "Stunned",
                StateLayer.Control, (int)ControlState.Stunned),
            new StateTimelineDefinition(
                "knockbackTimeline", "Knockback", "Control", "Knockback",
                StateLayer.Control, (int)ControlState.Knockback),
            new StateTimelineDefinition(
                "frozenTimeline", "Frozen", "Control", "Frozen",
                StateLayer.Control, (int)ControlState.Frozen),
            new StateTimelineDefinition(
                "deadTimeline", "Dead", "Life", "Dead",
                StateLayer.Life, (int)LifeState.Dead),
        };

        // 这些定义只描述代码支持的宏观状态和旧资源迁移名称，不再代表 Profile 中的固定字段或固定槽位。
        internal static IReadOnlyList<StateTimelineDefinition> StateTimelines => StateDefinitions;

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
            switch (layer)
            {
                case StateLayer.Locomotion:
                    return new[]
                    {
                        (int)LocomotionState.Idle,
                        (int)LocomotionState.Move,
                        (int)LocomotionState.Airborne,
                    };
                case StateLayer.Control:
                    return new[]
                    {
                        (int)ControlState.Stunned,
                        (int)ControlState.Knockback,
                        (int)ControlState.Frozen,
                    };
                case StateLayer.Life:
                    return new[] { (int)LifeState.Dead };
                case StateLayer.Action:
                    return new[]
                    {
                        (int)ActionState.Free,
                        (int)ActionState.Executing,
                    };
                default:
                    return Array.Empty<int>();
            }
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
            bool addDefaultAnimationTrack = true)
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
            AssetDatabase.CreateAsset(timeline, assetPath);
            EditorUtility.SetDirty(timeline);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(assetPath);
            return AssetDatabase.LoadAssetAtPath<TimelineAsset>(assetPath) ?? timeline;
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

                    if (presentation.Layer == StateLayer.Action)
                    {
                        issues.Add(new CombatValidationIssue(
                            CombatValidationSeverity.Error,
                            $"{label} 使用了 Action 层；技能表现必须放在动态技能列表中。",
                            context));
                    }
                    if (!CombatStateId.IsDefined(presentation.Layer, presentation.StateId))
                    {
                        issues.Add(new CombatValidationIssue(
                            CombatValidationSeverity.Error,
                            $"{label} 的逻辑状态未定义。",
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
            }

            // 先收集完整的技能 ID，再检查取消窗口，避免目标技能排在当前技能后面时误报。
            foreach (var action in actions)
            {
                if (action == null)
                {
                    continue;
                }

                if (action.Timeline == null)
                {
                    issues.Add(new CombatValidationIssue(
                        CombatValidationSeverity.Warning,
                        $"技能 {action.name} 未配置 Timeline，播放技能时不会有表现。",
                        action));
                }
                else
                {
                    ValidateTimelineRate(issues, profile, action.Timeline,
                        $"技能 {action.name}");
                    ValidateTimelineStructure(issues, action.Timeline,
                        $"技能 {action.name}");
                    if (action.Timeline.DurationFrames <= 0)
                    {
                        issues.Add(new CombatValidationIssue(
                            CombatValidationSeverity.Warning,
                            $"技能 {action.name} 的 Timeline 尚未添加表现 Clip。",
                            action.Timeline));
                    }
                }

                var windows = action.CancelWindows;
                if (windows == null)
                {
                    issues.Add(new CombatValidationIssue(
                        CombatValidationSeverity.Error,
                        $"技能 {action.name} 的取消窗口列表为空引用。",
                        action));
                    continue;
                }

                foreach (var window in windows)
                {
                    if (window == null)
                    {
                        issues.Add(new CombatValidationIssue(
                            CombatValidationSeverity.Error,
                            $"技能 {action.name} 包含空的取消窗口。",
                            action));
                        continue;
                    }

                    if (window.TargetActionId <= 0 || !actionIds.Contains(window.TargetActionId))
                    {
                        issues.Add(new CombatValidationIssue(
                            CombatValidationSeverity.Error,
                            $"技能 {action.name} 的取消窗口目标不存在：{window.TargetActionId}。",
                            action));
                    }
                    if (window.StartFrame < 0 || window.EndFrame < window.StartFrame)
                    {
                        issues.Add(new CombatValidationIssue(
                            CombatValidationSeverity.Error,
                            $"技能 {action.name} 存在无效取消窗口区间：[{window.StartFrame}, {window.EndFrame}]。",
                            action));
                    }
                    if (action.DurationFrames > 0 && window.StartFrame >= action.DurationFrames)
                    {
                        issues.Add(new CombatValidationIssue(
                            CombatValidationSeverity.Warning,
                            $"技能 {action.name} 的取消窗口起点超出技能时长：{window.StartFrame}/{action.DurationFrames}。",
                            action));
                    }
                }
            }

            return issues;
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
