using Cysharp.Threading.Tasks;
using Ux;
using UnityEngine;
using Unity.VisualScripting;

namespace Ux
{
    public class AStarComponent : Entity, IAwakeSystem<AstarPath>
    {
        public AstarPath AstarPath { get; private set; }
        Map Map => Parent as Map;
        public void OnAwake(AstarPath ap)
        {
            AstarPath = ap;
            _load().Forget();
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            AstarPath.data.OnDestroy();
            AstarPath = null;
        }
        async UniTaskVoid _load()
        {
            var ta = await ResMgr.Ins.LoadAssetAsync<TextAsset>("map001graph");
            AstarPath.data.DeserializeGraphs(ta.bytes);
            Parent.AddComponent<FogOfWarComponent>();
        }
    }
}