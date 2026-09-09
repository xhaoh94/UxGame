using Assets.Editor.Timeline;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Ux.Editor.Combat
{
    sealed class CombatCancelWindowTrackInspector : TimelineInspectorBase
    {
        public CombatCancelWindowTrackInspector(
            CombatLogicTimelineSource source,
            CombatCancelWindowEditorTrack track) : base(source, track, track)
        {
            Add(CreateTitle("战斗逻辑轨道"));
            Add(new Label("取消窗口"));
            Add(new HelpBox(
                "区间采用 [开始帧, 结束帧) 半开语义。多个取消窗口允许重叠，并由目标 ActionId 与命中条件分别判定。",
                HelpBoxMessageType.Info));
        }
    }

    sealed class CombatCancelWindowClipInspector : TimelineInspectorBase
    {
        readonly CombatCancelWindowEditorClip clip;
        readonly IntegerField startField;
        readonly IntegerField endField;
        readonly IntegerField targetActionField;
        readonly Toggle hitConfirmToggle;
        readonly Label durationLabel;
        bool refreshing;

        public CombatCancelWindowClipInspector(
            CombatLogicTimelineSource source,
            CombatCancelWindowEditorClip clip) : base(source, clip, clip)
        {
            this.clip = clip;
            Add(CreateTitle("取消窗口"));

            startField = new IntegerField("开始帧（含）");
            endField = new IntegerField("结束帧（不含）");
            targetActionField = new IntegerField("目标 ActionId");
            hitConfirmToggle = new Toggle("需要命中确认");
            durationLabel = new Label();

            startField.RegisterValueChangedCallback(evt =>
            {
                if (refreshing || !Source.CanEdit) return;
                clip.SetFrames(evt.newValue, clip.EndFrame);
                RefreshFields();
            });
            endField.RegisterValueChangedCallback(evt =>
            {
                if (refreshing || !Source.CanEdit) return;
                clip.SetFrames(clip.StartFrame, evt.newValue);
                RefreshFields();
            });
            targetActionField.RegisterValueChangedCallback(evt =>
            {
                if (refreshing || !Source.CanEdit) return;
                clip.SetTargetActionId(evt.newValue);
                RefreshFields();
            });
            hitConfirmToggle.RegisterValueChangedCallback(evt =>
            {
                if (refreshing || !Source.CanEdit) return;
                clip.SetRequiresHitConfirm(evt.newValue);
                RefreshFields();
            });

            Add(startField);
            Add(endField);
            Add(targetActionField);
            Add(hitConfirmToggle);
            durationLabel.style.marginTop = 6;
            Add(durationLabel);
            var idLabel = new Label($"窗口 ID\n{clip.Id}");
            idLabel.style.marginTop = 10;
            Add(idLabel);
            Add(new HelpBox(
                "结束帧本身不属于取消窗口；运行时仅在 actionFrame < EndFrame 时接受目标动作。",
                HelpBoxMessageType.Info));
            RefreshFields();
        }

        void RefreshFields()
        {
            refreshing = true;
            startField.SetValueWithoutNotify(clip.StartFrame);
            endField.SetValueWithoutNotify(clip.EndFrame);
            targetActionField.SetValueWithoutNotify(clip.TargetActionId);
            hitConfirmToggle.SetValueWithoutNotify(clip.RequiresHitConfirm);
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
