namespace Ux
{
    public abstract class PatchStateNode : StateNode
    {
        protected PatchMgr PatchMgr => PatchMgr.Ins;

        protected override void OnEnter(object args = null)
        {
            PatchMgr.View?.OnStatusChanged(GetType().Name);
            base.OnEnter(args);
        }
    }
}
