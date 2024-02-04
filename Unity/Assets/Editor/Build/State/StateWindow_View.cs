using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Ux;

partial class StateWindow
{
    VisualElement _infoView;

    TextField _txtGroup;

    TextField _txtPath;
    TextField _txtNs;


    IntegerField _txtPri;
    Toggle _tgMute;
    TextField _txtClass;
    TextField _txtName;
    TextField _txtDesc;
    EnumField _viewType;
    ObjectField _viewAnim;
    ObjectField _viewTimeline;
    void OnCreateView()
    {
        VisualElement root = rootVisualElement;

        _infoView = root.Q<VisualElement>("infoView");

        _txtGroup = root.Q<TextField>("txtGroup");
        _txtGroup.RegisterValueChangedCallback(e =>
        {
            SelectItem.Group = e.newValue;
        });

        _txtPri = root.Q<IntegerField>("txtPri");
        _txtPri.RegisterValueChangedCallback(evt =>
        {
            SelectItem.Pri = evt.newValue;
        });

        _tgMute = root.Q<Toggle>("tgMute");
        _tgMute.RegisterValueChangedCallback(evt =>
        {
            SelectItem.IsMute = evt.newValue;
        });

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
        _txtGroup.SetValueWithoutNotify(SelectItem.Group);
        _txtPri.SetValueWithoutNotify(SelectItem.Pri);
        _tgMute.SetValueWithoutNotify(SelectItem.IsMute);
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
}
