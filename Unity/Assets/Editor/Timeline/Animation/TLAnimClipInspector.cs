﻿using Assets.Editor.Timeline;
using UnityEngine;
using UnityEngine.UIElements;
using static Ux.AnimationClipAsset;

namespace Ux.Editor.Timeline.Animation
{
    public partial class TLAnimClipInspector : TimelineInspectorBase
    {
        AnimationClipAsset _asset;
        public TLAnimClipInspector(AnimationClipAsset asset) : base(asset)
        {
            CreateChildren();
            Add(root);
            _asset = asset;
            txtStartFrame.RegisterCallback<FocusInEvent>(_TxtFBlur);
            txtStartFrame.RegisterCallback<FocusOutEvent>(_TxtSBlur);
            txtStartFrame.RegisterCallback<MouseUpEvent>(_TxtUpEvent);

            txtStartFrame.labelElement.style.minWidth = 15;
            txtEndFrame.labelElement.style.minWidth = 15;

            ofClip.objectType = typeof(AnimationClip);

            OnFreshView();
        }
        protected override void OnFreshView()
        {
            txtName.SetValueWithoutNotify(_asset.clipName);
            ofClip.SetValueWithoutNotify(_asset.clip);

            lbStartTime.text = $" 秒 {_asset.StartTime}";
            txtStartFrame.SetValueWithoutNotify(_asset.StartFrame);

            lbEndTime.text = $" 秒 {_asset.EndTime}";
            txtEndFrame.SetValueWithoutNotify(_asset.EndFrame);

            lbInTime.text = $"秒  {_asset.InTime}";
            lbInFrame.text = $"帧  {_asset.InFrame}";

            lbOutTime.text = $"秒  {_asset.OutTime}";
            lbOutFrame.text = $"帧  {_asset.OutFrame}";

            lbDurationTime.text = $"秒  {_asset.EndTime - _asset.StartTime}";
            lbDurationFrame.text = $"帧  {_asset.EndFrame - _asset.StartFrame}";

            pre.Init(_asset.pre);
            post.Init(_asset.post);
            pre.style.display = (_asset.PreFrame >= 0 && _asset.PreFrame < _asset.StartFrame) ? DisplayStyle.Flex : DisplayStyle.None;
            post.style.display = _asset.PostFrame >= 0 ? DisplayStyle.Flex : DisplayStyle.None;

            btnDuration.style.display = DisplayStyle.None;
            if (_asset.clip != null)
            {
                var tFrame = _asset.clip.length * TimelineMgr.Ins.FrameRate;
                if (Mathf.RoundToInt(tFrame) != _asset.EndFrame - _asset.StartFrame)
                {
                    btnDuration.style.display = DisplayStyle.Flex;
                }
            }
        }

        int startFrame;
        int endFrame;
        void _TxtFBlur(FocusInEvent e)
        {
            startFrame = _asset.StartFrame;
            endFrame = _asset.EndFrame;
        }
        void _TxtUpEvent(MouseUpEvent e)
        {
            _CheckValid();
        }
        void _TxtSBlur(FocusOutEvent e)
        {
            _CheckValid();
        }

        void _CheckValid()
        {
            if (!ChcekValid())
            {
                _asset.StartFrame = startFrame;
                _asset.EndFrame = endFrame;
                TimelineWindow.Run(_asset);
            }
        }


        partial void _OnTxtStartFrameChanged(ChangeEvent<int> e)
        {
            var frame = e.newValue;
            if (frame < 0)
            {
                frame = 0;
            }
            var oldFrame = _asset.StartFrame;
            _asset.StartFrame = frame;
            if (tgMove.value)
            {
                var off = _asset.StartFrame - oldFrame;
                _asset.EndFrame += off;
            }
            TimelineWindow.Run(_asset);
        }
        partial void _OnTxtEndFrameChanged(ChangeEvent<int> e)
        {
            var frame = e.newValue;
            if (tgMove.value)
            {
                var oldFrame = _asset.EndFrame;
                var off = _asset.EndFrame - oldFrame;
                if (_asset.StartFrame + off < 0)
                {
                    off = -_asset.StartFrame;
                }
                _asset.StartFrame += off;
                _asset.EndFrame += off;
            }
            else if (frame < _asset.StartFrame + 1)
            {
                frame = _asset.StartFrame + 1;
                _asset.EndFrame = frame;
            }
            TimelineWindow.Run(_asset);
        }

        partial void _OnTxtNameChanged(ChangeEvent<string> e)
        {
            _asset.clipName = e.newValue;
            TimelineWindow.Run(_asset);
        }

        partial void _OnOfClipChanged(ChangeEvent<Object> e)
        {
            if (e.newValue is AnimationClip clip)
            {
                _asset.clip = clip;
                _asset.clipName = clip.name;
                TimelineWindow.Run(_asset);
            }
        }

        partial void _OnBtnDurationClick()
        {
            var tFrame = _asset.clip.length * TimelineMgr.Ins.FrameRate;
            var oldEndFrame = _asset.EndFrame;
            _asset.EndFrame = _asset.StartFrame + Mathf.RoundToInt(tFrame);
            TimelineWindow.Run(_asset);
            if (!ChcekValid())
            {
                _asset.EndFrame = oldEndFrame;
            }
        }
        partial void _OnPreChanged(ChangeEvent<System.Enum> e)
        {
            _asset.pre = (PostExtrapolate)e.newValue;
            TimelineWindow.Run(_asset);
        }
        partial void _OnPostChanged(ChangeEvent<System.Enum> e)
        {
            _asset.post = (PostExtrapolate)e.newValue;
            TimelineWindow.Run(_asset);
        }
    }
}