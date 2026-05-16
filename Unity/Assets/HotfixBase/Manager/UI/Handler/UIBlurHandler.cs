using System.Collections.Generic;
using UnityEngine;
using static Ux.UIMgr;

namespace Ux
{
    public class UIBlurHandler
    {
        private readonly IUIBlurHandlerCallback _callback;
        private Camera _mainCamera;
        private readonly List<BlurStack> _blurStacks = new List<BlurStack>();
        private readonly FairyGUI.BlurFilter _sharedBlurFilter = new FairyGUI.BlurFilter();

        public UIBlurHandler(IUIBlurHandlerCallback callback)
        {
            _callback = callback;
        }

        public void SetSceneCamera(Camera mainCamera)
        {
            _mainCamera = mainCamera;
        }

        public void OnShowed(IUI ui)
        {
            if (ui.Blur == UIBlur.None || ui.Blur == UIBlur.Normal) return;
            for (int i = _blurStacks.Count - 1; i >= 0; i--)
            {
                if (_blurStacks[i].ID == ui.ID)
                {
                    _blurStacks.RemoveAt(i);
                }
            }
#if UNITY_EDITOR
            _blurStacks.Add(new BlurStack(ui.Name, ui.ID, ui.Blur));
#else
            _blurStacks.Add(new BlurStack(ui.ID, ui.Blur));
#endif
            _FlagBlur();
        }

        public void OnHide(IUI ui)
        {
            if (ui.Blur == UIBlur.None || ui.Blur == UIBlur.Normal) return;
            for (int i = _blurStacks.Count - 1; i >= 0; i--)
            {
                if (_blurStacks[i].ID == ui.ID)
                {
                    _blurStacks.RemoveAt(i);
                    _FlagBlur();
                    return;
                }
            }
        }

        void _FlagBlur()
        {
            bool flagBlur = false, flagFixed = false, flagScene = false;
            BlurStack? blurStack = null;
            if (_blurStacks.Count > 0)
            {
                blurStack = _blurStacks[_blurStacks.Count - 1];
                var blur = blurStack.Value.Blur;
                flagBlur = (blur & UIBlur.Blur) != 0;
                flagFixed = (blur & UIBlur.Fixed) != 0;
            }

            var showed = _callback.GetShowedDict();
            var topBlurId = blurStack?.ID ?? 0;
            foreach (var ui in showed.Values)
            {
                if (blurStack == null)
                {
                    ui.Filter = null;
                    continue;
                }
                if (ui.ID == topBlurId)
                {
                    ui.Filter = null;
                    continue;
                }
                if ((ui.Blur & UIBlur.None) != 0)
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
                if (ReferenceEquals(ui.Filter, _sharedBlurFilter) || ui.Filter is FairyGUI.BlurFilter)
                {
                    ui.Filter = _sharedBlurFilter;
                    continue;
                }
                ui.Filter = _sharedBlurFilter;
            }

            if (_mainCamera != null)
            {
                if (blurStack != null)
                {
                    flagScene = (blurStack.Value.Blur & UIBlur.Scene) != 0;
                }
                Blur.SetCamera(_mainCamera, flagScene);
            }
        }
    }
}
