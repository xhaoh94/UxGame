using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Ux;

public class StateWindow : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;

    [MenuItem("UxGame/×´Ì¬»ú")]
    public static void ShowExample()
    {
        StateWindow wnd = GetWindow<StateWindow>();
        wnd.titleContent = new GUIContent("StateWindow");
    }

    private StateSettingData Setting;
    private int _lastModifyExportIndex = 0;
    ListView _listView;
    Button _btnAdd;
    Button _btnRemove;

    VisualElement _infoView;

    TextField _txtName;
    TextField _txtDesc;
    EnumField _viewType;
    ObjectField _viewAnim;
    ObjectField _viewTimeline;
    VisualElement _content;

    Button _btnAddCondition;

    public void CreateGUI()
    {
        try
        {
            LoadConfig();
            VisualElement root = rootVisualElement;
            m_VisualTreeAsset.CloneTree(root);

            _listView = root.Q<ListView>("listView");
            _listView.makeItem = MakeListViewItem;
            _listView.bindItem = BindListViewItem;
#if UNITY_2022_1_OR_NEWER
            _listView.selectionChanged += OnListViewSelectionChange;
#else
            _listView.onSelectionChange += OnListViewSelectionChange;
#endif

            _btnAdd = root.Q<Button>("btnAdd");
            _btnAdd.clicked += OnBtnAddClick;
            _btnRemove = root.Q<Button>("btnRemove");
            _btnRemove.clicked += OnBtnRemoveClick;

            _infoView = root.Q<VisualElement>("infoView");

            _txtName = root.Q<TextField>("txtName");
            _txtName.RegisterValueChangedCallback(evt =>
            {
                SelectItem.StateName = evt.newValue;
                OnUpdateListView();
            });
            _txtDesc = root.Q<TextField>("txtDesc");
            _txtDesc.RegisterValueChangedCallback(evt =>
            {
                SelectItem.Desc = evt.newValue;
                OnUpdateListView();
            });
            _viewType = root.Q<EnumField>("viewType");
            _viewType.Init(StateViewType.None);
            _viewType.RegisterValueChangedCallback(evt =>
            {
                SelectItem.ViewType = (StateViewType)evt.newValue;
                RefreshElement();
            });
            _viewAnim = root.Q<ObjectField>("viewAnim");
            _viewAnim.RegisterValueChangedCallback(evt =>
            {
                var path = AssetDatabase.GetAssetPath(evt.newValue);
                SelectItem.AnimName = path;
            });
            _viewTimeline = root.Q<ObjectField>("viewTimeline");
            _viewTimeline.RegisterValueChangedCallback(evt =>
            {
                var path = AssetDatabase.GetAssetPath(evt.newValue);
                SelectItem.TimeLineName = path;
            });


            _btnAddCondition = root.Q<Button>("btnAddCondition");
            _btnAddCondition.clicked += OnBtnAddState;

            _content = root.Q<VisualElement>("content");

            _lastModifyExportIndex = 0;
            OnUpdateListView();
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }
    }

    private VisualElement MakeListViewItem()
    {
        VisualElement element = new VisualElement();
        {
            var label = new Label();
            label.name = "Label1";
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            label.style.flexGrow = 1f;
            label.style.height = 20f;
            element.Add(label);
        }

        return element;
    }
    private void BindListViewItem(VisualElement element, int index)
    {
        var setting = Setting.StateSettings[index];

        var textField1 = element.Q<Label>("Label1");
        textField1.text = setting.StateName;
        if (!string.IsNullOrEmpty(setting.Desc))
        {
            if (string.IsNullOrEmpty(setting.StateName))
            {
                textField1.text = setting.Desc;
            }
            else
            {
                textField1.text += "@" + setting.Desc;
            }
        }
    }

    private void OnListViewSelectionChange(IEnumerable<object> objs)
    {
        if (_listView.selectedIndex < 0)
        {
            return;
        }
        _lastModifyExportIndex = _listView.selectedIndex;
        RefreshView();
    }
    void RefreshView()
    {
        if (SelectItem == null)
        {
            _infoView.style.display = DisplayStyle.None;
            return;
        }
        _infoView.style.display = DisplayStyle.Flex;

        _viewType.SetValueWithoutNotify(SelectItem.ViewType);
        _txtName.SetValueWithoutNotify(SelectItem.StateName);
        _txtDesc.SetValueWithoutNotify(SelectItem.Desc);
        _viewAnim.SetValueWithoutNotify(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(SelectItem.AnimName));
        _viewTimeline.SetValueWithoutNotify(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(SelectItem.TimeLineName));
        RefreshElement();
        RefreshCondition();
    }
    void RefreshElement()
    {
        switch (SelectItem.ViewType)
        {
            case StateViewType.None:
                _viewAnim.style.display = DisplayStyle.None;
                _viewTimeline.style.display = DisplayStyle.None;
                break;
            case StateViewType.Anim:
                _viewAnim.style.display = DisplayStyle.Flex;
                _viewTimeline.style.display = DisplayStyle.None;
                break;
            case StateViewType.Timeline:
                _viewAnim.style.display = DisplayStyle.None;
                _viewTimeline.style.display = DisplayStyle.Flex;
                break;
        }
    }

    private void OnUpdateListView()
    {
        _listView.Clear();
        _listView.ClearSelection();
        _listView.itemsSource = Setting.StateSettings;
        _listView.Rebuild();
        if (Setting.StateSettings.Count > 0)
        {
            if (_lastModifyExportIndex >= 0)
            {
                if (_lastModifyExportIndex >= _listView.itemsSource.Count)
                {
                    _lastModifyExportIndex = 0;
                }
                _listView.selectedIndex = _lastModifyExportIndex;
            }
        }
        else
        {
            RefreshView();
        }
    }

    void OnBtnAddClick()
    {
        var item = new StateData();
        Setting.StateSettings.Add(item);
        OnUpdateListView();
    }
    void OnBtnRemoveClick()
    {
        if (SelectItem == null)
        {
            return;
        }
        Setting.StateSettings.Remove(SelectItem);
        OnUpdateListView();
    }


    void OnBtnAddState()
    {
        var element = MakeConfitionItem();
        var data = new StateCondition();
        SelectItem.Conditions.Add(data);
        BindConditionItem(element, _content.childCount, data);
        _content.Add(element);
    }
    void RefreshCondition()
    {
        _content.Clear();
        for (int i = 0; i < SelectItem.Conditions.Count; i++)
        {
            var element = MakeConfitionItem();
            BindConditionItem(element, i, SelectItem.Conditions[i]);
            _content.Add(element);
        }
    }

    private StateConditionContent MakeConfitionItem()
    {
        var element = new StateConditionContent();
        return element;
    }
    private void BindConditionItem(StateConditionContent element, int index, StateCondition condition)
    {
        var btn = element.Q<Button>("Sub");
        btn.clicked += () =>
        {
            _content.RemoveAt(index);
            SelectItem.Conditions.RemoveAt(index);
        };
        element.SetData(condition);
    }

    StateData SelectItem
    {
        get
        {
            var selectItem = _listView.selectedItem as StateData;
            return selectItem;
        }
    }

    private void OnDestroy()
    {
        SaveConfig();
    }

    #region ³õÊ¼»¯   
    void LoadConfig()
    {
        Setting = SettingTools.GetSingletonAssets<StateSettingData>("Assets/Setting/State");
    }
    void SaveConfig()
    {
        Setting?.SaveFile();
    }

    #endregion
}
