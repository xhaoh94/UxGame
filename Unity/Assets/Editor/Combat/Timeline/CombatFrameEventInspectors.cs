using Assets.Editor.Timeline;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Ux.Editor.Timeline;

namespace Ux.Editor.Combat
{
    sealed class CombatFrameEventInspector : TimelineInspectorBase
    {
        readonly CombatLogicTimelineSource source;
        readonly CombatFrameEventEditorAdapter frameEvent;
        readonly IntegerField frameField;
        readonly TextField displayNameField;
        readonly ObjectField spawnField;
        bool refreshing;

        public CombatFrameEventInspector(
            CombatLogicTimelineSource source,
            CombatFrameEventEditorAdapter frameEvent)
            : base(source, frameEvent, frameEvent)
        {
            this.source = source;
            this.frameEvent = frameEvent;

            Add(CreateTitle("帧事件"));
            Add(new Label($"类型：{frameEvent.TypeName}"));

            displayNameField = new TextField("事件名称");
            displayNameField.RegisterValueChangedCallback(evt =>
            {
                if (refreshing || !Source.CanEdit)
                {
                    return;
                }
                frameEvent.SetDisplayName(evt.newValue);
                RefreshFields();
            });
            Add(displayNameField);

            frameField = new IntegerField("触发帧");
            frameField.RegisterValueChangedCallback(evt =>
            {
                if (refreshing || !Source.CanEdit)
                {
                    return;
                }
                frameEvent.SetFrame(evt.newValue);
                RefreshFields();
            });
            Add(frameField);

            spawnField = new ObjectField("生成物")
            {
                objectType = typeof(CombatSpawnProfile),
                allowSceneObjects = false,
            };
            spawnField.RegisterValueChangedCallback(evt =>
            {
                if (refreshing || !Source.CanEdit)
                {
                    return;
                }
                frameEvent.SetSpawnProfile(evt.newValue as CombatSpawnProfile);
                RefreshFields();
            });
            Add(spawnField);

            var deleteButton = new Button(() =>
            {
                if (!frameEvent.Remove())
                {
                    return;
                }
                TimelineWindow.InspectorContent?.FreshInspector(null, null);
            })
            {
                text = "删除事件",
            };
            deleteButton.style.marginTop = 8;
            Add(deleteButton);

            var idLabel = new Label($"事件 ID\n{frameEvent.Id}");
            idLabel.style.marginTop = 10;
            Add(idLabel);
            Add(new HelpBox(
                "帧事件只在触发帧成立一次；它不是区间窗口，也不会持续占用一条逻辑轨。",
                HelpBoxMessageType.Info));
            RefreshFields();
        }

        void RefreshFields()
        {
            refreshing = true;
            displayNameField.SetValueWithoutNotify(frameEvent.DisplayName);
            frameField.SetValueWithoutNotify(frameEvent.Frame);
            spawnField.SetValueWithoutNotify(frameEvent.SpawnProfile);
            refreshing = false;
        }

        protected override void OnFreshView()
        {
            RefreshFields();
        }
    }
}
