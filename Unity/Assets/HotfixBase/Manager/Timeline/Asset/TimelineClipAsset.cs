using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    [Serializable]
    public abstract class TimelineClipAsset
    {
        public string clipName;

        #region Frame        
        // ��ʼ֡        
        public int StartFrame;
        // 结束帧        
        public int EndFrame;
        // ��ʼ֡->���뻺��֡ Ȩ�� 0->1        
        public int InFrame;
        // �˳�����֡->����֡ Ȩ�� 1->0        
        public int OutFrame;

        #endregion

        #region Time        
        public float StartTime => StartFrame / TimelineMgr.Ins.FrameRate;
        public float EndTime => EndFrame / TimelineMgr.Ins.FrameRate;
        public float InTime => InFrame / TimelineMgr.Ins.FrameRate;
        public float OutTime => OutFrame / TimelineMgr.Ins.FrameRate;

        #endregion

        public abstract Type ClipType { get; }
    }
}

