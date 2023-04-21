using Cysharp.Threading.Tasks;
using Ux;
using UnityEngine;

namespace Ux
{
    public class AStarComponent : Entity, IAwakeSystem
    {
        private AstarPath _astarPath;

        public void OnAwake()
        {
            // _astarPath = GameObject.Find("A*").GetComponent<AstarPath>();
            // _astarPath.data.file_cachedStartup

            var go = new GameObject("A*");
            _astarPath = go.AddComponent<AstarPath>();
            _load().Forget();
        }

        async UniTaskVoid _load()
        {
            var handle = ResMgr.Instance.LoadAssetAsync<TextAsset>("map001graph");
            await handle.ToUniTask();
            // _astarPath.data.file_cachedStartup = (TextAsset)handle.AssetObject;
            // _astarPath.data.LoadFromCache();
            _astarPath.data.DeserializeGraphs(((TextAsset)handle.AssetObject).bytes);
        }
    }
}