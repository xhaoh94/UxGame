using Cysharp.Threading.Tasks;
using System;
namespace Ux
{
    internal class PatchDone : PatchStateNode
    {
        protected override void OnEnter()
        {
            base.OnEnter();
            try
            {                
                HotFixMgr.Ins.Init();
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
    }
}