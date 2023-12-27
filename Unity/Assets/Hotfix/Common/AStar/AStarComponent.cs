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
        protected override void OnDestroy()
        {
            base.OnDestroy(); 
            _astarPath = null;
        }
        async UniTaskVoid _load()
        {            
            var ta = await ResMgr.Ins.LoadAssetAsync<TextAsset>("map001graph");                        
            _astarPath.data.DeserializeGraphs(ta.bytes);
        }
    }
}