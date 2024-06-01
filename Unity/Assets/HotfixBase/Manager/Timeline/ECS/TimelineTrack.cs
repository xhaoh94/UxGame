using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ux
{
    public class TimelineTrack : Entity, IAwakeSystem<TimelineTrackAsset>
    {
        public TimelineComponent Component => Timeline.Component;
        public TimelineTrackAsset Asset { get; private set; }
        public Timeline Timeline => ParentAs<Timeline>();

        List<TimelineClip> _clips = new List<TimelineClip>();
        void IAwakeSystem<TimelineTrackAsset>.OnAwake(TimelineTrackAsset a)
        {
            Asset = a;
            AddComponent(Asset.TrackType);
            foreach (var asset in Asset.clips)
            {
                _clips.Add(AddChild<TimelineClip, TimelineClipAsset>(asset));
            }
        }
        protected override void OnDestroy()
        {
            _clips.Clear();
            Asset = null;
        }
        public void SetTime(float time)
        {
            foreach (var clip in _clips)
            {
                clip.SetTime(time);
            }
        }

    }
}
