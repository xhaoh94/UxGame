using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Ux;
using Ux.Editor.Timeline;
using Ux.Editor.Timeline.Animation;

namespace Assets.Editor.Timeline
{
    public class TimelineInspectorBase : VisualElement
    {
        protected readonly object AssetObject;
        Func<bool> _callback;

        public TimelineInspectorBase(object asset)
        {
            AssetObject = asset;
            style.flexGrow = 1;
            TimelineWindow.Bind(asset, OnFreshView);
        }

        public bool IsSame(object obj) => AssetObject == obj;

        public void SetChcekValid(Func<bool> callback)
        {
            _callback = callback;
        }

        protected bool ChcekValid()
        {
            return _callback?.Invoke() ?? true;
        }

        public void Release()
        {
            TimelineWindow.UnBind(AssetObject, OnFreshView);
        }

        protected static Label CreateTitle(string text)
        {
            var label = new Label(text);
            label.style.fontSize = 14;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginTop = 4;
            label.style.marginBottom = 8;
            return label;
        }

        protected static void CommitChange(object asset)
        {
            TimelineWindow.Run(asset);
            TimelineWindow.SaveAssets?.Invoke();
            TimelineWindow.RefreshEntity?.Invoke();
            TimelineWindow.wnd?.clipView?.RefreshLayout();
        }

        protected virtual void OnFreshView() { }
    }

    public sealed class TimelineInspectorView
    {
        readonly VisualElement root;
        TimelineInspectorBase current;

        public TimelineInspectorView(VisualElement root)
        {
            this.root = root;
            ShowEmptyState();
        }

        public void FreshInspector(object asset, Func<bool> chcekValid)
        {
            if (asset == null)
            {
                Clear();
                ShowEmptyState();
                return;
            }

            if (current != null && current.IsSame(asset))
            {
                current.SetChcekValid(chcekValid);
                return;
            }

            Clear();
            current = asset switch
            {
                AnimationTrackAsset animationTrack => new TLAnimTrackInspector(animationTrack),
                AnimationClipAsset animationClip => new TLAnimClipInspector(animationClip),
                ParticleAssetTrack particleTrack => new ParticleTrackInspector(particleTrack),
                TimelineClipAsset clip => new BasicClipInspector(clip),
                TimelineTrackAsset track => new BasicTrackInspector(track),
                _ => null,
            };

            if (current == null)
            {
                ShowEmptyState();
                return;
            }

            current.SetChcekValid(chcekValid);
            root.Add(current);
        }

        public void Clear()
        {
            current?.Release();
            current = null;
            root.Clear();
        }

        void ShowEmptyState()
        {
            var label = new Label("选择轨道或 Clip 以编辑属性");
            label.style.marginTop = 18;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.color = new Color(0.55f, 0.55f, 0.55f);
            root.Add(label);
        }
    }

    sealed class ParticleTrackInspector : TimelineInspectorBase
    {
        readonly ParticleAssetTrack asset;
        readonly TextField nameField;
        readonly ObjectField particleField;

        public ParticleTrackInspector(ParticleAssetTrack asset) : base(asset)
        {
            this.asset = asset;
            Add(CreateTitle("粒子轨道"));

            nameField = new TextField("名称");
            nameField.SetValueWithoutNotify(asset.trackName);
            nameField.RegisterValueChangedCallback(evt =>
            {
                asset.trackName = evt.newValue;
                CommitChange(asset);
            });
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
            nameField?.SetValueWithoutNotify(asset.trackName);
            particleField?.SetValueWithoutNotify(
                TimelineWindow.Timeline?.GetBinding<ParticleSystem>(asset));
        }
    }

    sealed class BasicTrackInspector : TimelineInspectorBase
    {
        readonly TimelineTrackAsset asset;
        readonly TextField nameField;

        public BasicTrackInspector(TimelineTrackAsset asset) : base(asset)
        {
            this.asset = asset;
            Add(CreateTitle("轨道"));
            nameField = new TextField("名称");
            nameField.SetValueWithoutNotify(asset.trackName);
            nameField.RegisterValueChangedCallback(evt =>
            {
                asset.trackName = evt.newValue;
                CommitChange(asset);
            });
            Add(nameField);
            Add(new Label($"Track ID\n{asset.Id}"));
        }

        protected override void OnFreshView()
        {
            nameField?.SetValueWithoutNotify(asset.trackName);
        }
    }

    sealed class BasicClipInspector : TimelineInspectorBase
    {
        readonly TimelineClipAsset asset;
        readonly TextField nameField;
        readonly IntegerField startField;
        readonly IntegerField endField;
        readonly Label durationLabel;
        bool refreshing;

        public BasicClipInspector(TimelineClipAsset asset) : base(asset)
        {
            this.asset = asset;
            Add(CreateTitle(asset is ParticleClipAsset ? "粒子 Clip" : "Timeline Clip"));

            nameField = new TextField("名称");
            startField = new IntegerField("开始帧");
            endField = new IntegerField("结束帧");
            durationLabel = new Label();

            nameField.RegisterValueChangedCallback(evt =>
            {
                if (refreshing) return;
                asset.clipName = evt.newValue;
                CommitChange(asset);
                RefreshFields();
            });
            startField.RegisterValueChangedCallback(evt =>
            {
                if (refreshing) return;
                var oldStart = asset.StartFrame;
                var oldEnd = asset.EndFrame;
                asset.StartFrame = Mathf.Max(0, evt.newValue);
                asset.EndFrame = Mathf.Max(asset.StartFrame + 1, asset.EndFrame);
                if (!ChcekValid())
                {
                    asset.StartFrame = oldStart;
                    asset.EndFrame = oldEnd;
                }
                CommitChange(asset);
                RefreshFields();
            });
            endField.RegisterValueChangedCallback(evt =>
            {
                if (refreshing) return;
                var oldEnd = asset.EndFrame;
                asset.EndFrame = Mathf.Max(asset.StartFrame + 1, evt.newValue);
                if (!ChcekValid())
                {
                    asset.EndFrame = oldEnd;
                }
                CommitChange(asset);
                RefreshFields();
            });

            Add(nameField);
            Add(startField);
            Add(endField);
            durationLabel.style.marginTop = 6;
            Add(durationLabel);
            var id = new Label($"Clip ID\n{asset.Id}");
            id.style.marginTop = 10;
            id.style.color = new Color(0.6f, 0.6f, 0.6f);
            Add(id);
            RefreshFields();
        }

        void RefreshFields()
        {
            refreshing = true;
            nameField.SetValueWithoutNotify(asset.clipName);
            startField.SetValueWithoutNotify(asset.StartFrame);
            endField.SetValueWithoutNotify(asset.EndFrame);
            durationLabel.text = $"长度：{asset.DurationFrames} 帧 / " +
                                 $"{TimelineWindow.FrameToTime(asset.DurationFrames):0.###} 秒";
            refreshing = false;
        }

        protected override void OnFreshView()
        {
            RefreshFields();
        }
    }
}
