﻿using UnityEngine.SceneManagement;
using YooAsset;

namespace Ux
{
    public partial class ResMgr
    {                
        /// <summary>
        /// 异步加载场景
        /// </summary>
        /// <param name="location">场景的定位地址</param>
        /// <param name="sceneMode">场景加载模式</param>
        /// <param name="activateOnLoad">加载完毕时是否主动激活</param>
        /// <param name="priority">优先级</param>
        public SceneHandle LoadSceneAsync(string location, LoadSceneMode sceneMode = LoadSceneMode.Single, LocalPhysicsMode physicsMode = LocalPhysicsMode.None, bool suspendLoad = false, uint priority = 100)
        {
            var package = YooMgr.Ins.GetPackageByLocation(location);
            if (package == null) return null;
            return package.Package.LoadSceneAsync(location, sceneMode, physicsMode, suspendLoad, priority);
        }
        /// <summary>
        /// 异步加载场景
        /// </summary>
        /// <param name="assetInfo">场景的资源信息</param>
        /// <param name="sceneMode">场景加载模式</param>
        /// <param name="suspendLoad">场景加载到90%自动挂起</param>
        /// <param name="priority">优先级</param>
        public SceneHandle LoadSceneAsync(AssetInfo assetInfo, LoadSceneMode sceneMode = LoadSceneMode.Single, LocalPhysicsMode physicsMode = LocalPhysicsMode.None, bool suspendLoad = false, uint priority = 100)
        {
            var package = YooMgr.Ins.GetPackageByLocation(assetInfo.Address);
            if (package == null) return null;
            return package.Package.LoadSceneAsync(assetInfo, sceneMode, physicsMode, suspendLoad, priority);
        }        
    }
}
