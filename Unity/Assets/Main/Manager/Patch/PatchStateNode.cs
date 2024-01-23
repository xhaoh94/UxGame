namespace Ux
{
    public abstract class PatchStateNode : StateNode
    {
        protected PatchMgr PatchMgr => PatchMgr.Ins;
        public override void Enter(object args = null)
        {                        
            PatchMgr.View?.OnStatusChanged(GetType().Name);
            base.Enter(args);
        }
    }
}
