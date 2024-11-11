using System;
using UnityEditor;
using UnityEngine.UIElements;
using static Ux.TimeMgr;
namespace Ux.Editor.Debugger.Time
{
    public partial class TimeDebuggerItemSub1 : TemplateContainer, IDebuggerListItem<TimeHandle>
    {
        private VisualTreeAsset _visualAsset;


        protected TextField _txtKey;
        protected TextField _txtGap;
        protected Label _lbType;
        protected Toggle _tgLoop;
        protected TextField _txtNext;
        protected TextField _txtExeCnt;
        protected TextField _txtTotaCnt;
        public TimeDebuggerItemSub1()
        {
            // 加载布局文件		
            _visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Debugger/Time/TimeDebuggerItemSub1.uxml");
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
            _txtGap = this.Q<TextField>("txtGap");
            _lbType = this.Q<Label>("lbType");
            _tgLoop = this.Q<Toggle>("tgLoop");
            _tgLoop.SetEnabled(false);
            _txtExeCnt = this.Q<TextField>("txtExeCnt");
            _txtTotaCnt = this.Q<TextField>("txtTotaCnt");
            _txtNext = this.Q<TextField>("txtNext");
        }


        public virtual void SetData(TimeHandle data)
        {
            _txtKey.SetValueWithoutNotify(data.Key.ToString());

            if (!data.IsLoop)
            {
                _tgLoop.style.display = DisplayStyle.None;
                _txtTotaCnt.style.display = DisplayStyle.Flex;
                _txtTotaCnt.SetValueWithoutNotify((data.ExeCnt + data.Repeat).ToString());
            }
            else
            {
                _tgLoop.style.display = DisplayStyle.Flex;
                _txtTotaCnt.style.display = DisplayStyle.None;
                _tgLoop.SetValueWithoutNotify(data.IsLoop);
            }
            _txtNext.SetValueWithoutNotify(data.ExeTime.ToString());
            _txtExeCnt.SetValueWithoutNotify(data.ExeCnt.ToString());
            _txtGap.SetValueWithoutNotify(data.Delay.ToString());
            _lbType.text = data.UseFrame ? "帧" : "秒";
        }

        public void SetClickEvt(Action<TimeHandle> action)
        {

        }
    }

}
