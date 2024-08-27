using Codice.CM.Common;
using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Ux
{
    public class TLAnimationClip : TimelineClip
    {
        private AnimationClipPlayable _source;
        private int _inputPort;
        TLAnimationTrack Track => ParentAs<TLAnimationTrack>();
        public PlayableGraph PlayableGraph => Track.Component.PlayableGraph;
        AnimationClipAsset animAsset;
        protected override void OnStart(TimelineClipAsset asset)
        {
            animAsset = asset as AnimationClipAsset;
            _source = AnimationClipPlayable.Create(PlayableGraph, animAsset.clip);
            _source.SetDuration(animAsset.clip.length);
            _inputPort = Track.Asset.clips.IndexOf(animAsset);
            PlayableGraph.Connect(_source, 0, Track.Mixer, _inputPort);
        }

        protected override void OnStop()
        {
            animAsset = null;
            if (PlayableGraph.IsValid() && _source.IsValid())
            {
                PlayableGraph.Disconnect(Track.Mixer, _inputPort);
                PlayableGraph.DestroySubgraph(_source);
            }
            _inputPort = 0;
        }
        protected override void OnEnable()
        {

        }
        protected override void OnDisable()
        {

        }
        protected override void OnEvaluate(float deltaTime)
        {
            
            switch (Status)
            {
                case TLClipStatus.Ing:
                    {
                        var _startTime = animAsset.StartTime;
                        var _endTime = animAsset.EndTime;
                        var _inTime = animAsset.InTime;
                        var _outTime = animAsset.OutTime;
                        float _time = Time - _startTime;
                        _source.SetTime(_time);
                        if ((_inTime == 0 || Time >= _inTime) && (_outTime == 0 || Time <= _outTime))
                        {
                            Weight = 1;
                        }
                        else if (Time < _inTime)
                        {
                            Weight = _time / (_inTime - _startTime);
                        }
                        else if (Time > _outTime)
                        {
                            Weight = 1 - ((Time - _outTime) / (_endTime - _outTime));
                        }
                    }                    
                    break;
                case TLClipStatus.Pre:
                    {
                        if (animAsset.PreFrame == -1)
                        {
                            Weight = 0;
                            break;
                        }
                        if (Time <= animAsset.PreTime)
                        {
                            Weight = 0;
                            break;
                        }
                        switch (animAsset.pre)
                        {
                            case AnimationClipAsset.PostExtrapolate.None:
                                Weight = 0;
                                break;
                            case AnimationClipAsset.PostExtrapolate.Hold:
                                _source.SetTime(0);
                                Weight = 1;
                                break;
                            case AnimationClipAsset.PostExtrapolate.Loop:
                                var _startTime = animAsset.StartTime;
                                var _endTime = animAsset.EndTime;
                                Weight = 1;
                                var time = Time - animAsset.PreTime;
                                var len = _endTime - _startTime;
                                var off = _startTime - (time - Mathf.FloorToInt(Time / len) * len);
                                _source.SetTime(len - off);
                                break;
                        }
                    }                    
                    break;
                case TLClipStatus.Post:
                    {
                        if (animAsset.PostFrame == -1)
                        {
                            Weight = 0;
                            break;
                        }
                        if (Time >= animAsset.PostTime)
                        {
                            Weight = 0;
                            break;
                        }
                        var _startTime = animAsset.StartTime;
                        var _endTime = animAsset.EndTime;
                        switch (animAsset.post)
                        {
                            case AnimationClipAsset.PostExtrapolate.None:
                                Weight = 0;
                                break;
                            case AnimationClipAsset.PostExtrapolate.Hold:
                                _source.SetTime(_endTime - _startTime);
                                Weight = 1;
                                break;
                            case AnimationClipAsset.PostExtrapolate.Loop:
                                Weight = 1;
                                var len = _endTime - _startTime;
                                var off = Time - _endTime;
                                off -= Mathf.FloorToInt(off / len) * len;
                                _source.SetTime(off);
                                break;
                        }
                    }                    
                    break;
                default:
                    Weight = 0;
                    break;
            }
        }
        /// <summary>
        /// 权重值
        /// </summary>
        float Weight
        {
            set
            {
                if (PlayableGraph.IsValid() && _source.IsValid())
                {
                    Track.Mixer.SetInputWeight(_inputPort, value);
                }
            }
        }
    }
}
