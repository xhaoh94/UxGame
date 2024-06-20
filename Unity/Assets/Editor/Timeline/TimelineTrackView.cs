using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Ux;
using YooAsset.Editor;

public class TimelineTrackView : VisualElement
{
    public TimeLineWindow window;
    ToolbarMenu btnAddTrack;
    VisualElement trackContent;

    Dictionary<string, TimelineTrackItem> trackItemDic = new Dictionary<string, TimelineTrackItem>();
    public TimelineTrackView()
    {
        //btnAddTrack = root.Q<ToolbarMenu>("btnAddTrack");

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

        //trackContent = root.Q<VisualElement>("trackContent");
    }

    void AddTrackItem(TimelineTrackAsset trackAsset)
    {
        if (window.asset == null)
        {
            return;
        }
        if (trackItemDic.ContainsKey(trackAsset.Name))
        {
            //TODO 改名
            return;
        }
        window.asset.tracks.Add(trackAsset);
        window.SaveAssets();

        var item = new TimelineTrackItem(trackAsset, window);
        trackContent.Add(item);
        trackItemDic.Add(trackAsset.Name, item);
        window.RefreshEntity();
    }
    void RemoveTrackItem(TimelineTrackAsset trackAsset)
    {

    }
    void RefreshView()
    {
        trackContent.Clear();
        foreach (var track in window.asset.tracks)
        {
            var item = new TimelineTrackItem(track, window);
            trackContent.Add(item);
        }
    }
}