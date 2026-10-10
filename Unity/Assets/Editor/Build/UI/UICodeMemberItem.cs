using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
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
        Action _saveCb;
        PopupField<string> enumEvt;

        protected VisualElement root;
        public TextField txtName;
        public TextField txtType;
        public TextField txtCustomType;
        public TextField txtRes;
        public VisualElement VisualElement;
        public Toggle tgExport;
        public Toggle tgCreate;
        public VisualElement evt;
        public VisualElement doubleEvt;
        public IntegerField dCnt;
        public FloatField dGapTime;
        public VisualElement longEvt;
        public FloatField lFirst;
        public FloatField lGapTime;
        public IntegerField lCnt;
        public FloatField lRadius;

        /// <summary>原 UICodeMemberItem.uxml 的手搭等价版本。</summary>
        private void BuildUI()
        {
            root = new VisualElement();
            root.style.flexGrow = 1f;
            root.style.backgroundColor = new Color(56f / 255f, 56f / 255f, 56f / 255f);
            root.style.borderLeftWidth = 1f;
            root.style.borderRightWidth = 1f;
            root.style.borderTopWidth = 1f;
            root.style.borderBottomWidth = 1f;
            root.style.borderLeftColor = Color.white;
            root.style.borderRightColor = Color.white;
            root.style.borderTopColor = Color.white;
            root.style.borderBottomColor = Color.white;

            txtName = new TextField("名字") { name = "txtName", pickingMode = PickingMode.Ignore, isReadOnly = true };
            txtName.style.flexShrink = 0f;
            root.Add(txtName);
            txtType = ReadOnlyField("txtType", "原类型");
            root.Add(txtType);
            txtCustomType = ReadOnlyField("txtCustomType", "生成类型");
            root.Add(txtCustomType);
            txtRes = ReadOnlyField("txtRes", "资源");
            root.Add(txtRes);

            VisualElement = new VisualElement { name = "VisualElement" };
            VisualElement.style.flexGrow = 0f;
            VisualElement.style.flexDirection = FlexDirection.Column;
            root.Add(VisualElement);

            tgExport = new Toggle("是否创建变量") { name = "tgExport" };
            tgExport.style.flexGrow = 0f;
            tgExport.RegisterValueChangedCallback(_OnTgExportChanged);
            VisualElement.Add(tgExport);

            tgCreate = new Toggle("是否创建实例") { name = "tgCreate" };
            tgCreate.RegisterValueChangedCallback(_OnTgCreateChanged);
            VisualElement.Add(tgCreate);

            var wrapper = new VisualElement();
            VisualElement.Add(wrapper);

            evt = new VisualElement { name = "evt" };
            wrapper.Add(evt);

            doubleEvt = new VisualElement { name = "doubleEvt" };
            doubleEvt.style.flexDirection = FlexDirection.Column;
            wrapper.Add(doubleEvt);
            dCnt = new IntegerField("多击次数") { name = "dCnt", value = 2, tooltip = "在间隔时间内，点击几次触发" };
            dCnt.RegisterValueChangedCallback(_OnDCntChanged);
            doubleEvt.Add(dCnt);
            dGapTime = new FloatField("间隔") { name = "dGapTime", value = 0.2f };
            dGapTime.RegisterValueChangedCallback(_OnDGapTimeChanged);
            doubleEvt.Add(dGapTime);

            longEvt = new VisualElement { name = "longEvt" };
            longEvt.style.flexDirection = FlexDirection.Column;
            wrapper.Add(longEvt);
            lFirst = new FloatField("首次触发") { name = "lFirst", value = -1f, tooltip = "首次触发时间，大于-1时，生效" };
            lFirst.RegisterValueChangedCallback(_OnLFirstChanged);
            longEvt.Add(lFirst);
            lGapTime = new FloatField("间隔") { name = "lGapTime", value = 0.2f, tooltip = "长按时，每次间隔触发时间" };
            lGapTime.RegisterValueChangedCallback(_OnLGapTimeChanged);
            longEvt.Add(lGapTime);
            lCnt = new IntegerField("触发次数") { name = "lCnt", value = 0, tooltip = "循环次数，达到次数，中断长按，大于0时生效" };
            lCnt.RegisterValueChangedCallback(_OnLCntChanged);
            longEvt.Add(lCnt);
            lRadius = new FloatField("手指位置") { name = "lRadius", value = 50f, tooltip = "长按时，手指移动超出此值，则中断长按" };
            lRadius.RegisterValueChangedCallback(_OnLRadiusChanged);
            longEvt.Add(lRadius);

            TextField ReadOnlyField(string fieldName, string label)
            {
                return new TextField(label) { name = fieldName, pickingMode = PickingMode.Ignore, isReadOnly = true };
            }
        }

        public UICodeMemberItem(Action saveCb)
        {
            _saveCb = saveCb;
            BuildUI();
            Add(root);
            style.flexGrow = 1f;
        }

        private void _OnTgExportChanged(ChangeEvent<bool> e)
        {
            if (data != null)
            {
                data.isCreateVar = e.newValue;
                _saveCb?.Invoke();
            }
        }
        private void _OnTgCreateChanged(ChangeEvent<bool> e)
        {
            if (data != null)
            {
                data.isCreateIns = e.newValue;
                _saveCb?.Invoke();
            }
        }
        private void _OnDCntChanged(ChangeEvent<int> e)
        {
            ChangeEvtType();
        }
        private void _OnDGapTimeChanged(ChangeEvent<float> e)
        {
            ChangeEvtType();
        }
        private void _OnLFirstChanged(ChangeEvent<float> e)
        {
            ChangeEvtType();
        }
        private void _OnLGapTimeChanged(ChangeEvent<float> e)
        {
            ChangeEvtType();
        }
        private void _OnLCntChanged(ChangeEvent<int> e)
        {
            ChangeEvtType();
        }
        private void _OnLRadiusChanged(ChangeEvent<float> e)
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
                case "双击":
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
                case "长按":
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
                case "双击":
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

