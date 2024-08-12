using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ux.Editor.Timeline
{
    public static class TimelineEditor
    {
        public static VisualElement ClipContent { get; set; }
        public static System.Action OnWheelChanged { get; set; }
        public static System.Func<int, float> GetPositionByFrame { get; set; }
        public static System.Func<int> GetFrameByMousePosition { get; set; }
        public static System.Action<int> MarkerMove { get; set; }
        public static TimelineAsset Asset { get; set; }
        public static System.Action SaveAssets { get; set; }
        public static System.Action RefreshEntity { get; set; }

        public static TimelineClipAsset CreateClipAsset(Type clipType,int start, UnityEngine.Object arg)
        {                  
            var clipAsset = Activator.CreateInstance(clipType);
            if (clipAsset is AnimationClipAsset aca)
            {
                aca.StartFrame = start;
                if (arg != null && arg is AnimationClip ac)
                {
                    aca.clip = ac;
                    aca.EndFrame = start + Mathf.RoundToInt(ac.length * TimelineMgr.Ins.FrameRate);
                    aca.Name = ac.name;
                }
                else
                {
                    aca.EndFrame = start + 100;                    
                }
                return aca;
            }
            return null;
        }
    }
}