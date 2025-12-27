using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using static Ux.Editor.Build.UI.UIMemberData;
namespace Ux.Editor.Build.UI
{
    public partial class UICodeMemberItem : TemplateContainer
    {
        private List<string> _evtList1 = new List<string>
    {
        "点击",
        "双击",
        "长按",
        "拖拽"
    };
        private List<string> _evtList2 = new List<string>
    {
        "双击",
        "列表点击",
    };
        private VisualTreeAsset _visualAsset;
        Action _saveCb;
        PopupField<string> enumEvt;
        public UICodeMemberItem(Action saveCb)
        {
            _saveCb = saveCb;
            CreateChildren();
            Add(root);
            style.flexGrow = 1f;
        }

        partial void _OnTgExportChanged(ChangeEvent<bool> e)
        {
            if (data != null)
            {
                data.isCreateVar = e.newValue;
                _saveCb?.Invoke();
            }
        }
        partial void _OnTgCreateChanged(ChangeEvent<bool> e)
        {
            if (data != null)
            {
                data.isCreateIns = e.newValue;
                _saveCb?.Invoke();
            }
        }
        partial void _OnDCntChanged(ChangeEvent<int> e)
        {
            ChangeEvtType();
        }
        partial void _OnDGapTimeChanged(ChangeEvent<float> e)
        {
            ChangeEvtType();
        }
        partial void _OnLFirstChanged(ChangeEvent<float> e)
        {
            ChangeEvtType();
        }
        partial void _OnLGapTimeChanged(ChangeEvent<float> e)
        {
            ChangeEvtType();
        }
        partial void _OnLCntChanged(ChangeEvent<int> e)
        {
            ChangeEvtType();
        }
        partial void _OnLRadiusChanged(ChangeEvent<float> e)
        {
            ChangeEvtType();
        }

        UIMemberData data;
        public void SetData(UIMemberData data)
        {
            this.data = data;
            txtName.SetValueWithoutNotify(data.name);
            txtType.SetValueWithoutNotify(data.defaultType);
            txtCustomType.SetValueWithoutNotify(data.customType);
            if (!string.IsNullOrEmpty(data.pkg) && !string.IsNullOrEmpty(data.res))
            {
                txtRes.SetValueWithoutNotify($"{data.res}@{data.pkg}");
                txtRes.style.display = DisplayStyle.Flex;
            }
            else
            {
                txtRes.style.display = DisplayStyle.None;
            }

            var comData = data.comData;

            tgExport.style.display = DisplayStyle.None;
            tgCreate.style.display = DisplayStyle.None;
            evt.style.display = DisplayStyle.None;
            doubleEvt.style.display = DisplayStyle.None;
            longEvt.style.display = DisplayStyle.None;

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
            tgExport.style.display = DisplayStyle.Flex;
            tgCreate.style.display = DisplayStyle.Flex;
            tgExport.SetValueWithoutNotify(data.isCreateVar);
            tgCreate.SetValueWithoutNotify(data.isCreateIns);


            switch (data.defaultType)
            {
                case nameof(FairyGUI.GButton):
                    CreateEnumEvt(_evtList1);
                    evt.style.display = DisplayStyle.Flex;
                    break;
                case nameof(FairyGUI.GList):
                    CreateEnumEvt(_evtList2);
                    evt.style.display = DisplayStyle.Flex;
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
                case "���":
                    doubleEvt.style.display = DisplayStyle.Flex;
                    longEvt.style.display = DisplayStyle.None;
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
                    dCnt.SetValueWithoutNotify(dContent.dCnt);
                    dGapTime.SetValueWithoutNotify(dContent.dGapTime);
                    break;
                case "����":
                    doubleEvt.style.display = DisplayStyle.None;
                    longEvt.style.display = DisplayStyle.Flex;
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
                    lFirst.SetValueWithoutNotify(lContent.lFirst);
                    lGapTime.SetValueWithoutNotify(lContent.lGapTime);
                    lCnt.SetValueWithoutNotify(lContent.lCnt);
                    lRadius.SetValueWithoutNotify(lContent.lRadius);
                    break;
                default:
                    doubleEvt.style.display = DisplayStyle.None;
                    longEvt.style.display = DisplayStyle.None;
                    break;
            }
        }

        void ChangeEvtType()
        {
            switch (data.evtType)
            {
                case "���":
                    var dContent = new MemberEvtDouble();
                    dContent.dCnt = dCnt.value;
                    dContent.dGapTime = dGapTime.value;
                    data.evtParam = JsonConvert.SerializeObject(dContent);
                    _saveCb?.Invoke();
                    break;
                case "长按":
                    var lContent = new MemberEvtLong();
                    lContent.lFirst = lFirst.value;
                    lContent.lGapTime = lGapTime.value;
                    lContent.lCnt = lCnt.value;
                    lContent.lRadius = lRadius.value;
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
            if (enumEvt == null)
            {
                enumEvt = new PopupField<string>(choices, choices.IndexOf(data.evtType));
                enumEvt.label = "添加事件";
                enumEvt.style.width = 280;
                //_enumEvt.style.flexGrow = 1f;
                enumEvt.RegisterValueChangedCallback(evt =>
                {
                    data.evtType = evt.newValue;
                    CheckEvtType();
                    ChangeEvtType();
                });
                evt.Add(enumEvt);
            }
        }
    }
}
