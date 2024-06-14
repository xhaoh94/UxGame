using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using static Ux.Editor.Build.UI.UIMemberData;
namespace Ux.Editor.Build.UI
{
    public class UICodeMemberItem : TemplateContainer
    {
        private List<string> _evtList1 = new List<string>
    {
        "无",
        "单击",
        "多击",
        "长按"
    };
        private List<string> _evtList2 = new List<string>
    {
        "无",
        "列表点击",
    };
        private VisualTreeAsset _visualAsset;

        TextField _txtName;
        TextField _txtType;
        TextField _txtCustomType;
        TextField _txtRes;
        Toggle _tgExport;
        Toggle _tgCreate;
        VisualElement _evt;
        PopupField<string> _enumEvt;
        VisualElement _doubleEvt;
        IntegerField _dCnt;
        FloatField _dGapTime;
        VisualElement _longEvt;
        FloatField _lFirst;
        FloatField _lGapTime;
        IntegerField _lCnt;
        FloatField _lRadius;
        Action _saveCb;
        public UICodeMemberItem(Action saveCb)
        {
            _saveCb = saveCb;
            // 加载布局文件		
            _visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Build/UI/UICodeMemberItem.uxml");
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
            _txtName = this.Q<TextField>("txtName");
            _txtType = this.Q<TextField>("txtType");
            _txtCustomType = this.Q<TextField>("txtCustomType");

            _txtRes = this.Q<TextField>("txtRes");
            _tgExport = this.Q<Toggle>("tgExport");
            _tgExport.RegisterValueChangedCallback(evt =>
            {
                if (data != null)
                {
                    data.isCreateVar = evt.newValue;
                    _saveCb?.Invoke();
                }
            });
            _tgCreate = this.Q<Toggle>("tgCreate");
            _tgCreate.RegisterValueChangedCallback(evt =>
            {
                if (data != null)
                {
                    data.isCreateIns = evt.newValue;
                    _saveCb?.Invoke();
                }
            });

            _evt = this.Q<VisualElement>("evt");

            _doubleEvt = this.Q<VisualElement>("doubleEvt");
            _dCnt = this.Q<IntegerField>("dCnt");
            _dCnt.RegisterValueChangedCallback(evt => { ChangeEvtType(); });
            _dGapTime = this.Q<FloatField>("dGapTime");
            _dGapTime.RegisterValueChangedCallback(evt => { ChangeEvtType(); });

            _longEvt = this.Q<VisualElement>("longEvt");
            _lFirst = this.Q<FloatField>("lFirst");
            _lFirst.RegisterValueChangedCallback(evt => { ChangeEvtType(); });

            _lGapTime = this.Q<FloatField>("lGapTime");
            _lGapTime.RegisterValueChangedCallback(evt => { ChangeEvtType(); });

            _lCnt = this.Q<IntegerField>("lCnt");
            _lCnt.RegisterValueChangedCallback(evt => { ChangeEvtType(); });

            _lRadius = this.Q<FloatField>("lRadius");
            _lRadius.RegisterValueChangedCallback(evt => { ChangeEvtType(); });
        }
        UIMemberData data;
        public void SetData(UIMemberData data)
        {
            this.data = data;
            _txtName.SetValueWithoutNotify(data.name);
            _txtType.SetValueWithoutNotify(data.defaultType);
            _txtCustomType.SetValueWithoutNotify(data.customType);
            if (!string.IsNullOrEmpty(data.pkg) && !string.IsNullOrEmpty(data.res))
            {
                _txtRes.SetValueWithoutNotify($"{data.res}@{data.pkg}");
                _txtRes.style.display = DisplayStyle.Flex;
            }
            else
            {
                _txtRes.style.display = DisplayStyle.None;
            }

            var comData = data.comData;

            _tgExport.style.display = DisplayStyle.None;
            _tgCreate.style.display = DisplayStyle.None;
            _evt.style.display = DisplayStyle.None;
            _doubleEvt.style.display = DisplayStyle.None;
            _longEvt.style.display = DisplayStyle.None;

            if (comData.IsTabFrame)
            {
                foreach (var temData in comData.TabViewData)
                {
                    if (temData.Name == data.name) return;
                }
            }

            if (comData.IsMessageBox)
            {
                foreach (var temData in comData.MessageBoxData)
                {
                    if (temData.Name == data.name) return;
                }
            }

            if (comData.IsTabFrame)
            {
                foreach (var temData in comData.TipData)
                {
                    if (temData.Name == data.name) return;
                }
            }
            _tgExport.style.display = DisplayStyle.Flex;
            _tgCreate.style.display = DisplayStyle.Flex;
            _tgExport.SetValueWithoutNotify(data.isCreateVar);
            _tgCreate.SetValueWithoutNotify(data.isCreateIns);


            switch (data.defaultType)
            {
                case nameof(FairyGUI.GButton):
                    CreateEnumEvt(_evtList1);
                    _evt.style.display = DisplayStyle.Flex;
                    break;
                case nameof(FairyGUI.GList):
                    CreateEnumEvt(_evtList2);
                    _evt.style.display = DisplayStyle.Flex;
                    break;
                default:
                    return;
            }

            CheckEvtType();

        }
        void CheckEvtType()
        {
            switch (data.evtType)
            {
                case "多击":
                    _doubleEvt.style.display = DisplayStyle.Flex;
                    _longEvt.style.display = DisplayStyle.None;
                    MemberEvtDouble dContent;
                    if (string.IsNullOrEmpty(data.evtParam))
                    {
                        dContent = new MemberEvtDouble();
                        dContent.dCnt = 2;
                        dContent.dGapTime = 0.2f;
                    }
                    else
                    {
                        dContent = JsonConvert.DeserializeObject<MemberEvtDouble>(data.evtParam);
                    }
                    _dCnt.SetValueWithoutNotify(dContent.dCnt);
                    _dGapTime.SetValueWithoutNotify(dContent.dGapTime);
                    break;
                case "长按":
                    _doubleEvt.style.display = DisplayStyle.None;
                    _longEvt.style.display = DisplayStyle.Flex;
                    MemberEvtLong lContent;
                    if (string.IsNullOrEmpty(data.evtParam))
                    {
                        lContent = new MemberEvtLong();
                        lContent.lFirst = -1;
                        lContent.lGapTime = 0.2f;
                        lContent.lCnt = 0;
                        lContent.lRadius = 50f;
                    }
                    else
                    {
                        lContent = JsonConvert.DeserializeObject<MemberEvtLong>(data.evtParam);
                    }
                    _lFirst.SetValueWithoutNotify(lContent.lFirst);
                    _lGapTime.SetValueWithoutNotify(lContent.lGapTime);
                    _lCnt.SetValueWithoutNotify(lContent.lCnt);
                    _lRadius.SetValueWithoutNotify(lContent.lRadius);
                    break;
                default:
                    _doubleEvt.style.display = DisplayStyle.None;
                    _longEvt.style.display = DisplayStyle.None;
                    break;
            }
        }

        void ChangeEvtType()
        {
            switch (data.evtType)
            {
                case "多击":
                    var dContent = new MemberEvtDouble();
                    dContent.dCnt = _dCnt.value;
                    dContent.dGapTime = _dGapTime.value;
                    data.evtParam = JsonConvert.SerializeObject(dContent);
                    _saveCb?.Invoke();
                    break;
                case "长按":
                    var lContent = new MemberEvtLong();
                    lContent.lFirst = _lFirst.value;
                    lContent.lGapTime = _lGapTime.value;
                    lContent.lCnt = _lCnt.value;
                    lContent.lRadius = _lRadius.value;
                    data.evtParam = JsonConvert.SerializeObject(lContent);
                    _saveCb?.Invoke();
                    break;
                default:
                    data.evtParam = string.Empty;
                    _saveCb?.Invoke();
                    break;
            }
        }
        void CreateEnumEvt(List<string> choices)
        {
            if (_enumEvt == null)
            {
                _enumEvt = new PopupField<string>(choices, choices.IndexOf(data.evtType));
                _enumEvt.label = "点击事件";
                _enumEvt.style.width = 280;
                //_enumEvt.style.flexGrow = 1f;
                _enumEvt.RegisterValueChangedCallback(evt =>
                {
                    data.evtType = evt.newValue;
                    CheckEvtType();
                    ChangeEvtType();
                });
                _evt.Add(_enumEvt);
            }
        }
    }
}
