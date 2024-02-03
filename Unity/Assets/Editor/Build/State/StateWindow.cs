using System;
using System.Collections.Generic;
using System.IO;
using UI.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Ux;

public class StateWindow : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;

    [MenuItem("UxGame/构建/状态机", false, 520)]
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

    TextField _txtPath;
    TextField _txtNs;

    TextField _txtClass;
    TextField _txtName;
    TextField _txtDesc;
    EnumField _viewType;
    ObjectField _viewAnim;
    ObjectField _viewTimeline;
    VisualElement _content;

    Button _btnCreate;

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

            _txtClass = root.Q<TextField>("txtClass");
            _txtClass.RegisterValueChangedCallback(evt =>
            {
                SelectItem.ClsName = evt.newValue;
            });


            _txtPath = root.Q<TextField>("txtPath");
            _txtPath.RegisterValueChangedCallback(evt =>
            {
                Setting.path = evt.newValue;
            });
            var btnCodePath = root.Q<Button>("btnCodePath");
            btnCodePath.clicked += OnBtnCodeGenPathClick;

            _txtNs = root.Q<TextField>("txtNs");
            _txtNs.RegisterValueChangedCallback(evt =>
            {
                Setting.ns = evt.newValue;
            });

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


            _btnCreate = root.Q<Button>("btnCreate");
            _btnCreate.clicked += OnBtnCreate;

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
    void OnBtnCodeGenPathClick()
    {
        var temPath = EditorUtility.OpenFolderPanel("请选择生成路径", Setting.path, "");
        if (temPath.Length == 0)
        {
            return;
        }

        if (!Directory.Exists(temPath))
        {
            EditorUtility.DisplayDialog("错误", "路径不存在!", "ok");
            return;
        }
        Setting.path = temPath;
        _txtPath.SetValueWithoutNotify(temPath);
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
        _txtPath.SetValueWithoutNotify(Setting.path);
        _txtNs.SetValueWithoutNotify(Setting.ns);
        if (SelectItem == null)
        {
            _infoView.style.display = DisplayStyle.None;
            return;
        }
        _infoView.style.display = DisplayStyle.Flex;
        _txtClass.SetValueWithoutNotify(SelectItem.ClsName);
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
        var item = new StateSettingData.StateData();
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

    void OnBtnCreate()
    {
        var data = SelectItem;
        if (data == null) return;
        if (string.IsNullOrEmpty(data.ClsName)) return;
        var write = new WriteData();
        write.Writeln(@"//自动生成的代码，请勿修改!!!");
        write.Writeln("using System.Collections.Generic;");
        write.Writeln($"namespace {Setting.ns}");
        write.StartBlock();
        var resName = string.Empty;
        switch (data.ViewType)
        {
            case StateViewType.None:
                write.Writeln($"public partial class {data.ClsName} : {nameof(UnitStateBase)}");
                break;
            case StateViewType.Anim:
                write.Writeln($"public partial class {data.ClsName} : {nameof(UnitStateAnim)}");
                resName = Path.GetFileNameWithoutExtension(data.AnimName);
                break;
            case StateViewType.Timeline:
                write.Writeln($"public partial class {data.ClsName} : {nameof(UnitStateTimeLine)}");
                resName = Path.GetFileNameWithoutExtension(data.TimeLineName);
                break;
        }
        write.StartBlock();
        write.Writeln($"public override string Name => \"{data.StateName}\";");
        if (!string.IsNullOrEmpty(resName))
        {
            write.Writeln($"public override string ResName => \"{resName}\";");
        }
        write.Writeln("public override List<StateConditionBase> Conditions { get; } = new List<StateConditionBase>()");
        write.StartBlock();

        foreach (var condition in data.Conditions)
        {
            switch (condition.Type)
            {
                case StateConditionBase.Type.State:
                    if (condition.stateType == StateConditionBase.State.Any)
                    {
                        write.Writeln($"new {nameof(StateCondition)}(StateConditionBase.State.Any, null),");
                    }
                    else
                    {
                        write.Writeln($"new {nameof(StateCondition)}(StateConditionBase.State.{condition.stateType}, new HashSet<string>");
                        write.StartBlock();
                        foreach (var state in condition.states)
                        {
                            write.Writeln($"\"{state}\",");
                        }
                        write.EndBlock(false);
                        write.Writeln("),", false);
                    }
                    break;
                case StateConditionBase.Type.TempBoolVar:
                    write.Writeln($"new {nameof(TemBoolVarCondition)}({condition.key}, {condition.value}),");
                    break;
                case StateConditionBase.Type.Action_Move:
                    write.Writeln($"new {nameof(ActionMoveCondition)}(),");
                    break;
                case StateConditionBase.Type.Action_Keyboard:
                    write.Writeln($"new {nameof(ActionKeyboardCondition)}(UnityEngine.InputSystem.Key.{condition.keyType},StateConditionBase.Trigger.{condition.triggerType}),");
                    break;
                case StateConditionBase.Type.Action_Input:
                    write.Writeln($"new {nameof(ActionInputCondition)}(StateConditionBase.Input.{condition.inputType},StateConditionBase.Trigger.{condition.triggerType}),");
                    break;
            }

        }
        write.EndBlock(false);
        write.Writeln(";", false);
        write.EndBlock();
        write.EndBlock();
        write.Export($"{Setting.path}/", data.ClsName);
    }
    void OnBtnAddState()
    {
        var element = MakeConfitionItem();
        var data = new StateSettingData.StateCondition();
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
    private void BindConditionItem(StateConditionContent element, int index, StateSettingData.StateCondition condition)
    {
        var btn = element.Q<Button>("Sub");
        btn.clicked += () =>
        {
            _content.RemoveAt(index);
            SelectItem.Conditions.RemoveAt(index);
        };
        element.SetData(condition);
    }

    StateSettingData.StateData SelectItem
    {
        get
        {
            var selectItem = _listView.selectedItem as StateSettingData.StateData;
            return selectItem;
        }
    }

    private void OnDestroy()
    {
        SaveConfig();
        AssetDatabase.Refresh();
    }

    #region 初始化   
    void LoadConfig()
    {
        Setting = SettingTools.GetSingletonAssets<StateSettingData>("Assets/Setting/Build/State");
    }
    void SaveConfig()
    {
        Setting?.SaveFile();
    }

    #endregion
}
