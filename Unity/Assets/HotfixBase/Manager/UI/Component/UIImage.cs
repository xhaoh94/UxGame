using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.U2D;
using YooAsset;

namespace Ux
{
    /// <summary>
    /// 按名字换图的 Image 包装，内部拼资源地址，持有句柄，换图或 Dispose 时释放。
    /// 外部只传名字（不带 UI_/Texture_/Atlas_ 前缀）。
    /// </summary>
    public class UIImage : UIObject
    {
        private AssetHandle _spriteHandle;
        private AssetHandle _atlasHandle;

        // 每次换图递增。异步回来发现版本变了 = 期间又换过图，本次结果作废。
        private int _requestVersion;

        public UnityEngine.UI.Image Image { get; private set; }

        public UIImage(GameObject container, UIObject parent = null)
        {
            Init(container, parent);
            Image = container != null ? container.GetComponent<UnityEngine.UI.Image>() : null;
            parent?.AddComponent(this);
        }
        
        public void SetSprite(string imageName)
        {
            ++_requestVersion;
            ReleaseAll();
            if (Image == null) return;
            Image.sprite = null;
            if (string.IsNullOrEmpty(imageName)) return;

            _spriteHandle = ResMgr.Ins.LoadAssetHandle(string.Format(PathHelper.Res.Texture, imageName));
            Image.sprite = _spriteHandle != null ? _spriteHandle.GetAssetObject<Sprite>() : null;
        }

        
        public async UniTask SetSpriteAsync(string imageName)
        {
            var version = ++_requestVersion;
            ReleaseAll();
            if (Image == null) return;
            Image.sprite = null;
            if (string.IsNullOrEmpty(imageName)) return;

            var handle = await ResMgr.Ins.LoadAssetHandleAsync(string.Format(PathHelper.Res.Texture, imageName));
            if (version != _requestVersion || Image == null)
            {
                handle?.Release();
                return;
            }

            _spriteHandle = handle;
            Image.sprite = handle != null ? handle.GetAssetObject<Sprite>() : null;
        }

        /// <summary>图集：传图集名和 sprite 名，图集地址为 Atlas_图集名。</summary>
        public void SetSpriteFromAtlas(string atlasName, string spriteName)
        {
            ++_requestVersion;
            ReleaseAll();
            if (Image == null) return;
            Image.sprite = null;
            if (string.IsNullOrEmpty(atlasName) || string.IsNullOrEmpty(spriteName)) return;

            _atlasHandle = ResMgr.Ins.LoadAssetHandle(string.Format(PathHelper.Res.Atlas, atlasName));
            var atlas = _atlasHandle != null ? _atlasHandle.GetAssetObject<SpriteAtlas>() : null;
            Image.sprite = atlas != null ? atlas.GetSprite(spriteName) : null;
        }

        /// <summary>图集（异步）。</summary>
        public async UniTask SetSpriteFromAtlasAsync(string atlasName, string spriteName)
        {
            var version = ++_requestVersion;
            ReleaseAll();
            if (Image == null) return;
            Image.sprite = null;
            if (string.IsNullOrEmpty(atlasName) || string.IsNullOrEmpty(spriteName)) return;

            var handle = await ResMgr.Ins.LoadAssetHandleAsync(string.Format(PathHelper.Res.Atlas, atlasName));
            if (version != _requestVersion || Image == null)
            {
                handle?.Release();
                return;
            }

            _atlasHandle = handle;
            var atlas = handle != null ? handle.GetAssetObject<SpriteAtlas>() : null;
            Image.sprite = atlas != null ? atlas.GetSprite(spriteName) : null;
        }

        /// <summary>不持有句柄的换图（prefab 上挂好的固定引用）。</summary>
        public void SetSpriteReference(Sprite sprite)
        {
            ++_requestVersion;
            ReleaseAll();
            if (Image != null) Image.sprite = sprite;
        }

        private void ReleaseAll()
        {
            _spriteHandle?.Release();
            _spriteHandle = null;
            _atlasHandle?.Release();
            _atlasHandle = null;
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            // 递增版本：还在飞的异步请求回来后会发现版本不符，自己释放句柄。
            ++_requestVersion;
            ReleaseAll();
            if (Image != null) Image.sprite = null;
        }
    }
}
