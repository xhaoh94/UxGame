using Cysharp.Threading.Tasks;
using Ux;
using UnityEngine;
using Unity.VisualScripting;

namespace Ux
{
    public class StateComponent : Entity, IAwakeSystem, IListenSystem
    {
        public StateMachine Machine { get; private set; }
        public void OnAwake()
        {
            Machine = StateMachine.CreateByPool(false, Parent);
            Machine.AddNode<StateIdle>();
            Machine.AddNode<StateRun>();
            Machine.AddNode<StateAttack>();
            Machine.Enter<StateIdle>();
        }

        protected override void OnDestroy()
        {
            Machine.Release();
            Machine = null;
        }

        [ListenAddEntity(typeof(AnimComponent))]
        void OnAddAnimComponent(AnimComponent anim)
        {
            if (anim == null) return;
            Machine.ForEach<UnitStateNode>(_node => _node.AddAnimation(anim).Forget());
        }
    }  
}
