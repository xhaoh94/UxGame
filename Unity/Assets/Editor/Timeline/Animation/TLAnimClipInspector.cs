using Assets.Editor.Timeline;
using UnityEngine;
using UnityEngine.UIElements;

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

            txtStartTime.labelElement.style.minWidth = 15;
            txtStartFrame.labelElement.style.minWidth = 15;
            txtEndTime.labelElement.style.minWidth = 15;
            txtEndFrame.labelElement.style.minWidth = 15;
            FreshView();
        }
        void FreshView()
        {
            txtStartTime.SetValueWithoutNotify(_asset.StartTime);
            txtStartFrame.SetValueWithoutNotify(_asset.StartFrame);

            txtEndTime.SetValueWithoutNotify(_asset.EndTime);
            txtEndFrame.SetValueWithoutNotify(_asset.EndFrame);

            lbInTime.text = $"{_asset.InTime}秒";
            lbInFrame.text = $"{_asset.InFrame}帧";

            lbOutTime.text = $"{_asset.OutTime}秒";
            lbOutFrame.text = $"{_asset.OutFrame}帧";

            lbDurationTime.text = $"{_asset.EndTime - _asset.StartTime}秒";
            lbDurationFrame.text = $"{_asset.EndFrame - _asset.StartFrame}帧";

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
            if (CallBack())
            {
                FreshView();
            }
            else
            {
                _asset.StartFrame = startFrame;
                _asset.EndFrame = endFrame;
                CallBack();
                FreshView();
            }
        }


        partial void _OnTxtStartFrameChanged(ChangeEvent<int> e)
        {
            var frame = e.newValue;
            if (frame < 0)
            {
                frame = 0;
            }
            _asset.StartFrame = frame;
            CallBack();
            FreshView();
        }
        partial void _OnTxtEndFrameChanged(ChangeEvent<int> e)
        {
            var frame = e.newValue;
            if (frame < _asset.StartFrame + 1)
            {
                frame = _asset.StartFrame + 1;
            }
            _asset.EndFrame = frame;
            CallBack();
            FreshView();
        }


        partial void _OnOfClipChanged(ChangeEvent<Object> e)
        {
            throw new System.NotImplementedException();
        }

        partial void _OnBtnDurationClick()
        {
            throw new System.NotImplementedException();
        }
    }
}