using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
namespace Ux.Editor.Timeline
{
    public class TimelineClipItem : VisualElement, IToolbarMenuElement
    {
        Color color;
        public TimelineClipAsset Asset { get; private set; }
        public TrackClipContent ClipContent { get; private set; }
        VisualElement content;
        VisualElement left;
        VisualElement center;
        VisualElement right;
        Label lbType;
        public DropdownMenu menu { get; }
        public TimelineClipItem()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Timeline/Uxml/TimelineClipItem.uxml");
            visualTree.CloneTree(this);
            content = this.Q<VisualElement>("content");

            left = this.Q<VisualElement>("left");
            center = this.Q<VisualElement>("center");
            right = this.Q<VisualElement>("right");
            lbType = this.Q<Label>("lbType");

            menu = new DropdownMenu();
            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<DragUpdatedEvent>(_OnDragUpd);
            RegisterCallback<DragPerformEvent>(_OnDragPerform);
            style.position = new StyleEnum<Position>(Position.Absolute);
            style.height = 30;
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
                (Asset as AnimationClipAsset).clip = clip;
                Timeline.SaveAssets();
                Timeline.RefreshEntity();
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
                    var clip = (Asset as AnimationClipAsset).clip;
                    var endFrame = clip.length * TimelineMgr.Ins.FrameRate;
                    Asset.EndFrame = Asset.StartFrame + Mathf.RoundToInt(endFrame);
                    UpdateView();
                }, e => DropdownMenuAction.Status.Normal);
                this.ShowMenu();
            }
        }



        public void Init(TimelineClipAsset asset, TrackClipContent track)
        {
            Asset = asset;
            ClipContent = track;
            color = ClipContent.TrackItem.Asset.GetType().GetAttribute<TLTrackAttribute>().Color;
            ElementDrag.Add(left, Timeline.ClipContent, OnStart, OnLeftDrag, OnEnd);
            ElementDrag.Add(right, Timeline.ClipContent, OnStart, OnRightDrag, OnEnd);
            ElementDrag.Add(center, Timeline.ClipContent, OnStart, OnDrag, OnEnd);
            UpdateView();
        }


        void OnDrag(bool left)
        {
            var frame = Timeline.GetFrameByMousePosition();

            if (left)
            {
                if (frame < 0)
                {
                    frame = 0;
                }
                if (frame >= Asset.EndFrame)
                {
                    frame = Asset.EndFrame - 1;
                }
                Asset.StartFrame = frame;
            }
            else
            {
                if (frame < Asset.StartFrame)
                {
                    frame = Asset.StartFrame;
                }

                Asset.EndFrame = frame;
            }
            UpdateView();
            ClipContent.ClipMarkDirtyRepaint();
        }


        void OnLeftDrag(Vector2 e)
        {
            OnDrag(true);
        }
        void OnRightDrag(Vector2 e)
        {
            OnDrag(false);
        }

        bool isDrag;
        int lastFrame;
        int startFrame;
        int endFrame;
        void OnStart()
        {
            lastFrame = Timeline.GetFrameByMousePosition();
            startFrame = Asset.StartFrame;
            endFrame = Asset.EndFrame;
            isDrag = true;
        }
        void OnEnd()
        {
            isDrag = false;
            if (!ClipContent.IsValid())
            {
                Asset.StartFrame = startFrame;
                Asset.EndFrame = endFrame;
            }
            UpdateView();
        }
        void OnDrag(Vector2 e)
        {
            var dragFrame = Timeline.GetFrameByMousePosition();
            var offFrame = dragFrame - lastFrame;
            lastFrame = dragFrame;
            if (Asset.StartFrame + offFrame < 0)
            {
                offFrame = 0 - Asset.StartFrame;
            }
            Asset.StartFrame += offFrame;
            Asset.EndFrame += offFrame;
            UpdateView();
            ClipContent.ClipMarkDirtyRepaint();
        }


        public void UpdateView()
        {
            var sx = Timeline.GetPositionByFrame(Asset.StartFrame);
            var ex = Timeline.GetPositionByFrame(Asset.EndFrame);
            var tFrame = (float)(Asset.EndFrame - Asset.StartFrame);
            lbType.text = Asset.Name;
            var width = ex - sx;
            lbType.style.left =  width * ((Asset.InFrame - Asset.StartFrame) / tFrame);
            lbType.style.right = width - (width * ((Asset.OutFrame - Asset.StartFrame) / tFrame));
            style.left = sx;
            style.width = width;

            if (ClipContent.IsValid())
            {
                //content.style.borderLeftWidth = 1;
                //content.style.borderRightWidth = 1;
                //content.style.borderTopWidth = 1;
                content.style.borderBottomWidth = 3;

                content.style.borderLeftColor = isDrag ? Color.white : color;
                content.style.borderRightColor = isDrag ? Color.white : color;
                content.style.borderTopColor = isDrag ? Color.white : color;
                content.style.borderBottomColor = isDrag ? Color.white : color;
            }
            else
            {
                //content.style.borderLeftWidth = 1;
                //content.style.borderRightWidth = 1;
                //content.style.borderTopWidth = 1;
                content.style.borderBottomWidth = 3;

                //content.style.borderLeftColor = new StyleColor(Color.red);
                //content.style.borderRightColor = new StyleColor(Color.red);
                //content.style.borderTopColor = new StyleColor(Color.red);
                content.style.borderBottomColor = new StyleColor(Color.red);
            }
            ClipContent.UpdateClipData();
        }
    }
}
