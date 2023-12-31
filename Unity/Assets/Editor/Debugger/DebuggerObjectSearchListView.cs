using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
public interface IDebuggerListItem<T>
{
    void SetClickEvt(Action<T> action);
    void SetData(T data);
}
public class DebuggerObjectSearchListView<T, V> where T : TemplateContainer, IDebuggerListItem<V>, new()
{
    VisualElement root;
    private int tPage = 1;
    private int curPage = 1;
    private int pageMaxCnt = 5;
    Dictionary<string, V> dict;

    private TextField _inputSearch;
    private Button _btnClear;
    private VisualElement _vePage;
    private IntegerField _inputPage;
    private Label _txtPage;
    private Button _btnLast;
    private Button _btnNext;
    private ListView _list;

    public DebuggerObjectSearchListView(VisualElement root, int pageMaxCnt = 5)
    {
        try
        {
            this.root = root;
            this.pageMaxCnt = pageMaxCnt;
            _inputSearch = root.Q<TextField>("inputSearch");
            _inputSearch.RegisterValueChangedCallback(_OnSearch);
            _btnClear = root.Q<Button>("btnClear");
            if (_btnClear != null)
            {
                _btnClear.clicked += _OnBtnClear;
            }


            _vePage = root.Q<VisualElement>("vePage");

            _inputPage = root.Q<IntegerField>("inputPage");
            _inputPage.RegisterValueChangedCallback(_OnPageChange);
            _txtPage = root.Q<Label>("txtPage");

            _btnLast = root.Q<Button>("btnLast");
            _btnLast.clicked += _OnBtnLast;
            _btnNext = root.Q<Button>("btnNext");
            _btnNext.clicked += _OnBtnNext;

            _list = root.Q<ListView>("list");
            _list.makeItem = _MakeListItem;
            _list.bindItem = _BindListItem;
        }
        catch (Exception e)
        {
            Log.Error(e.Message);
        }
    }
    public void SetVisable(bool v)
    {
        root.style.display = v ? DisplayStyle.Flex : DisplayStyle.None;
    }

    public void SetData(Dictionary<string, V> dict)
    {
        this.dict = dict;
        _OnSearch(null);
    }
    void _OnPageChange(ChangeEvent<int> e)
    {
        if (e.newValue < 1 || e.newValue > tPage)
        {
            _inputPage.SetValueWithoutNotify(curPage);
            return;
        }
        curPage = e.newValue;
        _OnSearch(null);
    }

    void _OnSearch(ChangeEvent<string> e)
    {
        FilterViewItems(dict);
        _list.Clear();
        _list.ClearSelection();
        _list.itemsSource = _listData;
        _list.RefreshItems();
    }
    void _OnBtnClear()
    {
        Search(string.Empty);
    }
    void _OnBtnLast()
    {
        if (curPage > 1)
        {
            curPage--;
            _OnSearch(null);
        }
    }
    void _OnBtnNext()
    {
        if (curPage < tPage)
        {
            curPage++;
            _OnSearch(null);
        }
    }

    public void Search(string text)
    {
        if (_inputSearch.text == text) return;
        _inputSearch.SetValueWithoutNotify(text);
        _OnSearch(null);
    }

    List<V> _listData;
    private void FilterViewItems(Dictionary<string, V> dict)
    {
        _listData = new List<V>();
        var searchKey = _inputSearch.text.ToLower();
        if (searchKey == string.Empty)
        {
            _listData = dict.Values.ToList();
        }
        else
        {
            foreach (var kv in dict)
            {
                if (kv.Key.ToLower().IndexOf(searchKey) >= 0)
                {
                    _listData.Add(kv.Value);
                }
            }
        }
        if (_listData.Count <= pageMaxCnt)
        {
            _vePage.style.display = DisplayStyle.None;
            return;
        }
        _vePage.style.display = DisplayStyle.Flex;
        tPage = Mathf.CeilToInt((float)_listData.Count / pageMaxCnt);
        _txtPage.text = $"/{tPage}";
        if (curPage > tPage) curPage = tPage;
        _btnLast.visible = curPage > 1;
        _btnNext.visible = curPage < tPage;
        _inputPage.SetValueWithoutNotify(curPage);
        var starIndex = (curPage - 1) * pageMaxCnt;
        var endIndex = curPage * pageMaxCnt;
        if (endIndex > _listData.Count) endIndex = _listData.Count;
        _listData = _listData.Skip(starIndex).Take(endIndex - starIndex).ToList();
    }
    VisualElement _MakeListItem()
    {
        return new T();
    }
    void _BindListItem(VisualElement element, int index)
    {
        var data = _listData[index];
        (element as IDebuggerListItem<V>).SetData(data);
    }
}