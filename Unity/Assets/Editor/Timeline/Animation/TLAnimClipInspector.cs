using Assets.Editor.Timeline;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static Ux.AnimationClipAsset;

namespace Ux.Editor.Timeline.Animation
{
    public partial class TLAnimClipInspector : TimelineInspectorBase
    {
        protected VisualElement root;
        public TextField txtName;
        public Toggle tgMove;
        public IntegerField txtStartFrame;
        public Label lbStartTime;
        public IntegerField txtEndFrame;
        public Label lbEndTime;
        public Label lbDurationFrame;
        public Label lbDurationTime;
        public Button btnDuration;
        public Label lbInFrame;
        public Label lbInTime;
        public Label lbOutFrame;
        public Label lbOutTime;
        public EnumField pre;
        public EnumField post;
        public ObjectField ofClip;

        readonly ITimelineEditorClip clip;
        readonly AnimationClipAsset asset;
        int startFrame;
        int endFrame;

        public TLAnimClipInspector(ITimelineEditorSource source, ITimelineEditorClip clip, AnimationClipAsset asset) : base(source, clip, asset)
        {
            BuildUI();
            Add(root);
            this.clip = clip;
            this.asset = asset;
            txtStartFrame.RegisterCallback<FocusInEvent>(_TxtFBlur);
            txtStartFrame.RegisterCallback<FocusOutEvent>(_TxtSBlur);
            txtStartFrame.RegisterCallback<MouseUpEvent>(_TxtUpEvent);

            txtStartFrame.labelElement.style.minWidth = 15;
            txtEndFrame.labelElement.style.minWidth = 15;
            ofClip.objectType = typeof(AnimationClip);
            OnFreshView();
        }

        /// <summary>原 TLAnimClipInspector.uxml 的手搭等价版本。</summary>
        private void BuildUI()
        {
            root = new VisualElement { style = { flexGrow = 1f } };

            txtName = new TextField("Name") { pickingMode = PickingMode.Ignore, style = { marginLeft = 4f } };
            txtName.RegisterValueChangedCallback(_OnTxtNameChanged);
            root.Add(txtName);

            var moveRow = Row();
            tgMove = new Toggle("Move") { style = { marginLeft = 4f } };
            moveRow.Add(tgMove);
            var moveHint = new Label("（勾选情况下，更改时为整体移动模式）");
            moveHint.style.fontSize = 8f;
            moveHint.style.height = 15f;
            moveHint.style.unityTextAlign = TextAnchor.LowerLeft;
            moveHint.style.color = new Color(192f / 255f, 187f / 255f, 79f / 255f);
            moveRow.Add(moveHint);
            root.Add(moveRow);

            var startRow = Row(4f);
            startRow.Add(FixedLabel("Start"));
            txtStartFrame = FrameField();
            txtStartFrame.RegisterValueChangedCallback(_OnTxtStartFrameChanged);
            startRow.Add(txtStartFrame);
            lbStartTime = FixedLabel(string.Empty);
            startRow.Add(lbStartTime);
            root.Add(startRow);

            var endRow = Row(4f);
            endRow.Add(FixedLabel("End"));
            txtEndFrame = FrameField();
            txtEndFrame.RegisterValueChangedCallback(_OnTxtEndFrameChanged);
            endRow.Add(txtEndFrame);
            lbEndTime = FixedLabel(string.Empty);
            endRow.Add(lbEndTime);
            root.Add(endRow);

            var durationRow = Row(4f);
            durationRow.Add(FixedLabel("Duration"));
            lbDurationFrame = FixedLabel(string.Empty, 107f);
            durationRow.Add(lbDurationFrame);
            lbDurationTime = FixedLabel(string.Empty);
            durationRow.Add(lbDurationTime);
            btnDuration = new Button(_OnBtnDurationClick) { text = "修正" };
            durationRow.Add(btnDuration);
            root.Add(durationRow);

            var inRow = Row(4f);
            inRow.Add(FixedLabel("In"));
            lbInFrame = FixedLabel(string.Empty, 107f);
            inRow.Add(lbInFrame);
            lbInTime = FixedLabel(string.Empty);
            inRow.Add(lbInTime);
            root.Add(inRow);

            var outRow = Row(4f);
            outRow.Add(FixedLabel("Out"));
            lbOutFrame = FixedLabel(string.Empty, 107f);
            outRow.Add(lbOutFrame);
            lbOutTime = FixedLabel(string.Empty);
            outRow.Add(lbOutTime);
            root.Add(outRow);

            var extrapolateRow = new VisualElement { style = { flexGrow = 0f } };
            pre = new EnumField("Pre-Extrapolate", PostExtrapolate.None);
            pre.RegisterValueChangedCallback(_OnPreChanged);
            extrapolateRow.Add(pre);
            post = new EnumField("Post-Extrapolate", PostExtrapolate.None);
            post.RegisterValueChangedCallback(_OnPostChanged);
            extrapolateRow.Add(post);
            root.Add(extrapolateRow);

            ofClip = new ObjectField("Clip") { style = { marginLeft = 4f } };
            ofClip.RegisterValueChangedCallback(_OnOfClipChanged);
            root.Add(ofClip);
        }

        private static VisualElement Row(float paddingLeft = 0f)
        {
            var row = new VisualElement();
            row.style.flexGrow = 0f;
            row.style.flexDirection = FlexDirection.Row;
            row.style.height = 20f;
            if (paddingLeft > 0f)
            {
                row.style.paddingLeft = paddingLeft;
            }
            return row;
        }

        private static Label FixedLabel(string text, float width = 100f)
        {
            var label = new Label(text);
            label.style.width = width;
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            return label;
        }

        private static IntegerField FrameField()
        {
            var field = new IntegerField("帧");
            field.style.width = 100f;
            field.style.flexGrow = 0f;
            field.style.flexShrink = 0f;
            field.style.marginLeft = 0f;
            return field;
        }

        protected override void OnFreshView()
        {
            txtName.SetValueWithoutNotify(clip.Name);
            ofClip.SetValueWithoutNotify(asset.clip);

            lbStartTime.text = $" 秒 {clip.StartFrame / (float)Source.FrameRate}";
            txtStartFrame.SetValueWithoutNotify(clip.StartFrame);

            lbEndTime.text = $" 秒 {clip.EndFrame / (float)Source.FrameRate}";
            txtEndFrame.SetValueWithoutNotify(clip.EndFrame);

            lbInTime.text = $"秒  {clip.InFrame / (float)Source.FrameRate}";
            lbInFrame.text = $"帧  {clip.InFrame}";

            lbOutTime.text = $"秒  {clip.OutFrame / (float)Source.FrameRate}";
            lbOutFrame.text = $"帧  {clip.OutFrame}";

            lbDurationTime.text = $"秒  {clip.DurationFrames / (float)Source.FrameRate}";
            lbDurationFrame.text = $"帧  {clip.DurationFrames}";

            pre.Init(asset.pre);
            post.Init(asset.post);
            pre.style.display = asset.PreFrame >= 0 && asset.PreFrame < clip.StartFrame
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            post.style.display = asset.PostFrame >= 0 ? DisplayStyle.Flex : DisplayStyle.None;

            btnDuration.style.display = DisplayStyle.None;
            if (asset.clip != null)
            {
                var targetFrames = asset.clip.length * Source.FrameRate;
                if (Mathf.RoundToInt(targetFrames) != clip.DurationFrames)
                {
                    btnDuration.style.display = DisplayStyle.Flex;
                }
            }
        }

        void _TxtFBlur(FocusInEvent evt)
        {
            startFrame = clip.StartFrame;
            endFrame = clip.EndFrame;
        }

        void _TxtUpEvent(MouseUpEvent evt)
        {
            CheckValid();
        }

        void _TxtSBlur(FocusOutEvent evt)
        {
            CheckValid();
        }

        void CheckValid()
        {
            if (!ChcekValid())
            {
                clip.SetFrames(startFrame, endFrame, false);
                CommitChange();
            }
        }

        private void _OnTxtStartFrameChanged(ChangeEvent<int> evt)
        {
            if (!Source.CanEdit) return;
            var oldStart = clip.StartFrame;
            var oldEnd = clip.EndFrame;
            var nextStart = Mathf.Max(0, evt.newValue);
            var nextEnd = clip.EndFrame;
            if (tgMove.value)
            {
                nextEnd += nextStart - clip.StartFrame;
            }

            clip.RecordUndo("timeline_clip_start_frame");
            clip.SetFrames(nextStart, nextEnd, false);
            if (!ChcekValid())
            {
                clip.SetFrames(oldStart, oldEnd, false);
            }
            CommitChange();
        }

        private void _OnTxtEndFrameChanged(ChangeEvent<int> evt)
        {
            if (!Source.CanEdit) return;
            var oldStart = clip.StartFrame;
            var oldEnd = clip.EndFrame;
            clip.RecordUndo("timeline_clip_end_frame");
            if (tgMove.value)
            {
                var offset = evt.newValue - clip.EndFrame;
                if (clip.StartFrame + offset < 0)
                {
                    offset = -clip.StartFrame;
                }
                clip.SetFrames(clip.StartFrame + offset, clip.EndFrame + offset, false);
            }
            else
            {
                clip.SetFrames(clip.StartFrame, evt.newValue, false);
            }

            if (!ChcekValid())
            {
                clip.SetFrames(oldStart, oldEnd, false);
            }
            CommitChange();
        }

        private void _OnTxtNameChanged(ChangeEvent<string> evt)
        {
            clip.Rename(evt.newValue);
        }

        private void _OnOfClipChanged(ChangeEvent<Object> evt)
        {
            if (Source.CanEdit && evt.newValue is AnimationClip animation &&
                clip.TryAssignAnimation(animation))
            {
                TimelineWindow.RefreshEntity?.Invoke();
                TimelineWindow.wnd?.clipView?.RefreshLayout();
            }
            else
            {
                OnFreshView();
            }
        }

        private void _OnBtnDurationClick()
        {
            if (clip.FitAnimationDuration())
            {
                TimelineWindow.RefreshEntity?.Invoke();
                TimelineWindow.wnd?.clipView?.RefreshLayout();
            }
        }

        private void _OnPreChanged(ChangeEvent<System.Enum> evt)
        {
            if (!Source.CanEdit) return;
            clip.RecordUndo("timeline_clip_pre_extrapolate");
            asset.pre = (PostExtrapolate)evt.newValue;
            CommitChange();
        }

        private void _OnPostChanged(ChangeEvent<System.Enum> evt)
        {
            if (!Source.CanEdit) return;
            clip.RecordUndo("timeline_clip_post_extrapolate");
            asset.post = (PostExtrapolate)evt.newValue;
            CommitChange();
        }
    }
}
