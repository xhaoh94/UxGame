namespace Ux
{
    public interface ITimelineClip
    {
        void OnEnable();
        void OnDisable();
        void SetTime(float time);
    }
    public class TimelineClip : Entity, IAwakeSystem<TimelineClipAsset>, IFixedUpdateSystem
    {
        public TimelineClipAsset Asset { get; private set; }
        public TimelineTrack Track => ParentAs<TimelineTrack>();
        public TimelineComponent Component => Track.Component;

        public float Time { get; protected set; }        
        public bool Active { get; protected set; }

        ITimelineClip _clip;
        void IAwakeSystem<TimelineClipAsset>.OnAwake(TimelineClipAsset a)
        {
            Asset = a;
            _clip = AddComponent(Asset.ClipType) as ITimelineClip;
        }
        void IFixedUpdateSystem.OnFixedUpdate()
        {
            Evaluate(UnityEngine.Time.deltaTime * (float)Component.PlaySpeed);
            _clip?.SetTime(Time);
        }
        public void Evaluate(float deltaTime)
        {            
            var _time = Time + deltaTime;

            if (!Active && Asset.StartTime <= _time && _time <= Asset.EndTime)
            {
                Active = true;
                OnEnable();
            }
            else if (Active && (_time < Asset.StartTime || Asset.EndTime < _time))
            {
                Active = false;
                OnDisable();
            }

            Time = _time;
        }
        public void OnEnable()
        {
            _clip?.OnEnable();
        }
        public void OnDisable()
        {
            _clip?.OnDisable();
        }

        public void SetTime(float time)
        {
            _clip?.SetTime(time);
        }
    }
}
