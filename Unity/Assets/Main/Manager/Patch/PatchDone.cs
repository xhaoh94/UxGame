using Cysharp.Threading.Tasks;
using System;
namespace Ux
{
    internal class PatchDone : PatchStateNode
    {
        protected override void OnEnter(object args)
        {
            try
            {
                HotFixMgr.Ins.Load();
                HotFixMgr.Ins.Init();
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