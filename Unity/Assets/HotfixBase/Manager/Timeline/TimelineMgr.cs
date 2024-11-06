using UnityEngine;

namespace Ux
{
    public class TimelineMgr : Singleton<TimelineMgr>
    {
        public float FrameRate = 60;

        //帧转换为时间
        public float FrameConvertTime(int frame)
        {
            return frame / FrameRate;
        }
        public int TimeConverFrame(float time)
        {
            return Mathf.FloorToInt(time * FrameRate);
        }
        public TimelineAsset LoadAsset(string assetName)
        {
            var asset = ResMgr.Ins.LoadAsset<TimelineAsset>($"{PathHelper.Res.Timeline}/{assetName}");
            return asset;
        }
    }
}