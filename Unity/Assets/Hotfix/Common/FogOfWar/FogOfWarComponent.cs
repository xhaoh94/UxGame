using Cysharp.Threading.Tasks;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace Ux
{
    public class FogOfWarComponent : Entity, IAwakeSystem
    {
        Scene Map => ParentAs<Scene>();
        void IAwakeSystem.OnAwake()
        {
            _UpdateCamera();
            _UpdateMapDataAsync().Forget();
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            FogOfWarMgr.Ins.Destroy();
        }
        void _UpdateCamera()
        {
            FogOfWarMgr.Ins.SetCamera(Map.Camera.MapCamera);
        }
        async UniTaskVoid _UpdateMapDataAsync()
        {
            var astar = Map.AStar;
            while (astar != null && !astar.IsLoadComplete)
            {
                await UniTask.Yield();
            }
            var gridGraph = astar.AstarPath.data.gridGraph;
            FogOfWarMgr.Ins.Init(gridGraph.Width, gridGraph.Depth, gridGraph.nodeSize);
            for (int i = 0; i < gridGraph.nodes.Length; i++)
            {
                FogOfWarMgr.Ins.SetAltitude(i, 1);
            }
        }
    }
}