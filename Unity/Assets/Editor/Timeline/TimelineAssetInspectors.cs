using Assets.Editor.Timeline;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ux.Editor.Timeline
{
    sealed class ParticleTrackInspector : TimelineInspectorBase
    {
        readonly ITimelineEditorTrack track;
        readonly ParticleAssetTrack asset;
        readonly TextField nameField;
        readonly ObjectField particleField;

        public ParticleTrackInspector(ITimelineEditorSource source, ITimelineEditorTrack track, ParticleAssetTrack asset) : base(source, track, asset)
        {
            this.track = track;
            this.asset = asset;
            Add(CreateTitle("粒子轨道"));

            nameField = new TextField("名称");
            nameField.SetValueWithoutNotify(track.Name);
            nameField.RegisterValueChangedCallback(evt => track.Rename(evt.newValue));
            Add(nameField);

            particleField = new ObjectField("Particle System")
            {
                objectType = typeof(ParticleSystem),
                allowSceneObjects = true,
            };
            particleField.SetValueWithoutNotify(
                TimelineWindow.Timeline?.GetBinding<ParticleSystem>(asset));
            particleField.RegisterValueChangedCallback(evt =>
            {
                TimelineWindow.RefreshBinds?.Invoke(asset, evt.newValue);
                TimelineWindow.RefreshEntity?.Invoke();
            });
            Add(particleField);

            var help = new HelpBox(
                "粒子 Clip 使用该轨道绑定的 ParticleSystem。拖动时间标尺时会按绝对帧重建粒子状态。",
                HelpBoxMessageType.Info);
            help.style.marginTop = 8;
            Add(help);
        }

        protected override void OnFreshView()
        {
            nameField?.SetValueWithoutNotify(track.Name);
            particleField?.SetValueWithoutNotify(
                TimelineWindow.Timeline?.GetBinding<ParticleSystem>(asset));
        }
    }

    sealed class BasicTrackInspector : TimelineInspectorBase
    {
        readonly ITimelineEditorTrack track;
        readonly TextField nameField;

        public BasicTrackInspector(ITimelineEditorSource source, ITimelineEditorTrack track, TimelineTrackAsset asset) : base(source, track, asset)
        {
            this.track = track;
            Add(CreateTitle("轨道"));
            nameField = new TextField("名称");
            nameField.SetValueWithoutNotify(track.Name);
            nameField.RegisterValueChangedCallback(evt => track.Rename(evt.newValue));
            Add(nameField);
            Add(new Label($"Track ID\n{track.Id}"));
        }

        protected override void OnFreshView()
        {
            nameField?.SetValueWithoutNotify(track.Name);
        }
    }

    sealed class BasicClipInspector : TimelineInspectorBase
    {
        readonly ITimelineEditorClip clip;
        readonly TextField nameField;
        readonly IntegerField startField;
        readonly IntegerField endField;
        readonly Label durationLabel;
        bool refreshing;

        public BasicClipInspector(ITimelineEditorSource source, ITimelineEditorClip clip, TimelineClipAsset asset) : base(source, clip, asset)
        {
            this.clip = clip;
            Add(CreateTitle(asset is ParticleClipAsset ? "粒子 Clip" : "Timeline Clip"));

            nameField = new TextField("名称");
            startField = new IntegerField("开始帧");
            endField = new IntegerField("结束帧");
            durationLabel = new Label();

            nameField.RegisterValueChangedCallback(evt =>
            {
                if (refreshing) return;
                clip.Rename(evt.newValue);
                RefreshFields();
            });
            startField.RegisterValueChangedCallback(evt =>
            {
                if (refreshing || !Source.CanEdit) return;
                var oldStart = clip.StartFrame;
                var oldEnd = clip.EndFrame;
                clip.RecordUndo("timeline_clip_start_frame");
                clip.SetFrames(evt.newValue, clip.EndFrame, false);
                if (!ChcekValid())
                {
                    clip.SetFrames(oldStart, oldEnd, false);
                }
                clip.CommitEdit();
                RefreshFields();
            });
            endField.RegisterValueChangedCallback(evt =>
            {
                if (refreshing || !Source.CanEdit) return;
                var oldStart = clip.StartFrame;
                var oldEnd = clip.EndFrame;
                clip.RecordUndo("timeline_clip_end_frame");
                clip.SetFrames(clip.StartFrame, evt.newValue, false);
                if (!ChcekValid())
                {
                    clip.SetFrames(oldStart, oldEnd, false);
                }
                clip.CommitEdit();
                RefreshFields();
            });

            Add(nameField);
            Add(startField);
            Add(endField);
            durationLabel.style.marginTop = 6;
            Add(durationLabel);
            var id = new Label($"Clip ID\n{clip.Id}");
            id.style.marginTop = 10;
            id.style.color = new Color(0.6f, 0.6f, 0.6f);
            Add(id);
            RefreshFields();
        }

        void RefreshFields()
        {
            refreshing = true;
            nameField.SetValueWithoutNotify(clip.Name);
            startField.SetValueWithoutNotify(clip.StartFrame);
            endField.SetValueWithoutNotify(clip.EndFrame);
            durationLabel.text = $"长度：{clip.DurationFrames} 帧 / " +
                                 $"{clip.DurationFrames / (float)Source.FrameRate:0.###} 秒";
            refreshing = false;
        }

        protected override void OnFreshView()
        {
            RefreshFields();
        }
    }
}
