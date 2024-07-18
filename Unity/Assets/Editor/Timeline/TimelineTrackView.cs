using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using YooAsset.Editor;
namespace Ux.Editor.Timeline
{
    public partial class TimelineTrackView : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<TimelineTrackView, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            public UxmlTraits()
            {
                base.focusIndex.defaultValue = 0;
                base.focusable.defaultValue = true;
            }
        }        

        Dictionary<string, TimelineTrackItem> trackItemDic = new Dictionary<string, TimelineTrackItem>();
        public TimelineTrackView()
        {
            CreateChildren();            
            Add(root);
            var trackAssets = EditorTools.GetAssignableTypes(typeof(TimelineTrackAsset));
            foreach (var ta in trackAssets)
            {
                var temName = ta.Name.Substring(0, ta.Name.Length - 5);
                if (temName.EndsWith("Track"))
                {
                    temName = temName.Substring(0, temName.Length - 5) + " Track";
                }
                else
                {
                    temName += " Track";
                }
                DropdownMenuAction.Status TrackMenuFun(DropdownMenuAction action)
                {
                    return DropdownMenuAction.Status.Normal;
                }
                void TrackMenuAction(DropdownMenuAction action)
                {
                    var trackType = (System.Type)action.userData;
                    var track = Activator.CreateInstance(trackType) as TimelineTrackAsset;

                    var tName = trackType.Name.Substring(0, trackType.Name.Length - 5);
                    if (tName.EndsWith("Track"))
                    {
                        tName = temName.Substring(0, tName.Length - 5);
                    }
                    track.Name = tName;
                    AddTrackItem(track);
                }

                btnAddTrack.menu.AppendAction(temName, TrackMenuAction, TrackMenuFun, ta);
            }
            
        }


        void AddTrackItem(TimelineTrackAsset trackAsset)
        {
            if (Timeline.Asset == null)
            {
                return;
            }
            if (trackItemDic.ContainsKey(trackAsset.Name))
            {
                //TODO 改名
                return;
            }
            Timeline.Asset.tracks.Add(trackAsset);
            Timeline.SaveAssets();

            var item = new TimelineTrackItem(trackAsset);
            trackContent.Add(item);
            trackItemDic.Add(trackAsset.Name, item);
            Timeline.RefreshEntity();
        }
        void RemoveTrackItem(TimelineTrackAsset trackAsset)
        {
            if(trackItemDic.TryGetValue(trackAsset.Name,out var item))
            {
                item.Release();
                trackContent.Remove(item);
                trackItemDic.Remove(trackAsset.Name);
                Timeline.RefreshEntity();
            }
        }
        public void RefreshView()
        {
            trackContent.Clear();
            trackItemDic.Clear();
            if (Timeline.Asset!=null)
            {
                foreach (var track in Timeline.Asset.tracks)
                {
                    var item = new TimelineTrackItem(track);
                    trackContent.Add(item);
                    trackItemDic.Add(track.Name, item);
                }
            }            
        }
    }
}
