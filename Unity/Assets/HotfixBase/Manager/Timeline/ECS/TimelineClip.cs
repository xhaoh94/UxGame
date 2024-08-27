namespace Ux
{
    public abstract class TimelineClip : Entity, IAwakeSystem<TimelineClipAsset>
    {
        public enum TLClipStatus
        {
            Start,
            Pre,
            Ing,
            Post,
            Stop
        }
        public float Time => ParentAs<TimelineTrack>().Time;
        public TLClipStatus Status { get; private set; }
        public bool IsDone => _asset.EndTime <= Time;

        TimelineClipAsset _asset;
        void IAwakeSystem<TimelineClipAsset>.OnAwake(TimelineClipAsset asset)
        {
            _asset = asset;
            Status = TLClipStatus.Start;
            OnStart(asset);
        }
        protected override void OnDestroy()
        {
            _asset = null;
            Status = TLClipStatus.Stop;
            OnStop();
        }


        public void Evaluate(float deltaTime)
        {
            if (Status != TLClipStatus.Ing && Time >= _asset.StartTime && Time <= _asset.EndTime)
            {
                Log.Debug(_asset.clipName + "：Ing");
                Status = TLClipStatus.Ing;
                OnEnable();
            }
            else if (Status != TLClipStatus.Pre && Time < _asset.StartTime)
            {
                Log.Debug(_asset.clipName + "：Pre");
                if (Status == TLClipStatus.Ing)
                {
                    OnDisable();
                }
                Status = TLClipStatus.Pre;
            }
            else if (Status != TLClipStatus.Post && Time > _asset.EndTime)
            {
                Log.Debug(_asset.clipName + "：Post");
                Log.Debug(_asset.clipName + "：Time" +Time);
                Log.Debug(_asset.clipName + "：EndTime" + _asset.EndTime);
                if (Status == TLClipStatus.Ing)
                {
                    OnDisable();
                }
                Status = TLClipStatus.Post;
            }

            OnEvaluate(deltaTime);
        }
        protected abstract void OnStart(TimelineClipAsset asset);
        protected abstract void OnEnable();
        protected abstract void OnDisable();
        protected abstract void OnStop();
        protected abstract void OnEvaluate(float deltaTime);
    }
}
