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
            Log.Debug("AStarComponent-OnAwake");
            _astarPath = ap;
            _load().Forget();
        }

        async UniTaskVoid _load()
        {
            Log.Debug("AStarComponent-LoadAssetAsync.map001graph");
            var handle = ResMgr.Ins.LoadAssetAsync<TextAsset>("map001graph");
            await handle.ToUniTask();
            Log.Debug("AStarComponent-DeserializeGraphs.map001graph");
            _astarPath.data.DeserializeGraphs(((TextAsset)handle.AssetObject).bytes);
        }
    }
}