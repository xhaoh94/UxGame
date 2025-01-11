using Codice.CM.Common;
using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using static UnityEngine.Rendering.DebugUI;

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
            if (animAsset.clip != null)
            {
                _source = AnimationClipPlayable.Create(PlayableGraph, animAsset.clip);
                _source.SetDuration(animAsset.clip.length);
                _inputPort = Track.Asset.clips.IndexOf(animAsset);
                PlayableGraph.Connect(_source, 0, Track.Mixer, _inputPort);
                _source.Pause();
            }
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
            if (animAsset.clip == null) return;
            float curTime = Time;
            float setTime = 0;
            float setWeight = 0;
            switch (Status)
            {
                case TLClipStatus.Ing:
                    {
                        var _startTime = animAsset.StartTime;
                        var _endTime = animAsset.EndTime;
                        var _inTime = animAsset.InTime;
                        var _outTime = animAsset.OutTime;
                        setTime = curTime - _startTime;
                        if ((_inTime == 0 || curTime >= _inTime) && (_outTime == 0 || curTime <= _outTime))
                        {
                            setWeight = 1;
                        }
                        else if (curTime < _inTime)
                        {
                            setWeight = setTime / (_inTime - _startTime);
                        }
                        else if (curTime > _outTime)
                        {
                            setWeight = 1 - ((curTime - _outTime) / (_endTime - _outTime));
                        }
                    }
                    break;
                case TLClipStatus.Pre:
                    {
                        if (animAsset.PreFrame == -1)
                        {
                            break;
                        }
                        if (curTime <= animAsset.PreTime)
                        {
                            break;
                        }
                        switch (animAsset.pre)
                        {
                            case AnimationClipAsset.PostExtrapolate.Hold:
                                setWeight = 1;
                                break;
                            case AnimationClipAsset.PostExtrapolate.Loop:
                                var _startTime = animAsset.StartTime;
                                var _endTime = animAsset.EndTime;
                                setWeight = 1;
                                var time = curTime - animAsset.PreTime;
                                var len = _endTime - _startTime;
                                var off = _startTime - (time - Mathf.FloorToInt(curTime / len) * len);
                                setTime = len - off;
                                break;
                        }
                    }
                    break;
                case TLClipStatus.Post:
                    {
                        if (animAsset.PostFrame == -1)
                        {
                            break;
                        }
                        if (curTime >= animAsset.PostTime)
                        {
                            break;
                        }
                        var _startTime = animAsset.StartTime;
                        var _endTime = animAsset.EndTime;
                        switch (animAsset.post)
                        {
                            case AnimationClipAsset.PostExtrapolate.Hold:
                                setTime = _endTime - _startTime;
                                setWeight = 1;
                                break;
                            case AnimationClipAsset.PostExtrapolate.Loop:
                                setWeight = 1;
                                var len = _endTime - _startTime;
                                var off = curTime - _endTime;
                                off -= Mathf.FloorToInt(off / len) * len;
                                setTime = off;
                                break;
                        }
                    }
                    break;
            }

            SetWeight(setWeight);
            SetTime(setTime);
        }
        /// <summary>
        /// 权重值
        /// </summary>
        void SetWeight(float value)
        {
            if (_source.IsValid())
            {
                Track.Mixer.SetInputWeight(_inputPort, value);
            }
        }

        void SetTime(float value)
        {
            if (_source.IsValid())
            {
                _source.SetTime(value);
            }
        }

    }
}
