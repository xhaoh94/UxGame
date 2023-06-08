using Cysharp.Threading.Tasks;
using Ux;
using UnityEngine;

namespace Ux
{
    public class AStarComponent : Entity, IAwakeSystem<AstarPath>
    {
        private AstarPath _astarPath;

        public void OnAwake(AstarPath ap)
        {
            _astarPath = ap;
            _load().Forget();
        }

        async UniTaskVoid _load()
        {
            var handle = ResMgr.Ins.LoadAssetAsync<TextAsset>("map001graph");
            await handle.ToUniTask();
            _astarPath.data.DeserializeGraphs(((TextAsset)handle.AssetObject).bytes);
        }
    }
}