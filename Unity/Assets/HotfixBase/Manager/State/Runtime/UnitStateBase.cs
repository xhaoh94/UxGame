using System;
using System.Collections.Generic;

namespace Ux
{
    public interface IUnitState : IStateNode
    {
        void Set(long id);
        bool IsMute { get; }
        int Priority { get; }
        bool IsValid { get; }
        bool IsAdditive { get; }
        long OwnerID { get; }
        UnitStateMachine StateMachine { get; }
        List<StateConditionBase> Conditions { get; }
    }

    public abstract class UnitStateBase : StateNode, IUnitState
    {
        public UnitStateMachine StateMachine => Machine as UnitStateMachine;
        public StateAsset Asset { get; private set; }
        public virtual bool IsMute => Asset.isMute;
        public virtual int Priority => Asset.priority;
        public virtual bool IsAdditive { get; } = false;
        public virtual string AssetName { get; } = null;
        public List<StateConditionBase> Conditions { get; protected set; }
        public virtual bool IsValid
        {
            get
            {
                if (Conditions == null) return false;
                foreach (var condition in Conditions)
                {
                    if (!condition.IsValid) return false;
                }
                return true;
            }
        }

        public long OwnerID { get; private set; }
        void IUnitState.Set(long id)
        {
            OwnerID = id;
            InitConditions();            
        }
        protected virtual void InitConditions()
        {
            if(string.IsNullOrEmpty(AssetName)) return;
            Asset = ResMgr.Ins.LoadAsset<StateAsset>($"{AssetName}");
            Conditions = Asset.conditions;
        }
                

        protected override void OnRelease()
        {
            base.OnRelease();
            OwnerID = 0;
            Conditions = null;
        }
    }    
}