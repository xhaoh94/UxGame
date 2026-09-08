using Assets.Editor.Timeline;
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

            lbStartTime.text = $" 秒 {TimelineWindow.FrameToTime(_asset.StartFrame)}";
            txtStartFrame.SetValueWithoutNotify(_asset.StartFrame);

            lbEndTime.text = $" 秒 {TimelineWindow.FrameToTime(_asset.EndFrame)}";
            txtEndFrame.SetValueWithoutNotify(_asset.EndFrame);

            lbInTime.text = $"秒  {TimelineWindow.FrameToTime(_asset.InFrame)}";
            lbInFrame.text = $"帧  {_asset.InFrame}";

            lbOutTime.text = $"秒  {TimelineWindow.FrameToTime(_asset.OutFrame)}";
            lbOutFrame.text = $"帧  {_asset.OutFrame}";

            lbDurationTime.text = $"秒  {TimelineWindow.FrameToTime(_asset.EndFrame - _asset.StartFrame)}";
            lbDurationFrame.text = $"帧  {_asset.EndFrame - _asset.StartFrame}";

            pre.Init(_asset.pre);
            post.Init(_asset.post);
            pre.style.display = (_asset.PreFrame >= 0 && _asset.PreFrame < _asset.StartFrame) ? DisplayStyle.Flex : DisplayStyle.None;
            post.style.display = _asset.PostFrame >= 0 ? DisplayStyle.Flex : DisplayStyle.None;

            btnDuration.style.display = DisplayStyle.None;
            if (_asset.clip != null)
            {
                var tFrame = _asset.clip.length * TimelineWindow.FrameRate;
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
                CommitChange(_asset);
            }
        }

        partial void _OnTxtStartFrameChanged(ChangeEvent<int> e)
        {
            var frame = Mathf.Max(0, e.newValue);
            var oldFrame = _asset.StartFrame;
            _asset.StartFrame = frame;
            if (tgMove.value)
            {
                var off = _asset.StartFrame - oldFrame;
                _asset.EndFrame += off;
            }
            _asset.EndFrame = Mathf.Max(_asset.StartFrame + 1, _asset.EndFrame);
            CommitChange(_asset);
        }

        partial void _OnTxtEndFrameChanged(ChangeEvent<int> e)
        {
            var frame = e.newValue;
            if (tgMove.value)
            {
                var off = frame - _asset.EndFrame;
                if (_asset.StartFrame + off < 0)
                {
                    off = -_asset.StartFrame;
                }
                _asset.StartFrame += off;
                _asset.EndFrame += off;
            }
            else
            {
                _asset.EndFrame = Mathf.Max(_asset.StartFrame + 1, frame);
            }
            CommitChange(_asset);
        }

        partial void _OnTxtNameChanged(ChangeEvent<string> e)
        {
            _asset.clipName = e.newValue;
            CommitChange(_asset);
        }

        partial void _OnOfClipChanged(ChangeEvent<Object> e)
        {
            if (e.newValue is AnimationClip clip)
            {
                _asset.clip = clip;
                _asset.clipName = clip.name;
                _asset.EndFrame = _asset.StartFrame +
                    Mathf.Max(1, Mathf.RoundToInt(clip.length * TimelineWindow.FrameRate));
                CommitChange(_asset);
            }
        }

        partial void _OnBtnDurationClick()
        {
            if (_asset.clip == null)
            {
                return;
            }
            var tFrame = _asset.clip.length * TimelineWindow.FrameRate;
            var oldEndFrame = _asset.EndFrame;
            _asset.EndFrame = _asset.StartFrame + Mathf.Max(1, Mathf.RoundToInt(tFrame));
            if (!ChcekValid())
            {
                _asset.EndFrame = oldEndFrame;
            }
            CommitChange(_asset);
        }

        partial void _OnPreChanged(ChangeEvent<System.Enum> e)
        {
            _asset.pre = (PostExtrapolate)e.newValue;
            CommitChange(_asset);
        }

        partial void _OnPostChanged(ChangeEvent<System.Enum> e)
        {
            _asset.post = (PostExtrapolate)e.newValue;
            CommitChange(_asset);
        }
    }
}