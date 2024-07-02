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
        Label lbType;
        VisualElement right;
        public DropdownMenu menu { get; }
        public TimelineClipItem()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Timeline/TimelineClipItem.uxml");
            visualTree.CloneTree(this);
            content = this.Q<VisualElement>("content");
            left = this.Q<VisualElement>("left");

            lbType = this.Q<Label>("lbType");
            right = this.Q<VisualElement>("right");

            RegisterCallback<PointerDownEvent>(OnPointerDown);
            menu = new DropdownMenu();

        }
        public void OnGUI()
        {
            var pos = this.WorldToLocal(Event.current.mousePosition);
            if (pos.x > 0 && pos.y > 0 && pos.x < style.width.value.value || pos.y < style.height.value.value)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                if ((Event.current.type == UnityEngine.EventType.DragUpdated || Event.current.type == UnityEngine.EventType.DragExited))
                {
                    //改变鼠标的外表  
                    DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                    if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
                    {
                        string retPath = DragAndDrop.paths[0];
                        Log.Debug(retPath);
                    }
                }
            }            
        }
        void OnPointerDown(PointerDownEvent e)
        {
            if (e.button == 0)
            {

            }
            else if (e.button == 1)
            {
                menu.AppendAction("修正长度", e =>
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
            ElementDrag.Add(left, window.clipView, OnSizeStart, OnLeftDrag, OnSizeEnd);
            ElementDrag.Add(right, window.clipView, OnSizeStart, OnRightDrag, OnSizeEnd);
            ElementDrag.Add(content, window.clipView, OnStart, OnDrag, OnEnd);
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
        void OnSizeStart()
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Link;
        }
        void OnSizeEnd()
        {

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
        void OnEnd()
        {
            startFrame = -1;
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
            this.style.position = new StyleEnum<Position>(Position.Absolute);
            this.style.left = sx;
            this.style.width = ex - sx;
        }
    }
}
