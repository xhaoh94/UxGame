using Cysharp.Threading.Tasks;
using System;
namespace Ux
{
    internal class PatchDone : PatchStateNode
    {
        protected override void OnEnter(object args)
        {
            StartAsync().Forget();
        }
        private async UniTaskVoid StartAsync()
        {
            try
            {
                await HotFixMgr.Instance.Load();
                HotFixMgr.Instance.Init();
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        protected override void OnUpdate()
        {
        }
        protected override void OnExit()
        {            
        }
    }
}