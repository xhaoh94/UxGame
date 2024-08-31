using Assets.Editor.Timeline;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Ux.Editor.Timeline.Animation;

namespace Ux.Editor.Timeline
{
    public partial class TimelineWindow
    {
        public static TimelineInspectorView InspectorContent { get; set; }
        public static VisualElement ClipContent { get; set; }
        public static System.Func<int, float> GetPositionByFrame { get; set; }
        public static System.Func<int> GetFrameByMousePosition { get; set; }
        public static System.Action<int,int> MarkerMove { get; set; }
        public static TimelineComponent Timeline { get; set; }
        public static TimelineAsset Asset { get; set; }
        public static System.Action SaveAssets { get; set; }
        public static System.Action RefreshEntity { get; set; }
        public static System.Action RefreshView { get; set; }
        public static System.Action RefreshClip { get; set; }
        public static bool IsPlaying { get; set; }
        public static bool IsValid()
        {
            return Asset != null && Timeline != null && !IsPlaying;
        }

        public static float GetDuration(TimelineAsset asset)
        {
            float _duration = 0;
            foreach (var track in asset.tracks)
            {
                var trackDuration = GetDuration(track);
                if (_duration < trackDuration)
                {
                    _duration = trackDuration;
                }
            }
            return _duration;
        }
        public static float GetDuration(TimelineTrackAsset asset)
        {
            float _duration = 0;
            foreach (var clip in asset.clips)
            {
                if (_duration < clip.EndTime)
                {
                    _duration = clip.EndTime;
                }
            }
            return _duration;
        }
        public static TimelineClipAsset CreateClipAsset(Type clipType, int start, UnityEngine.Object arg)
        {
            var clipAsset = Activator.CreateInstance(clipType);
            if (clipAsset is AnimationClipAsset aca)
            {
                aca.StartFrame = start;
                aca.EndFrame = start + 100;
                if (arg != null)
                {
                    if (arg is AnimationClip ac)
                    {
                        aca.clip = ac;
                        aca.EndFrame = start + Mathf.RoundToInt(ac.length * TimelineMgr.Ins.FrameRate);
                        aca.clipName = ac.name;
                    }
                }
                return aca;
            }
            return null;
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