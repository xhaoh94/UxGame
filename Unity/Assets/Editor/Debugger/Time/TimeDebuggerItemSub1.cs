using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static Ux.TimeMgr;
namespace Ux.Editor.Debugger.Time
{
    public partial class TimeDebuggerItemSub1 : TemplateContainer, IDebuggerListItem<TimeHandle>
    {
        protected VisualElement root;
        public TextField txtKey;
        public Toggle tgLoop;
        public TextField txtTotaCnt;
        public TextField txtExeCnt;
        public TextField txtNext;
        public TextField txtGap;
        public Label lbType;

        public TimeDebuggerItemSub1()
        {
            BuildUI();
            this.Add(root);
            CreateView();
        }

        /// <summary>原 TimeDebuggerItemSub1.uxml 的手搭等价版本。</summary>
        private void BuildUI()
        {
            root = new VisualElement();
            root.style.flexGrow = 1f;

            txtKey = DebuggerUiUtil.LockedField("txtKey", "Key");
            txtKey.style.flexShrink = 0f;
            root.Add(txtKey);

            tgLoop = new Toggle("是否循环") { name = "tgLoop" };
            tgLoop.style.flexGrow = 0f;
            root.Add(tgLoop);

            txtTotaCnt = DebuggerUiUtil.LockedField("txtTotaCnt", "需执行次数");
            txtTotaCnt.style.flexShrink = 0f;
            root.Add(txtTotaCnt);

            txtExeCnt = DebuggerUiUtil.LockedField("txtExeCnt", "已执行次数");
            txtExeCnt.style.flexShrink = 0f;
            root.Add(txtExeCnt);

            txtNext = DebuggerUiUtil.LockedField("txtNext", "下次执行");
            root.Add(txtNext);

            var gapRow = new VisualElement();
            gapRow.style.flexDirection = FlexDirection.Row;
            gapRow.style.flexGrow = 0f;
            gapRow.style.flexShrink = 0f;
            gapRow.style.height = 23f;
            txtGap = DebuggerUiUtil.LockedField("txtGap", "间隔");
            txtGap.style.flexGrow = 1f;
            gapRow.Add(txtGap);
            lbType = new Label("秒") { name = "lbType" };
            lbType.style.unityTextAlign = TextAnchor.MiddleLeft;
            gapRow.Add(lbType);
            root.Add(gapRow);
        }

        /// <summary>
        /// 初始化页面
        /// </summary>
        void CreateView()
        {
            tgLoop.SetEnabled(false);
        }


        public virtual void SetData(TimeHandle data)
        {
            if (txtKey == null) return;
            txtKey.SetValueWithoutNotify(data.Key.ToString());

            if (!data.IsLoop)
            {
                tgLoop.style.display = DisplayStyle.None;
                txtTotaCnt.style.display = DisplayStyle.Flex;
                txtTotaCnt.SetValueWithoutNotify((data.ExeCnt + data.Repeat).ToString());
            }
            else
            {
                tgLoop.style.display = DisplayStyle.Flex;
                txtTotaCnt.style.display = DisplayStyle.None;
                tgLoop.SetValueWithoutNotify(data.IsLoop);
            }
            txtNext.SetValueWithoutNotify(data.ExeTime.ToString("#0.###"));
            txtExeCnt.SetValueWithoutNotify(data.ExeCnt.ToString());
            txtGap.SetValueWithoutNotify(data.Delay.ToString());
            lbType.text = data.UseFrame ? "帧" : "秒";
        }

        public void SetClickEvt(Action<TimeHandle> action)
        {

        }
    }

}
