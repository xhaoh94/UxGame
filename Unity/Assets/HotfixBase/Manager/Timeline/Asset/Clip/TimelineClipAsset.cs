using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    [Serializable]
    public abstract class TimelineClipAsset
    {        
        #region Frame        
        // 开始帧        
        public int StartFrame;
        // 结束帧        
        public int EndFrame;
        // 开始帧->进入缓动帧 权重 0->1        
        public int InFrame;
        // 退出缓动帧->结束帧 权重1->0        
        public int OutFrame;

        #endregion

        #region Time
        float _rate => (float)TimelineMgr.Ins.FrameRate;
        public float StartTime => StartFrame / _rate;
        public float EndTime => EndFrame / _rate;
        public float InTime => InFrame / _rate;
        public float OutTime => OutFrame / _rate;

        #endregion

        public abstract Type ClipType { get; }
    }
}

