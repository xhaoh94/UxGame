namespace Ux
{
    public abstract class TimelineClip : Entity, IAwakeSystem<TimelineClipAsset>
    {
        public float Time => ParentAs<TimelineTrack>().Time;
        public bool Active { get; private set; }
        public bool IsDone => _asset.EndTime <= Time;

        TimelineClipAsset _asset;
        void IAwakeSystem<TimelineClipAsset>.OnAwake(TimelineClipAsset asset)
        {            
            _asset = asset;
            OnStart(asset);            
        }
        protected override void OnDestroy()
        {
            _asset = null;
            Active=false;            
        }


        public void Evaluate(float deltaTime)
        {                        
            if (!Active && Time >= _asset.StartTime  && Time <= _asset.EndTime)
            {
                Active = true;
                OnEnable();
            }
            else if (Active && (Time < _asset.StartTime || Time > _asset.EndTime))
            {
                
                Active = false;
                OnDisable();
            }            
            OnEvaluate(deltaTime);
        }
        protected abstract void OnStart(TimelineClipAsset asset);
        protected abstract void OnEnable();
        protected abstract void OnDisable();
        protected abstract void OnEvaluate(float deltaTime);
    }
}
