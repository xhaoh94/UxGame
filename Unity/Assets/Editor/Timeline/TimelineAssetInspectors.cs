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

    sealed class ParticleClipInspector : TimelineInspectorBase
    {
        readonly ITimelineEditorClip clip;
        readonly ParticleClipAsset asset;
        readonly TextField nameField;
        readonly IntegerField startField;
        readonly IntegerField endField;
        readonly ColorField colorField;
        readonly Vector3Field positionField;
        readonly Vector3Field rotationField;
        readonly FloatField scaleField;
        bool refreshing;

        public ParticleClipInspector(ITimelineEditorSource source, ITimelineEditorClip clip, ParticleClipAsset asset) : base(source, clip, asset)
        {
            this.clip = clip;
            this.asset = asset;
            Add(CreateTitle("粒子 Clip"));

            nameField = new TextField("名称");
            startField = new IntegerField("开始帧");
            endField = new IntegerField("结束帧");
            colorField = new ColorField("起始颜色");
            positionField = new Vector3Field("位置偏移");
            rotationField = new Vector3Field("旋转偏移");
            scaleField = new FloatField("缩放倍率");

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
            colorField.RegisterValueChangedCallback(evt =>
            {
                if (refreshing || !Source.CanEdit) return;
                clip.RecordUndo("timeline_clip_particle_color");
                asset.startColor = evt.newValue;
                CommitChange();
                // 颜色在 Clip 激活时生效，必须让预览重播一次当前层才能看到。
                TimelineWindow.RefreshEntity?.Invoke();
            });
            positionField.RegisterValueChangedCallback(evt =>
            {
                if (refreshing || !Source.CanEdit) return;
                clip.RecordUndo("timeline_clip_particle_position");
                asset.positionOffset = evt.newValue;
                CommitChange();
                TimelineWindow.RefreshEntity?.Invoke();
            });
            rotationField.RegisterValueChangedCallback(evt =>
            {
                if (refreshing || !Source.CanEdit) return;
                clip.RecordUndo("timeline_clip_particle_rotation");
                asset.rotationEuler = evt.newValue;
                CommitChange();
                TimelineWindow.RefreshEntity?.Invoke();
            });
            scaleField.RegisterValueChangedCallback(evt =>
            {
                if (refreshing || !Source.CanEdit) return;
                clip.RecordUndo("timeline_clip_particle_scale");
                asset.scaleFactor = evt.newValue;
                CommitChange();
                TimelineWindow.RefreshEntity?.Invoke();
            });

            Add(nameField);
            Add(startField);
            Add(endField);
            Add(colorField);
            Add(positionField);
            Add(rotationField);
            Add(scaleField);

            var help = new HelpBox(
                "颜色与位姿都在 Clip 激活时写入轨道绑定的 ParticleSystem；位姿是相对美术摆放值的偏移。" +
                "切换 Clip 时预览会重播，拖动标尺则按绝对帧重建粒子状态。",
                HelpBoxMessageType.Info);
            help.style.marginTop = 8;
            Add(help);
            RefreshFields();
        }

        void RefreshFields()
        {
            refreshing = true;
            nameField.SetValueWithoutNotify(clip.Name);
            startField.SetValueWithoutNotify(clip.StartFrame);
            endField.SetValueWithoutNotify(clip.EndFrame);
            colorField.SetValueWithoutNotify(asset.startColor);
            positionField.SetValueWithoutNotify(asset.positionOffset);
            rotationField.SetValueWithoutNotify(asset.rotationEuler);
            scaleField.SetValueWithoutNotify(asset.scaleFactor);
            refreshing = false;
        }

        protected override void OnFreshView()
        {
            RefreshFields();
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
