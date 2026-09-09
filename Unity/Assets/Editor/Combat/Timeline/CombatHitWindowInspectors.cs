using Assets.Editor.Timeline;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Ux.Editor.Combat
{
    sealed class CombatHitWindowTrackInspector : TimelineInspectorBase
    {
        public CombatHitWindowTrackInspector(
            CombatLogicTimelineSource source,
            CombatHitWindowEditorTrack track) : base(source, track, track)
        {
            Add(CreateTitle("战斗逻辑轨道"));
            Add(new Label("命中激活窗口"));
            Add(new HelpBox(
                "本轨只声明确定性命中时序，不包含形状、目标查询或伤害。每条窗口代表独立命中周期，允许相邻或重叠。",
                HelpBoxMessageType.Info));
        }
    }

    sealed class CombatHitWindowClipInspector : TimelineInspectorBase
    {
        readonly CombatHitWindowEditorClip clip;
        readonly IntegerField startField;
        readonly IntegerField endField;
        readonly EnumField shapeField;
        readonly IntegerField radiusField;
        readonly Label durationLabel;
        bool refreshing;

        public CombatHitWindowClipInspector(
            CombatLogicTimelineSource source,
            CombatHitWindowEditorClip clip) : base(source, clip, clip)
        {
            this.clip = clip;
            Add(CreateTitle("命中激活窗口"));

            startField = new IntegerField("开始帧（含）");
            endField = new IntegerField("结束帧（不含）");
            shapeField = new EnumField("形状", ActionHitShape.Circle);
            radiusField = new IntegerField("半径（毫米）");
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

            shapeField.RegisterValueChangedCallback(evt =>
            {
                if (refreshing || !Source.CanEdit) return;
                clip.SetGeometry((ActionHitShape)evt.newValue, clip.RadiusMillimeters);
                RefreshFields();
            });
            radiusField.RegisterValueChangedCallback(evt =>
            {
                if (refreshing || !Source.CanEdit) return;
                clip.SetGeometry(clip.Shape, evt.newValue);
                RefreshFields();
            });

            Add(startField);
            Add(endField);
            Add(shapeField);
            Add(radiusField);
            durationLabel.style.marginTop = 6;
            Add(durationLabel);
            var idLabel = new Label($"窗口 ID\n{clip.Id}");
            idLabel.style.marginTop = 10;
            Add(idLabel);
            Add(new HelpBox(
                "运行时由 CombatActionRunner 按序列化列表顺序解释当前帧激活窗口；结束帧本身不属于窗口。",
                HelpBoxMessageType.Info));
            RefreshFields();
        }

        void RefreshFields()
        {
            refreshing = true;
            startField.SetValueWithoutNotify(clip.StartFrame);
            endField.SetValueWithoutNotify(clip.EndFrame);
            shapeField.SetValueWithoutNotify(clip.Shape);
            radiusField.SetValueWithoutNotify(clip.RadiusMillimeters);
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
