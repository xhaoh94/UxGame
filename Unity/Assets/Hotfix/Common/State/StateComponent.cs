namespace Ux
{
    public class StateComponent : Entity, IAwakeSystem
    {
        public UnitStateMachine Machine { get; private set; }
        public void OnAwake()
        {
            Machine = StateMachine.CreateByPool<UnitStateMachine>(this);
            Machine.InitGroup("HeroZS", Parent.ID);
        }

        protected override void OnDestroy()
        {
            StateMgr.Ins.Remove(Parent.ID);
            Machine.Release();
            Machine = null;
        }
    }
}
