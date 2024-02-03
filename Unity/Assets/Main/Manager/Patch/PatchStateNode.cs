namespace Ux
{
    public abstract class PatchStateNode : StateNode
    {
        protected PatchMgr PatchMgr => PatchMgr.Ins;

        protected override void OnEnter()
        {
            PatchMgr.View?.OnStatusChanged(GetType().Name);
            base.OnEnter();
        }
    }
}
