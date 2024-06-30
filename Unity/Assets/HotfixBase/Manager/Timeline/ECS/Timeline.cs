using SJ;
using System.Collections.Generic;
using UnityEditor.VersionControl;

namespace Ux
{
    public class Timeline : Entity, IAwakeSystem<TimelineAsset>
    {
        public TimelineAsset Asset { get; private set; }
        public TimelineComponent Component => ParentAs<TimelineComponent>();
        List<TimelineTrack> _tacks = new List<TimelineTrack>();

        void IAwakeSystem<TimelineAsset>.OnAwake(TimelineAsset asset)
        {                           
            Asset = asset;
            foreach (var trackAsset in asset.tracks)
            {
                var track = Pool.Get<TimelineTrack>();
                track.Init(this, trackAsset);
                _tacks.Add(track);
            }
        }

        protected override void OnDestroy()
        {
            foreach (var track in _tacks)
            {
                track.Release();
            }
            _tacks.Clear();                        
        }

        public void Bind()
        {

        }
        public void UnBind()
        {

        }

        public void Evaluate(float time)
        {
            foreach (var tack in _tacks)
            {
                tack.Evaluate(time);
            }
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
