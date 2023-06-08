using UnityEditor;
using UnityEngine.UIElements;
using Ux;

public class UIDebuggerItem : TemplateContainer, IDebuggerListItem<IUIData>
{
    private VisualTreeAsset _visualAsset;


    Label _txtIDStr;
    //TextField _txtID;
    TextField _txtType;
    TextField _txtPkgs;
    TextField _txtTags;
    TextField _txtChildrens;
    TextField _txtParID;
    TextField _txtParRedPoint;
    TextField _txtParTitle;

    public UIDebuggerItem()
    {
        // 加载布局文件		
        _visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Debugger/UI/UIDebuggerItem.uxml");
        if (_visualAsset == null)
            return;

        var _root = _visualAsset.CloneTree();
        _root.style.flexGrow = 1f;
        style.flexGrow = 1f;
        Add(_root);
        CreateView();
    }

    /// <summary>
    /// 初始化页面
    /// </summary>
    void CreateView()
    {
        _txtIDStr = this.Q<Label>("txtIDStr");
        //_txtID = this.Q<TextField>("txtID");
        _txtType = this.Q<TextField>("txtType");
        _txtPkgs = this.Q<TextField>("txtPkgs");
        _txtTags = this.Q<TextField>("txtTags");
        _txtChildrens = this.Q<TextField>("txtChildrens");
        _txtParID = this.Q<TextField>("txtParID");
        _txtParRedPoint = this.Q<TextField>("txtParRedPoint");
        _txtParTitle = this.Q<TextField>("txtParTitle");
    }

    public void SetData(IUIData data)
    {
        var zs = data.IDStr.Split("_");
        _txtIDStr.text = $"{zs[1]}";
        //_txtID.SetValueWithoutNotify(zs[1]);
        _txtType.SetValueWithoutNotify(data.CType.FullName);


        if (data.Pkgs != null && data.Pkgs.Length > 0)
        {
            _txtPkgs.style.display = DisplayStyle.Flex;
            _txtPkgs.SetValueWithoutNotify(string.Join(",", data.Pkgs));
        }
        else
        {
            _txtPkgs.style.display = DisplayStyle.None;
        }

        if (data.Lazyloads != null && data.Lazyloads.Length > 0)
        {
            _txtTags.style.display = DisplayStyle.Flex;
            _txtTags.SetValueWithoutNotify(string.Join(",", data.Lazyloads));
        }
        else
        {
            _txtTags.style.display = DisplayStyle.None;
        }

        if (data.Children != null && data.Children.Count > 0)
        {
            _txtChildrens.style.display = DisplayStyle.Flex;
            _txtChildrens.SetValueWithoutNotify(string.Join(",", data.Children));
        }
        else
        {
            _txtChildrens.style.display = DisplayStyle.None;
        }

        if (data.TabData != null)
        {
            _txtParID.style.display = DisplayStyle.Flex;
            _txtParID.SetValueWithoutNotify(data.TabData.PID.ToString());
            if (data.TabData.TagType == null)
            {
                _txtParRedPoint.style.display = DisplayStyle.None;
            }
            else
            {
                _txtParRedPoint.style.display = DisplayStyle.Flex;
                _txtParRedPoint.SetValueWithoutNotify(data.TabData.TagType.FullName);
            }

            var title = data.TabData.TitleStr;
            if (string.IsNullOrEmpty(title))
            {
                _txtParTitle.style.display = DisplayStyle.None;
            }
            else
            {
                _txtParTitle.style.display = DisplayStyle.Flex;
                _txtParTitle.SetValueWithoutNotify(title);
            }
        }
        else
        {
            _txtParID.style.display = DisplayStyle.None;
            _txtParRedPoint.style.display = DisplayStyle.None;
            _txtParTitle.style.display = DisplayStyle.None;
        }
    }
}