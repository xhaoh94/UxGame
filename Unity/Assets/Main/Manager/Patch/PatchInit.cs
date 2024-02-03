using Cysharp.Threading.Tasks;
using System.Collections;
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
            await YooMgr.Ins.Initialize(playMode);

            if (playMode == EPlayMode.EditorSimulateMode)
            {
                PatchMgr.Enter<PatchDone>();
            }
            else
            {
                PatchMgr.Enter<PatchUpdateStaticVersion>();
            }
        }

    }
}