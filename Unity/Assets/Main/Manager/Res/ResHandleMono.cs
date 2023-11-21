using System;
using System.Collections;
using UnityEngine;
using YooAsset;

namespace Ux
{
    public class ResHandleMono : MonoBehaviour
    {
        AssetHandle _handle;
        public void Init(AssetHandle handle)
        {
            _handle = handle;
        }
        private void OnDestroy()
        {
            _handle?.Release();
        }
    }
}