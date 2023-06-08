namespace Ux
{
    internal class PatchPatchInit : PatchStateNode
    {
        protected override void OnEnter(object args)
        {
            UIMgr.Ins.Show<PatchView>();
            PatchMgr.Enter<PatchUpdateStaticVersion>();
        }
        protected override void OnUpdate()
        {
        }
        protected override void OnExit()
        {

        }


    }
}