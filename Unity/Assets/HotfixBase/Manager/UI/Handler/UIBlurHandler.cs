using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using static Ux.UIMgr;

namespace Ux
{
    internal sealed class UIBlurHandler
    {
#if UNITY_EDITOR
        /// <summary>
        /// 编辑器下默认跳过截图模糊。
        /// ScreenCapture 只能按 Screen.width/height 截图，而 Game 视图缩放时
        /// 实际渲染目标尺寸与它不一致，必然失败。真机不受影响。
        /// </summary>
        public static bool EnableInEditor = false;

        private static bool _editorSkipWarned;
#endif

        private readonly IUIBlurHandlerCallback _callback;
        private readonly List<BlurStack> _blurStacks = new();
        private readonly List<BlurSnapshot> _snapshots = new();
        private RectTransform _backdropRoot;
        private RawImage _backdropImage;

        public UIBlurHandler(IUIBlurHandlerCallback callback)
        {
            _callback = callback;
        }

        public async UniTask PrepareBeforeShowAsync(IUI ui)
        {
            if (ui == null || (ui.Blur & UIBlur.Blur) == 0)
                return;

#if UNITY_EDITOR
            if (!EnableInEditor)
            {
                if (!_editorSkipWarned)
                {
                    _editorSkipWarned = true;
                    Log.Warning("编辑器下已跳过 UI 模糊截图（Game 视图渲染目标尺寸与 Screen 不一致）。");
                }
                return;
            }
#endif

            // 截图必须在帧末（EndOfFrame）执行，LastPostLateUpdate 不保证满足该条件。
            await UniTask.WaitForEndOfFrame();

            Texture2D texture = null;
            try
            {
                texture = ScreenCapture.CaptureScreenshotAsTexture();
            }
            catch (System.Exception ex)
            {
                Log.Warning("UI 模糊截图失败，已跳过：{0}", ex.Message);
            }

            // Editor 的 Game 视图尺寸与渲染目标不一致时，ScreenCapture 会直接失败并返回 null。
            // 此时不能中断显示流程，降级为无模糊背景。
            if (texture == null)
            {
                Log.Warning("UI 模糊截图不可用（Game 视图分辨率与渲染目标不一致），已跳过模糊背景。");
                return;
            }

            texture.hideFlags = HideFlags.HideAndDontSave;
            _snapshots.Add(new BlurSnapshot(ui.ID, texture));
            ShowBackdrop(texture);
        }

        public void OnShowed(IUI ui)
        {
            if (ui == null || ui.Blur == UIBlur.None) return;
            _blurStacks.RemoveAll(x => x.ID == ui.ID);
#if UNITY_EDITOR
            _blurStacks.Add(new BlurStack(ui.Name, ui.ID, ui.Blur));
#else
            _blurStacks.Add(new BlurStack(ui.ID, ui.Blur));
#endif
        }

        public void OnHide(IUI ui)
        {
            if (ui == null) return;
            for (var i = _snapshots.Count - 1; i >= 0; i--)
            {
                if (_snapshots[i].OwnerId != ui.ID) continue;
                var snapshot = _snapshots[i];
                _snapshots.RemoveAt(i);
                ReleaseTexture(snapshot.Texture);
                if (i == _snapshots.Count)
                {
                    if (_snapshots.Count > 0) ShowBackdrop(_snapshots[^1].Texture);
                    else HideBackdrop();
                }
                return;
            }
            _blurStacks.RemoveAll(x => x.ID == ui.ID);
        }

        public void ClearSnapshots()
        {
            _blurStacks.Clear();
            foreach (var snapshot in _snapshots) ReleaseTexture(snapshot.Texture);
            _snapshots.Clear();
            HideBackdrop();
        }

        private void EnsureBackdrop()
        {
            if (_backdropRoot != null) return;
            var go = new GameObject("BlurBackdrop", typeof(RectTransform), typeof(CanvasGroup));
            _backdropRoot = go.transform as RectTransform;
            _backdropRoot.SetParent(UIMgr.Ins.GetUILayer(UILayer.BlurBackdrop), false);
            _backdropRoot.anchorMin = Vector2.zero;
            _backdropRoot.anchorMax = Vector2.one;
            _backdropRoot.offsetMin = Vector2.zero;
            _backdropRoot.offsetMax = Vector2.zero;
            var imageGo = new GameObject("BackdropImage", typeof(RectTransform), typeof(RawImage));
            var rect = imageGo.transform as RectTransform;
            rect.SetParent(_backdropRoot, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            _backdropImage = imageGo.GetComponent<RawImage>();
        }

        private void ShowBackdrop(Texture texture)
        {
            EnsureBackdrop();
            _backdropImage.texture = texture;
            _backdropRoot.gameObject.SetActive(true);
        }

        private void HideBackdrop()
        {
            if (_backdropRoot != null)
                _backdropRoot.gameObject.SetActive(false);
        }

        private static void ReleaseTexture(Texture texture)
        {
            if (texture != null) Object.Destroy(texture);
        }
    }

    internal readonly struct BlurSnapshot
    {
        public readonly int OwnerId;
        public readonly Texture Texture;
        public BlurSnapshot(int ownerId, Texture texture)
        {
            OwnerId = ownerId;
            Texture = texture;
        }
    }
}
