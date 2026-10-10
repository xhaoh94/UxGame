using Assets.Editor.Timeline;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ux.Editor.Timeline.Animation
{
    public partial class TLAnimTrackInspector : TimelineInspectorBase
    {
        protected VisualElement root;
        public TextField txtName;
        public ObjectField ofAvatarMask;
        public Toggle tgAdditive;
        public ObjectField ofAnimator;

        readonly ITimelineEditorTrack track;
        readonly AnimationTrackAsset asset;

        public TLAnimTrackInspector(ITimelineEditorSource source, ITimelineEditorTrack track, AnimationTrackAsset asset) : base(source, track, asset)
        {
            BuildUI();
            Add(root);
            this.track = track;
            this.asset = asset;
            ofAnimator.objectType = typeof(Animator);
            ofAnimator.allowSceneObjects = true;
            ofAvatarMask.objectType = typeof(AvatarMask);
            var maskHelp = new HelpBox(
                "AvatarMask 说明：空 Mask = 全身覆盖；非空 Mask = 仅限定骨骼。IsAdditive 是真正的增量动画开关，不是并行播放开关。Generic 动画应使用 TransformMask；Humanoid 若只包含人体位且没有 transform paths，可能无法按预期驱动骨骼。",
                HelpBoxMessageType.Info);
            maskHelp.tooltip = "空 Mask 为全身覆盖，非空 Mask 限定骨骼；IsAdditive 表示增量动画。";
            root.Add(maskHelp);
            OnFreshView();
        }

        private void _OnOfAvatarMaskChanged(ChangeEvent<Object> evt)
        {
            if (!Source.CanEdit) return;
            track.RecordUndo("timeline_track_avatar_mask");
            asset.avatarMask = evt.newValue as AvatarMask;
            CommitChange();
        }

        private void _OnTgAdditiveChanged(ChangeEvent<bool> evt)
        {
            if (!Source.CanEdit) return;
            track.RecordUndo("timeline_track_additive");
            asset.isAdditive = evt.newValue;
            CommitChange();
        }

        private void _OnTxtNameChanged(ChangeEvent<string> evt)
        {
            track.Rename(evt.newValue);
            if (ofAnimator.value != null)
            {
                TimelineWindow.RefreshBinds?.Invoke(asset, ofAnimator.value);
            }
        }

        private void _OnOfAnimatorChanged(ChangeEvent<Object> evt)
        {
            TimelineWindow.RefreshBinds?.Invoke(asset, evt.newValue);
            TimelineWindow.RefreshEntity?.Invoke();
        }

        /// <summary>原 TLAnimTrackInspector.uxml 的手搭等价版本。</summary>
        private void BuildUI()
        {
            root = new VisualElement { style = { flexGrow = 1f } };

            txtName = new TextField("Name") { pickingMode = PickingMode.Ignore };
            txtName.RegisterValueChangedCallback(_OnTxtNameChanged);
            root.Add(txtName);

            ofAvatarMask = new ObjectField("Avatar Mask");
            ofAvatarMask.RegisterValueChangedCallback(_OnOfAvatarMaskChanged);
            root.Add(ofAvatarMask);

            tgAdditive = new Toggle("IsAdditive");
            tgAdditive.RegisterValueChangedCallback(_OnTgAdditiveChanged);
            root.Add(tgAdditive);

            ofAnimator = new ObjectField("动画组件");
            ofAnimator.RegisterValueChangedCallback(_OnOfAnimatorChanged);
            root.Add(ofAnimator);
        }

        protected override void OnFreshView()
        {
            txtName.SetValueWithoutNotify(track.Name);
            ofAvatarMask.SetValueWithoutNotify(asset.avatarMask);
            var animator = TimelineWindow.Timeline?.GetBinding<Animator>(asset);
            ofAnimator.SetValueWithoutNotify(animator);
            tgAdditive.SetValueWithoutNotify(asset.isAdditive);
            tgAdditive.style.display = DisplayStyle.Flex;
            tgAdditive.tooltip = "IsAdditive：启用真正的增量动画混合，不代表并行播放。";
            ofAvatarMask.tooltip = "空 Mask = 全身覆盖；非空 Mask = 限定骨骼。Generic 动画请使用 TransformMask。";
        }
    }
}
