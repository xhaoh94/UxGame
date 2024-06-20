using System.Collections.Generic;

namespace Ux
{
    public class Timeline : Entity, IAwakeSystem<TimelineAsset,TimelineComponent>
    { 
        public TimelineAsset Asset { get; private set; }
        public TimelineComponent Component { get; private set; }
        List<TimelineTrack> _tacks = new List<TimelineTrack>();

        void IAwakeSystem<TimelineAsset, TimelineComponent>.OnAwake(TimelineAsset a, TimelineComponent b)
        {
            Asset = a;            
            Component = b;

            foreach (var asset in Asset.tracks)
            {
                _tacks.Add(AddChild<TimelineTrack, TimelineTrackAsset>(asset));
            }

        }

        protected override void OnDestroy()
        {
            _tacks.Clear();
            Asset = null;
            Component = null;
        }

        public void Bind()
        {

        }
        public void UnBind()
        {

        }

        public void SetTime(float time)
        {
            foreach (var tack in _tacks)
            {
                tack.SetTime(time);
            }
        }
    }
}
