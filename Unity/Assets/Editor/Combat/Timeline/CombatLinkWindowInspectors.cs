using Assets.Editor.Timeline;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Ux.Editor.Combat
{
    sealed class CombatLinkWindowTrackInspector : TimelineInspectorBase
    {
        public CombatLinkWindowTrackInspector(
            CombatLogicTimelineSource source,
            CombatLinkWindowEditorTrack track)
            : base(source, track, track)
        {
            Add(CreateTitle("战斗逻辑轨道"));
            Add(new Label("连招衔接"));
            Add(new HelpBox(
                "本轨用于连招和动作分支。输入目标 ActionId 在窗口区间内成立时，当前动作会衔接到目标动作。",
                HelpBoxMessageType.Info));
        }
    }

    sealed class CombatLinkWindowClipInspector : TimelineInspectorBase
    {
        readonly CombatLinkWindowEditorClip clip;
        readonly IntegerField startField;
        readonly IntegerField endField;
        readonly IntegerField targetActionField;
        readonly Toggle hitConfirmToggle;
        readonly Label durationLabel;
        bool refreshing;

        public CombatLinkWindowClipInspector(
            CombatLogicTimelineSource source,
            CombatLinkWindowEditorClip clip)
            : base(source, clip, clip)
        {
            this.clip = clip;
            Add(CreateTitle("连招衔接窗口"));

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
            var idLabel = new Label($"事件 ID\n{clip.Id}");
            idLabel.style.marginTop = 10;
            Add(idLabel);
            Add(new HelpBox(
                "连招衔接只在窗口区间内响应对应输入；结束帧本身不属于窗口。",
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
