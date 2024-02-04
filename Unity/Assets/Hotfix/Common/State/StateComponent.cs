using Cysharp.Threading.Tasks;
using Ux;
using UnityEngine;
using Unity.VisualScripting;

namespace Ux
{
    public class StateComponent : Entity, IAwakeSystem, IListenSystem
    {
        public UnitStateMachine Machine { get; private set; }
        public void OnAwake()
        {
            Machine = StateMachine.CreateByPool<UnitStateMachine>(true, this);
            Machine.InitGroup("HeroZs", Parent.ID);
        }

        protected override void OnDestroy()
        {
            Machine.Release();
            Machine = null;
        }

        [ListenAddEntity(typeof(AnimComponent))]
        void OnAddAnimComponent(AnimComponent anim)
        {
            Machine.ForEach<IUnitState>((unit) =>
            {
                if (unit is IUnitAnimState animState)
                {
                    animState.Set(anim);
                }
            });
        }
        [ListenAddEntity(typeof(PlayableDirectorComponent))]
        void OnAddAnimComponent(PlayableDirectorComponent director)
        {
            Machine.ForEach<IUnitState>((unit) =>
            {
                if (unit is IUnitTimelineState timeLineState)
                {
                    timeLineState.Set(director);
                }
            });
        }
    }
}
