using System;
using UnityEditor;
using UnityEngine.UIElements;
using static Ux.TimeMgr;

public class TimeDebuggerItemSub2<T> : TemplateContainer, IDebuggerListItem<T>
{
    private VisualTreeAsset _visualAsset;

    protected TextField _txtKey;
    protected TextField _txtCorn;
    protected TextField _txtTimeStamp;
    protected TextField _txtTimeDesc;
    public TimeDebuggerItemSub2()
    {
        // 加载布局文件		
        _visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Debugger/Time/TimeDebuggerItemSub2.uxml");
        if (_visualAsset == null)
            return;

        var _root = _visualAsset.CloneTree();
        _root.style.flexShrink = 1f;

        style.flexShrink = 1f;
        Add(_root);
        CreateView();
    }

    /// <summary>
    /// 初始化页面
    /// </summary>
    void CreateView()
    {
        _txtKey = this.Q<TextField>("txtKey");
        _txtCorn = this.Q<TextField>("txtCorn");
        _txtTimeStamp = this.Q<TextField>("txtTimeStamp");
        _txtTimeDesc = this.Q<TextField>("txtTimeDesc");
    }

    public virtual void SetData(T data)
    {
    }

    public virtual void SetClickEvt(Action<T> action)
    {        
    }
}

public class TimeDebuggerItemSub2Cron : TimeDebuggerItemSub2<CronHandle>
{
    public override void SetData(CronHandle data)
    {
        base.SetData(data);        
        _txtKey.SetValueWithoutNotify(data.Key.ToString());
        _txtTimeStamp.SetValueWithoutNotify(data.TimeStamp.ToString());
        _txtTimeDesc.SetValueWithoutNotify(data.TimeStampDesc);
        _txtCorn.style.display = DisplayStyle.Flex;
        _txtCorn.SetValueWithoutNotify(data.Cron);
    }
}
public class TimeDebuggerItemSub2TimeStamp : TimeDebuggerItemSub2<TimeStampHandle>
{
    public override void SetData(TimeStampHandle data)
    {
        base.SetData(data);
        _txtKey.SetValueWithoutNotify(data.Key.ToString());
        _txtTimeStamp.SetValueWithoutNotify(data.TimeStamp.ToString());
        _txtTimeDesc.SetValueWithoutNotify(data.TimeStampDesc);
        _txtCorn.style.display = DisplayStyle.None;
    }
}