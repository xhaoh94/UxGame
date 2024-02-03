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
            Machine.AddNode<HeroZSIdle>();
            Machine.AddNode<HeroZSRun>();
            //Machine.AddNode<StateAttack>();
            //Machine.AddNode<StateSkilll08>();            
        }

        protected override void OnDestroy()
        {
            Machine.Release();
            Machine = null;
        }

        //[ListenAddEntity(typeof(AnimComponent))]
        //void OnAddAnimComponent(AnimComponent anim)
        //{
        //    Machine.Enter<StateIdle>();            
        //}
        [ListenAddEntity(typeof(PlayableDirectorComponent))]
        void OnAddAnimComponent(PlayableDirectorComponent anim)
        {
            //Machine.Enter<StateIdle>();            
        }
    }  
}
