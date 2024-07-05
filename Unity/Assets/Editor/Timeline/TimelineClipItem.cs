using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
namespace Ux.Editor.Timeline
{
    public class TimelineClipItem : VisualElement, IToolbarMenuElement
    {
        TimelineClipAsset asset;
        TimelineWindow window;
        TimelineTrackItem trackItem;
        VisualElement content;
        VisualElement left;
        VisualElement center;
        Label lbType;
        VisualElement right;
        public DropdownMenu menu { get; }
        public TimelineClipItem()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Timeline/TimelineClipItem.uxml");
            visualTree.CloneTree(this);
            content = this.Q<VisualElement>("content");
            left = this.Q<VisualElement>("left");
            center = this.Q<VisualElement>("center");
            lbType = this.Q<Label>("lbType");
            right = this.Q<VisualElement>("right");

            menu = new DropdownMenu();

            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<DragUpdatedEvent>(_OnDragUpd);
            RegisterCallback<DragPerformEvent>(_OnDragPerform);

        }
        void _OnDragUpd(DragUpdatedEvent e)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Move;
        }
        void _OnDragPerform(DragPerformEvent e)
        {
            if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
            {
                string retPath = DragAndDrop.paths[0];
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(retPath);
                if (clip == null)
                {
                    return;
                }
                var asset = trackItem.CreateClipAsset<AnimationClipAsset>();                
                asset.StartFrame =Mathf.CeilToInt(trackItem.asset.MaxTime / TimelineMgr.Ins.FrameRate);
                asset.EndFrame = asset.StartFrame + Mathf.RoundToInt(clip.length * TimelineMgr.Ins.FrameRate);
                asset.clip = clip;
                asset.Name = clip.name;
                trackItem.AddClipItem(asset);
            }
        }
        void OnPointerDown(PointerDownEvent e)
        {
            if (e.button == 0)
            {

            }
            else if (e.button == 1)
            {
                menu.AppendAction("ÐÞÕý³¤¶È", e =>
                {
                    var clip = (asset as AnimationClipAsset).clip;
                    var endFrame = clip.length * TimelineMgr.Ins.FrameRate;
                    asset.EndFrame = asset.StartFrame + Mathf.RoundToInt(endFrame);
                    UpdateView();
                }, e => DropdownMenuAction.Status.Normal);
                this.ShowMenu();
            }
        }



        public void Init(TimelineClipAsset asset, TimelineTrackItem track, TimelineWindow window)
        {
            this.asset = asset;
            this.trackItem = track;
            this.window = window;
            ElementDrag.Add(left, window.clipView, OnLeftDrag);
            ElementDrag.Add(right, window.clipView, OnRightDrag);
            ElementDrag.Add(center, window.clipView, OnStart, OnDrag);
            UpdateView();
        }

        void OnDrag(bool left)
        {
            var frame = window.clipView.GetFrameByMousePosition();

            if (left)
            {
                if (frame < 0)
                {
                    frame = 0;
                }
                if (frame >= asset.EndFrame)
                {
                    frame = asset.EndFrame - 1;
                }
                asset.StartFrame = frame;
            }
            else
            {
                if (frame < asset.StartFrame)
                {
                    frame = asset.StartFrame;
                }

                asset.EndFrame = frame;
            }
            UpdateView();
        }


        void OnLeftDrag(Vector2 e)
        {
            OnDrag(true);
        }
        void OnRightDrag(Vector2 e)
        {
            OnDrag(false);
        }

        int startFrame;
        void OnStart()
        {
            startFrame = window.clipView.GetFrameByMousePosition();
        }
        void OnDrag(Vector2 e)
        {
            var dragFrame = window.clipView.GetFrameByMousePosition();
            var offFrame = dragFrame - startFrame;
            startFrame = dragFrame;
            if (asset.StartFrame + offFrame < 0)
            {
                offFrame = 0 - asset.StartFrame;
            }
            asset.StartFrame += offFrame;
            asset.EndFrame += offFrame;
            UpdateView();
        }



        public void UpdateView()
        {
            var sx = window.clipView.GetPositionByFrame(asset.StartFrame);
            var ex = window.clipView.GetPositionByFrame(asset.EndFrame);
            this.lbType.text = asset.Name;
            this.style.position = new StyleEnum<Position>(Position.Absolute);
            this.style.left = sx;
            this.style.width = ex - sx;
        }
    }
}
