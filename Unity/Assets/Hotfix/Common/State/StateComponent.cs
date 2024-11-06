namespace Ux
{
    public class StateComponent : Entity, IAwakeSystem, IListenSystem
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

        [ListenAddEntity(typeof(AnimComponent))]
        void OnAddAnimComponent(AnimComponent anim)
        {
            Machine.ForEach<IUnitState>((unit) =>
            {
                //if (unit is IUnitAnimState animState)
                //{
                //    animState.Set(anim);
                //}
            });
        }
        [ListenAddEntity(typeof(PlayableDirectorComponent))]
        void OnAddAnimComponent(PlayableDirectorComponent director)
        {
            Machine.ForEach<IUnitState>((unit) =>
            {
                //if (unit is IUnitTimelineState timeLineState)
                //{
                //    timeLineState.Set(director);
                //}
            });
        }
    }
}
