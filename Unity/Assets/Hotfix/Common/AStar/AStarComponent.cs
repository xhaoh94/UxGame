﻿using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Ux
{
    public class AStarComponent : Entity, IAwakeSystem<AstarPath>
    {
        public bool IsLoadComplete { get; private set; }
        public AstarPath AstarPath { get; private set; }
        Scene Map => Parent as Scene;
        public void OnAwake(AstarPath ap)
        {
            AstarPath = ap;
            _Load().Forget();
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            AstarPath.data.OnDestroy();
            AstarPath = null;
            IsLoadComplete = false;
        }
        async UniTaskVoid _Load()
        {
            IsLoadComplete = false;
            var path = string.Format(PathHelper.Res.Prefab, "map001graph");
            var ta = await ResMgr.Ins.LoadAssetAsync<TextAsset>(path);
            AstarPath.data.DeserializeGraphs(ta.bytes);
            IsLoadComplete = true;
        }
    }
}