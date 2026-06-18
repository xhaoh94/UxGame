using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FairyGUI;
using UnityEngine;
using static Ux.UIMgr;

namespace Ux
{
    internal class UIBlurHandler
    {
        private readonly IUIBlurHandlerCallback _callback;
        private readonly List<BlurStack> _blurStacks = new List<BlurStack>();

        // 使用 List 代替 Stack，支持按 OwnerId 中间移除，避免非栈顶 UI 隐藏时截图泄漏
        private readonly List<BlurSnapshot> _snapshots = new List<BlurSnapshot>();
        private GComponent _backdropRoot;
        private GLoader _backdropLoader;
        private NTexture _backdropNTexture;
        private readonly FairyGUI.BlurFilter _sharedBlurFilter = new FairyGUI.BlurFilter();

        public UIBlurHandler(IUIBlurHandlerCallback callback)
        {
            _callback = callback;
        }

        public async UniTask PrepareBeforeShowAsync(IUI ui)
        {
            if (!NeedSnapshotBlur(ui))
            {
                return;
            }

            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);            

            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            texture.hideFlags = HideFlags.HideAndDontSave;

            var snapshot = new BlurSnapshot(ui.ID, texture);

            _snapshots.Add(snapshot);
            ShowBackdrop(texture);
        }

        public void OnShowed(IUI ui)
        {
            if (ui.Blur == UIBlur.None) return;
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
        }

        public void OnHide(IUI ui)
        {
            if (_snapshots.Count > 0)
            {
                var removedIndex = -1;
                for (int i = _snapshots.Count - 1; i >= 0; i--)
                {
                    if (_snapshots[i].OwnerId == ui.ID)
                    {
                        removedIndex = i;
                        break;
                    }
                }

                if (removedIndex >= 0)
                {
                    var snapshot = _snapshots[removedIndex];
                    _snapshots.RemoveAt(removedIndex);
                    ReleaseTexture(snapshot.Texture);

                    // 只有当栈顶被移除时才更新 backdrop
                    if (removedIndex == _snapshots.Count)
                    {
                        if (_snapshots.Count > 0)
                        {
                            ShowBackdrop(_snapshots[_snapshots.Count - 1].Texture);
                        }
                        else
                        {
                            HideBackdrop();
                        }
                    }
                    return;
                }
            }

            if (ui.Blur == UIBlur.None) return;
            for (int i = _blurStacks.Count - 1; i >= 0; i--)
            {
                if (_blurStacks[i].ID == ui.ID)
                {
                    _blurStacks.RemoveAt(i);
                    return;
                }
            }
        }

        public void ClearSnapshots()
        {
            while (_snapshots.Count > 0)
            {
                var snapshot = _snapshots[_snapshots.Count - 1];
                _snapshots.RemoveAt(_snapshots.Count - 1);
                ReleaseTexture(snapshot.Texture);
            }
            HideBackdrop();
        }

        private bool NeedSnapshotBlur(IUI ui)
        {
            return ui != null && (ui.Blur & UIBlur.Blur) != 0;
        }

        private void EnsureBackdrop()
        {
            if (_backdropRoot != null) return;

            _backdropRoot = new GComponent();
            _backdropRoot.name = _backdropRoot.gameObjectName = "BlurBackdrop";
            _backdropRoot.sortingOrder = 10;
            _backdropRoot.MakeFullScreen();
            _backdropRoot.AddRelation(GRoot.inst, RelationType.Size);
            UIMgr.Ins.GetLayer(UILayer.BlurBackdrop).AddChild(_backdropRoot);

            _backdropLoader = new GLoader();            
            _backdropLoader.SetSize(GRoot.inst.width, GRoot.inst.height);
            _backdropLoader.fill = FillType.ScaleFree;
            _backdropRoot.AddChild(_backdropLoader);
        }

        private void ShowBackdrop(Texture texture)
        {
            EnsureBackdrop();

            if (_backdropNTexture != null)
            {
                _backdropNTexture.Dispose();
            }

            _backdropNTexture = new NTexture(texture);
            _backdropLoader.texture = _backdropNTexture;
            _backdropRoot.visible = true;
            _backdropRoot.filter = _sharedBlurFilter;
        }

        private void HideBackdrop()
        {
            if (_backdropRoot != null)
            {
                _backdropRoot.visible = false;
                _backdropRoot.filter = null;
            }

            if (_backdropNTexture != null)
            {
                var unityTex = _backdropNTexture.nativeTexture;
                _backdropNTexture.Dispose();
                _backdropNTexture = null;
                if (unityTex != null)
                {
                    Object.Destroy(unityTex);
                }
            }
        }

        private void ReleaseTexture(Texture texture)
        {
            if (texture != null)
            {
                Object.Destroy(texture);
            }
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
