using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using Ux;
using static Ux.StateSettingData;

partial class StateWindow
{
    private int _lastModifyExportIndex = 0;
    TextField _inputSearch;
    Button _btnSearchClear;
    ListView _listView;
    Button _btnAdd;
    Button _btnRemove;

    void OnCreateListView()
    {
        VisualElement root = rootVisualElement;

        _inputSearch = root.Q<TextField>("inputSearch");
        _inputSearch.RegisterValueChangedCallback(e =>
        {
            OnUpdateListView();
        });
        _btnSearchClear = root.Q<Button>("btnSearchClear");
        _btnSearchClear.clicked += () =>
        {
            _inputSearch.SetValueWithoutNotify(string.Empty);
            OnUpdateListView();
        };

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

        _lastModifyExportIndex = 0;
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


    private void OnUpdateListView()
    {
        _listView.Clear();
        _listView.ClearSelection();
        List<StateData> datas = new List<StateData>();
        if (!string.IsNullOrEmpty(_inputSearch.text))
        {
            foreach (var state in Setting.StateSettings)
            {
                if (state.Group == _inputSearch.text)
                {
                    datas.Add(state);
                }
            }
        }
        else
        {
            datas.AddRange(Setting.StateSettings);
        }

        _listView.itemsSource = datas;
        _listView.Rebuild();
        if (datas.Count > 0)
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
}
