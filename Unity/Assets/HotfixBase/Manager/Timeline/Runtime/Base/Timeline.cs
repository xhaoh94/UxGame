﻿using System.Collections.Generic;

namespace Ux
{
    public class Timeline : Entity, IAwakeSystem<TimelineAsset>
    {
        public float Time { get; private set; }
        public TimelineAsset Asset { get; private set; }
        public TimelineComponent Component => ParentAs<TimelineComponent>();
        List<TimelineTrack> _tacks = new();
        public bool IsDone { get; private set; }

        void IAwakeSystem<TimelineAsset>.OnAwake(TimelineAsset asset)
        {                           
            Asset = asset;
            foreach (var trackAsset in asset.tracks)
            {
                var track = Add(trackAsset.TrackType, trackAsset) as TimelineTrack;
                _tacks.Add(track);
            }
            Time = 0;            
        }

        protected override void OnDestroy()
        {
            _tacks.Clear();    
            Asset = null;
            Time = 0;
            IsDone = false;
        }
        
        public void Evaluate(float deltaTime)
        {
            Time += deltaTime;            
            IsDone = true;
            foreach (var tack in _tacks)
            {
                tack.Evaluate(deltaTime);
                if (IsDone && !tack.IsDone)
                {
                    IsDone = false;
                }
            }
        }

        public void Set(int frame)
        {
            var oldTime = Time;
            Time = TimelineMgr.Ins.FrameConvertTime(frame);            
            IsDone = true;
            foreach (var tack in _tacks)
            {
                tack.Evaluate(Time - oldTime);
                if (IsDone && !tack.IsDone)
                {
                    IsDone = false;
                }
            }
        }
    }
}