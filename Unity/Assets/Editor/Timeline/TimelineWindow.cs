using NUnit.Framework.Internal.Filters;
using System;
using System.IO;
using System.Runtime.Remoting.Activation;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Ux;
using Ux.Editor;
using YooAsset.Editor;

public class TimeLineWindow : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;

    static TimeLineWindow wnd;
    [MenuItem("UxGame/时间轴")]
    public static void ShowExample()
    {
        wnd = GetWindow<TimeLineWindow>();
        wnd.titleContent = new GUIContent("时间轴");
    }
    static string Path = "Assets/Data/Res/Timeline";
    #region 组件
    ObjectField ofEntity;
    ObjectField ofTimeline;
    Button btnCreate;
    VisualElement createView;
    TextField inputPath;
    Button btnPath;
    TextField inputName;
    Button btnOk;

    Button btnPlay;
    Button btnPause;
    ToolbarMenu btnAddTrack;
    VisualElement trackContent;

    public TimelineClipView clipView;
    #endregion

    TimelineEditor entity;
    public TimelineComponent Component => entity?.GetComponent<TimelineComponent>();
    public void CreateGUI()
    {
        VisualElement root = rootVisualElement;
        m_VisualTreeAsset.CloneTree(root);
        ofEntity = root.Q<ObjectField>("ofEntity");
        ofEntity.objectType = typeof(GameObject);
        ofEntity.RegisterValueChangedCallback(OnEntityChanged);
        ofTimeline = root.Q<ObjectField>("ofTimeline");
        ofTimeline.objectType = typeof(TimelineAsset);
        ofTimeline.RegisterValueChangedCallback(OnTimelineChanged);


        btnCreate = root.Q<Button>("btnCreate");
        btnCreate.clicked += OnBtnCreateClick;
        createView = root.Q<VisualElement>("createView");
        createView.style.display = DisplayStyle.None;

        inputPath = root.Q<TextField>("inputPath");
        inputPath.SetValueWithoutNotify(Path);
        btnPath = root.Q<Button>("btnPath");
        btnPath.clicked += OnBtnAssetPathClick;
        inputName = root.Q<TextField>("inputName");
        btnOk = root.Q<Button>("btnOk");
        btnOk.clicked += OnCreateClick;


        btnPlay = root.Q<Button>("btnPlay");
        btnPlay.clicked += OnBtnPlayClick;
        btnPause = root.Q<Button>("btnPause");
        btnPause.clicked += OnBtnPauseClick;
        btnAddTrack = root.Q<ToolbarMenu>("btnAddTrack");
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
                if (Component == null)
                {
                    return;
                }
                var trackType = (System.Type)action.userData;
                var track = Activator.CreateInstance(trackType) as TimelineTrackAsset;

                var tName = trackType.Name.Substring(0, trackType.Name.Length - 5);
                if (tName.EndsWith("Track"))
                {
                    tName = temName.Substring(0, tName.Length - 5);
                }    
                track.Name= tName;
                Component.CurTimeline.Asset.tracks.Add(track);
                Component.SetTimeline(Component.CurTimeline.Asset);
                EditorUtility.SetDirty(Component.CurTimeline.Asset);
                AssetDatabase.SaveAssets();
                UpdateView();
            }

            btnAddTrack.menu.AppendAction(temName, TrackMenuAction, TrackMenuFun, ta);
        }

        trackContent = root.Q<VisualElement>("trackContent");

        clipView = root.Q<TimelineClipView>("clipView");
    }


    private void OnDestroy()
    {
        if (entity != null)
        {
            entity.Destroy();
            entity = null;
        }
    }

    void OnEntityChanged(ChangeEvent<UnityEngine.Object> e)
    {
        if (e.newValue is GameObject gameObject)
        {
            if (entity == null)
            {
                entity = Entity.Create<TimelineEditor>();
                entity.AddComponent<TimelineComponent>(gameObject);
            }
            else if (entity.Go == gameObject)
            {
                return;
            }
            entity.Go = gameObject;
            Component.Init(gameObject);
            if (ofTimeline.value is TimelineAsset asset)
            {
                Component.SetTimeline(asset);
                UpdateView();
            }
        }
        else
        {
            if (entity != null)
            {
                entity.Destroy();
                entity = null;
            }
            UpdateView();
        }
    }
    void OnTimelineChanged(ChangeEvent<UnityEngine.Object> e)
    {
        if (e.newValue is TimelineAsset asset)
        {
            if (Component == null)
            {
                return;
            }
            Component.SetTimeline(asset);
            UpdateView();
        }
    }

    void UpdateView()
    {
        trackContent.Clear();
        if (Component == null)
        {
            return;
        }
        if (Component.CurTimeline == null)
        {
            return;
        }
        foreach (var track in Component.CurTimeline.Asset.tracks)
        {
            var item = new TimelineTrackItem(track, this);
            trackContent.Add(item);
        }
    }
    void OnBtnCreateClick()
    {
        if (createView.style.display == DisplayStyle.None)
        {
            createView.style.display = DisplayStyle.Flex;
        }
        else
        {
            createView.style.display = DisplayStyle.None;
        }
    }
    void OnBtnAssetPathClick()
    {
        var temPath = EditorUtility.OpenFolderPanel("请选择生成路径", Path, "");
        if (temPath.Length == 0)
        {
            return;
        }

        if (!Directory.Exists(temPath))
        {
            EditorUtility.DisplayDialog("错误", "路径不存在!", "ok");
            return;
        }
        inputPath.SetValueWithoutNotify(temPath);
    }
    void OnCreateClick()
    {
        if (string.IsNullOrEmpty(inputName.text))
        {
            Log.Error("名字不能为空");
            return;
        }
        var assetName = $"{inputPath.text}/{inputName.text}.asset";
        if (File.Exists(assetName))
        {
            Log.Error("重复创建相同的TimelineAsset");
            return;
        }

        createView.style.display = DisplayStyle.None;
        var asset = ScriptableObject.CreateInstance<TimelineAsset>();
        AssetDatabase.CreateAsset(asset, assetName);
        ofTimeline.SetValueWithoutNotify(asset);
    }
    void OnBtnPlayClick()
    {

    }
    void OnBtnPauseClick()
    {

    }


}

