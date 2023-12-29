using UnityEngine;
using YooAsset;

namespace Ux
{
    public class ResHandleMono : MonoBehaviour
    {
        AssetHandle _handle;
        public string Location { get;private set; }
        public void Init(string location, AssetHandle handle)
        {
            _handle = handle;
            Location = location;
        }
        private void OnDestroy()
        {
            _handle?.Release();
        }
    }
}