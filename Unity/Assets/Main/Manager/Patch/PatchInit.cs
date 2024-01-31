using Cysharp.Threading.Tasks;
using System.Collections;
using UnityEngine;
using YooAsset;

namespace Ux
{
    internal class PatchInit : PatchStateNode
    {
        protected override void OnEnter(object args = null)
        {
            base.OnEnter(args);
            if (args is EPlayMode playMode)
            {
                Initialize(playMode).Forget();
            }
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