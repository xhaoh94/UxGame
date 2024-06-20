using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Ux;
using Ux.Editor;

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

    static GameObject lastObject;
    static TimelineAsset lastTimeline;
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


    public TimelineClipView clipView;
    public TimelineTrackView trackView;
    #endregion


    EntityEmpty entity;
    public TimelineAsset asset;
    public void SaveAssets()
    {
        if (asset != null)
        {
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }
    }

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

        clipView = root.Q<TimelineClipView>("clipView");

        if (lastObject != null)
        {
            ofEntity.SetValueWithoutNotify(lastObject);
        }
        if (lastTimeline != null)
        {
            asset = lastTimeline;
            ofTimeline.SetValueWithoutNotify(lastTimeline);
        }
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
            lastObject = gameObject;
            if (entity == null)
            {
                entity = Entity.Create<EntityEmpty>();
                entity.AddComponent<TimelineComponent>().Init(gameObject);
            }
            else if (entity.GetComponent<TimelineComponent>().GameObject == gameObject)
            {
                return;
            }
            else
            {
                entity.GetComponent<TimelineComponent>().Init(gameObject);
            }
            RefreshEntity();
        }
        else
        {
            if (entity != null)
            {
                entity.Destroy();
                entity = null;
            }
            //RefreshView();
        }
    }
    void OnTimelineChanged(ChangeEvent<UnityEngine.Object> e)
    {
        if (e.newValue is TimelineAsset asset)
        {
            lastTimeline = asset;
            this.asset= asset;
            RefreshEntity();
            //RefreshView();
        }
    }

    public void RefreshEntity()
    {
        entity.GetComponent<TimelineComponent>().SetTimeline(asset);        
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

