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
                    track.trackName = tName;
                    AddTrackItem(track);
                }

                btnAddTrack.menu.AppendAction(temName, TrackMenuAction, TrackMenuFun, ta);
            }
            
        }


        void AddTrackItem(TimelineTrackAsset trackAsset)
        {
            if (TimelineEditor.Asset == null)
            {
                return;
            }
            if (trackItemDic.ContainsKey(trackAsset.trackName))
            {
                //TODO 改名
                return;
            }
            TimelineEditor.Asset.tracks.Add(trackAsset);
            TimelineEditor.SaveAssets();

            var item = new TimelineTrackItem(trackAsset);
            trackContent.Add(item);
            trackItemDic.Add(trackAsset.trackName, item);
            TimelineEditor.RefreshEntity();
        }
        void RemoveTrackItem(TimelineTrackAsset trackAsset)
        {
            if(trackItemDic.TryGetValue(trackAsset.trackName,out var item))
            {
                item.Release();
                trackContent.Remove(item);
                trackItemDic.Remove(trackAsset.trackName);
                TimelineEditor.RefreshEntity();
            }
        }
        public void RefreshView()
        {
            trackContent.Clear();
            trackItemDic.Clear();
            if (TimelineEditor.Asset!=null)
            {                
                foreach (var track in TimelineEditor.Asset.tracks)
                {
                    var item = new TimelineTrackItem(track);
                    trackContent.Add(item);
                    trackItemDic.Add(track.trackName, item);
                }
            }            
        }
    }
}
