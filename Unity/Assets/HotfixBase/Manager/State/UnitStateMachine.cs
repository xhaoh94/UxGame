using System.Collections.Generic;

namespace Ux
{
    public class UnitStateMachine : StateMachine
    {
        public long ID;
        public IUnitState CurrentState { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        public HashSet<IUnitState> Additives { get; } = new();
        protected override bool IsCanEnter(IStateNode node)
        {
            return IsPlaying(node.Name) == false;
        }
        protected override bool IsCanExit(IStateNode node)
        {
            return IsPlaying(node.Name);
        }
        public bool IsPlaying(string entryNode)
        {
            var node = GetNode(entryNode) as IUnitState;
            if (node.IsAdditive)
            {
                return Additives.Contains(node);
            }
            return CurrentState == node;            
        }
        protected override void OnEnter(IStateNode node)
        {
            IUnitState unitState = node as IUnitState;            
            if (unitState.IsAdditive)
            {
                Additives.Add(unitState);
            }
            else
            {
                if (CurrentState != null)
                {
                    Exit(CurrentState);
                }
                CurrentState = unitState;
            }            
            StateMgr.Ins.Update(ID, StateConditionBase.ConditionType.State);            
        }
        protected override void OnExit(IStateNode node)
        {
            IUnitState unitState = node as IUnitState;
            if (unitState.IsAdditive)
            {
                Additives.Remove(unitState);
            }
        }
    }
}