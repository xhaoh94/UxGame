using Assets.Editor.Timeline;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Ux.Editor.Timeline.Animation;

namespace Ux.Editor.Timeline
{
    public partial class TimelineWindow
    {
        public static UxUndo Undo { get; private set; }
        public static TimelineInspectorView InspectorContent { get; set; }
        public static VisualElement ClipContent { get; set; }
        public static System.Func<int, float> GetPositionByFrame { get; set; }
        public static System.Func<int> GetFrameByMousePosition { get; set; }
        public static System.Action<int> MarkerMove { get; set; }
        public static TimelineComponent Timeline { get; set; }
        public static TimelineAsset Asset { get; set; }
        public static System.Action SaveAssets { get; set; }
        public static System.Action<TimelineTrackAsset, UnityEngine.Object> RefreshBinds { get; set; }
        public static System.Action RefreshEntity { get; set; }
        public static System.Action RefreshView { get; set; }
        public static System.Action RefreshClip { get; set; }
        public static bool IsPlaying { get; set; }
        public static int FrameRate => Asset?.FrameRate ?? TimelineAsset.DefaultFrameRate;
        public static bool IsValid()
        {
            return Asset != null && Timeline != null && !IsPlaying;
        }
        public static float FrameToTime(int frame)
        {
            return frame / (float)FrameRate;
        }
        public static int GetDurationFrame(TimelineAsset asset)
        {
            return asset?.DurationFrames ?? 0;
        }
        public static int GetDurationFrame(TimelineTrackAsset asset)
        {
            return asset?.GetEndFrame() ?? 0;
        }
        public static float GetDuration(TimelineAsset asset)
        {
            return asset == null ? 0 : asset.FrameToTime(asset.DurationFrames);
        }
        public static float GetDuration(TimelineTrackAsset asset)
        {
            return GetDurationFrame(asset) / (float)FrameRate;
        }

        public static TimelineClipAsset CreateClipAsset(Type clipType, int start, string retPath)
        {
            if (clipType == null || !typeof(TimelineClipAsset).IsAssignableFrom(clipType))
            {
                return null;
            }

            var clipAsset = Activator.CreateInstance(clipType) as TimelineClipAsset;
            if (clipAsset == null)
            {
                return null;
            }

            clipAsset.StartFrame = Mathf.Max(0, start);
            clipAsset.EndFrame = clipAsset.StartFrame + FrameRate;
            clipAsset.clipName = clipType.Name;

            if (clipAsset is AnimationClipAsset animationClipAsset && !string.IsNullOrEmpty(retPath))
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(retPath);
                if (clip != null)
                {
                    animationClipAsset.clip = clip;
                    animationClipAsset.EndFrame = start + Mathf.Max(1, Mathf.RoundToInt(clip.length * FrameRate));
                    animationClipAsset.clipName = clip.name;
                }
            }

            clipAsset.ValidateData();
            return clipAsset;
        }


        static Dictionary<object, HashSet<Action>> assetActionMap = new();
        public static void ResetActionMap()
        {
            assetActionMap.Clear();
        }
        public static void Bind(object asset, Action action)
        {
            if (!assetActionMap.TryGetValue(asset, out var actions))
            {
                actions = new HashSet<Action>();
                assetActionMap.Add(asset, actions);
            }
            actions.Add(action);
        }
        public static void UnBind(object asset, Action action)
        {
            if (assetActionMap.TryGetValue(asset, out var actions))
            {
                actions.Remove(action);
            }
        }
        public static void Run(object asset)
        {
            if (assetActionMap.TryGetValue(asset, out var actions))
            {
                foreach (var item in actions)
                {
                    item.Invoke();
                }
            }
        }


    }
}