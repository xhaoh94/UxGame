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
        // ��ʼ֡        
        public int StartFrame;
        // ����֡        
        public int EndFrame;
        // ��ʼ֡->���뻺��֡ Ȩ�� 0->1        
        public int InFrame;
        // �˳�����֡->����֡ Ȩ��1->0        
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

