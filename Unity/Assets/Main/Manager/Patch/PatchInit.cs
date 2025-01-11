using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using YooAsset;

namespace Ux
{
    internal class PatchInit : PatchStateNode
    {
        protected override void OnEnter()
        {
            Initialize(GameMain.Ins.PlayMode).Forget();            
        }

        async UniTaskVoid Initialize(EPlayMode playMode)
        {
            var succeed = await YooMgr.Ins.Initialize(playMode);
            if (succeed)
            {                
                PatchMgr.Enter<PatchUpdateStaticVersion>();                
            }
            else
            {
                // 如果初始化失败弹出提示界面            
                Action callback = () =>
                {
                    Application.Quit();
                };
                PatchMgr.Ins.View.ShowMessageBox("初始化失败", "确定", callback);
            }
        }

    }
}