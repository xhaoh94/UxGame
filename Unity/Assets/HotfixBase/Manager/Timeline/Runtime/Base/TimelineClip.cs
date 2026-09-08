namespace Ux
{
    public abstract class TimelineClip : Entity, IAwakeSystem<TimelineClipAsset, int>
    {
        public enum TLClipStatus
        {
            Start,
            Pre,
            Ing,
            Post,
            Stop
        }

        public TimelineTrack Track => ParentAs<TimelineTrack>();
        public int CurrentFrame => Track.CurrentFrame;
        public int FrameRate => Track.Timeline.FrameRate;
        public int InputIndex { get; private set; }
        public TLClipStatus Status { get; private set; }
        public bool IsDone => CurrentFrame >= (Asset?.EndFrame ?? 0);
        protected TimelineClipAsset Asset { get; private set; }

        void IAwakeSystem<TimelineClipAsset, int>.OnAwake(TimelineClipAsset asset, int inputIndex)
        {
            Asset = asset;
            InputIndex = inputIndex;
            Status = TLClipStatus.Start;
            OnStart(asset);
        }

        protected override void OnDestroy()
        {
            if (Status == TLClipStatus.Ing)
            {
                OnDisable();
            }
            OnStop();
            Asset = null;
            InputIndex = 0;
            Status = TLClipStatus.Stop;
        }

        public void StopImmediate()
        {
            if (Status == TLClipStatus.Ing)
            {
                OnDisable();
            }
            Status = TLClipStatus.Stop;
        }

        public void Evaluate(in TimelineEvaluationContext context)
        {
            var nextStatus = GetStatus(context.CurrentFrame);
            if (nextStatus != Status)
            {
                if (Status == TLClipStatus.Ing)
                {
                    OnDisable();
                }

                Status = nextStatus;
                if (Status == TLClipStatus.Ing)
                {
                    OnEnable();
                }
            }

            OnEvaluate(context);
        }

        private TLClipStatus GetStatus(int frame)
        {
            if (frame < Asset.StartFrame)
            {
                return TLClipStatus.Pre;
            }
            if (frame >= Asset.EndFrame)
            {
                return TLClipStatus.Post;
            }
            return TLClipStatus.Ing;
        }

        protected float FrameToTime(int frame)
        {
            return frame / (float)FrameRate;
        }

        protected abstract void OnStart(TimelineClipAsset asset);
        protected abstract void OnEnable();
        protected abstract void OnDisable();
        protected abstract void OnStop();
        protected abstract void OnEvaluate(in TimelineEvaluationContext context);
    }
}
