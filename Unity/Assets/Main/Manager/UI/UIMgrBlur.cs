using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    public partial class UIMgr
    {
        /// <summary>
        /// 模糊队列
        /// </summary>
        List<BlurStack> _blurStacks = new List<BlurStack>();
        void _ShowCallBack_Blur(IUI ui)
        {
            if (ui.Blur == UIBlur.None || ui.Blur == UIBlur.Normal) return;
#if UNITY_EDITOR
            _blurStacks.Add(new BlurStack(ui.IDStr, ui.ID, ui.Blur));
#else
            _blurStacks.Add(new BlurStack(ui.ID, ui.Blur));
#endif
            _FlagBlur();
        }

        void _HideCallBack_Blur(IUI ui)
        {
            if (ui.Blur == UIBlur.None || ui.Blur == UIBlur.Normal) return;
            var lastIndex = _blurStacks.FindLastIndex(x => x.ID == ui.ID);
            if (lastIndex >= 0)
            {
                _blurStacks.RemoveAt(lastIndex);
                _FlagBlur();
            }
        }
        void _FlagBlur()
        {
            bool flagBlur = false, flagFixed = false, flagScene = false;
            BlurStack? blurStack = null;
            if (_blurStacks.Count > 0)
            {
                blurStack = _blurStacks[_blurStacks.Count - 1];
                flagBlur = blurStack.Value.Blur.HasFlag(UIBlur.Blur);
                flagFixed = blurStack.Value.Blur.HasFlag(UIBlur.Fixed);
            }

            foreach (var kv in _showed)
            {
                var ui = kv.Value;
                if (blurStack == null)
                {
                    ui.Filter = null;
                    continue;
                }
                if (kv.Key == blurStack.Value.ID)
                {
                    ui.Filter = null;
                    continue;
                }
                if (ui.Blur.HasFlag(UIBlur.None))
                {
                    ui.Filter = null;
                    continue;
                }
                if (ui.Type == UIType.Fixed && !flagFixed)
                {
                    ui.Filter = null;
                    continue;
                }
                if (ui.Type != UIType.Fixed && !flagBlur)
                {
                    ui.Filter = null;
                    continue;
                }
                var filter = ui.Filter;
                if (filter != null && filter is FairyGUI.BlurFilter) continue;
                ui.Filter = new FairyGUI.BlurFilter();
            }


            if (_blurStacks != null)
            {
                flagScene = blurStack.Value.Blur.HasFlag(UIBlur.Scene);
            }

            SceneBlur.Set(flagScene);            
        }
    }
}